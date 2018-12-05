using Domain.Interop;
using Domain.V1.Entities;
using Domain.V1.Messages.Scraper;
using GitHubAPI;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Serialization;
using ServiceInterfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ScraperService
{
    public static class Scraper
    {
        public async static Task RunAsync(
            CancellationToken cancellationToken,
            IGitHubClient gitHubClient,
            IReliableStateManager stateManager,
            IUserRepoSearchActorProvider userRepoSearchActorProvider)
        {
            IReliableConcurrentQueue<ScrapingTask> firstTaskQueue =
                await GetFirstTaskQueue(stateManager);

            IReliableConcurrentQueue<ScrapingTask> secondTaskQueue =
                await GetSecondTaskQueue(stateManager);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (firstTaskQueue.Count > 0 || secondTaskQueue.Count > 0)
                {
                    using (var tx = stateManager.CreateTransaction())
                    {
                        var dequeued = await firstTaskQueue.TryDequeueAsync(tx, cancellationToken);

                        if (!dequeued.HasValue)
                            dequeued = await secondTaskQueue.TryDequeueAsync(tx, cancellationToken);

                        await ProcessScrapingTask(
                            scrapingTask: dequeued.Value,
                            gitHubClient: gitHubClient, tx: tx,
                            firstTaskQueue: firstTaskQueue,
                            secondTaskQueue: secondTaskQueue,
                            userRepoSearchActorProvider: userRepoSearchActorProvider);

                        await tx.CommitAsync();
                    }
                }
                else
                {
                    await Task.Delay(Constants.EMPTY_DELAY, cancellationToken);
                }
            }
        }

        private static Task<IReliableConcurrentQueue<ScrapingTask>>
            GetFirstTaskQueue(IReliableStateManager stateManager)
        {
            return stateManager.GetOrAddAsync<IReliableConcurrentQueue<ScrapingTask>>(
                Constants.FIRST_TASK_QUEUE);
        }

        private static Task<IReliableConcurrentQueue<ScrapingTask>>
            GetSecondTaskQueue(IReliableStateManager stateManager)
        {
            return stateManager.GetOrAddAsync<IReliableConcurrentQueue<ScrapingTask>>(
                    Constants.SECOND_TASK_QUEUE);
        }

        private async static Task ProcessScrapingTask(
            ScrapingTask scrapingTask,
            IGitHubClient gitHubClient,
            ITransaction tx,
            IReliableConcurrentQueue<ScrapingTask> firstTaskQueue,
            IReliableConcurrentQueue<ScrapingTask> secondTaskQueue,
            IUserRepoSearchActorProvider userRepoSearchActorProvider)
        {
            IUserRepoSearchActor userRepoSearchActor =
                userRepoSearchActorProvider.Provide(scrapingTask.UserLogin);

            await Scraper.PerformTaskAsync(
                scrapingTask: scrapingTask, gitHubClient: gitHubClient,
                tx: tx, userRepoSearchActor: userRepoSearchActor,
                firstTaskQueue: firstTaskQueue,
                secondTaskQueue: secondTaskQueue);
        }

        public async static Task PerformTaskAsync(
            ScrapingTask scrapingTask,
            IGitHubClient gitHubClient,
            ITransaction tx,
            IUserRepoSearchActor userRepoSearchActor,
            IReliableConcurrentQueue<ScrapingTask> firstTaskQueue,
            IReliableConcurrentQueue<ScrapingTask> secondTaskQueue)
        {
            switch (scrapingTask.Type)
            {
                case ScrapingTaskType.Repository:
                    await ScrapeRepositoryAsync(
                        scrapingTask: scrapingTask, gitHubClient: gitHubClient,
                        tx: tx, userRepoSearchActor: userRepoSearchActor,
                        secondTaskQueue: secondTaskQueue);
                    break;
                case ScrapingTaskType.UserInfo:
                    await ScrapeUserInfoAsync(
                        scrapingTask: scrapingTask, gitHubClient: gitHubClient,
                        tx: tx, userRepoSearchActor: userRepoSearchActor,
                        secondTaskQueue: secondTaskQueue);
                    break;
                case ScrapingTaskType.FollowingRepositories:
                    await ScrapeFollowingRepositoriesAsync(
                        scrapingTask: scrapingTask, gitHubClient: gitHubClient,
                        tx: tx, userRepoSearchActor: userRepoSearchActor,
                        secondTaskQueue: secondTaskQueue);
                    break;
            }
        }

        private async static Task ScrapeUserInfoAsync(
            ScrapingTask scrapingTask, IGitHubClient gitHubClient,
            ITransaction tx, IUserRepoSearchActor userRepoSearchActor,
            IReliableConcurrentQueue<ScrapingTask> secondTaskQueue)
        {
            Result<UserInfo> result = await gitHubClient
                .GetUserInfoAsync(scrapingTask.AuthToken);

            if (result.Success)
            {
                await userRepoSearchActor.SetUserInfoAsync(result.Data);
                await secondTaskQueue.EnqueueAsync(tx, new ScrapingTask
                {
                    Type = ScrapingTaskType.FollowingRepositories,
                    AuthToken = scrapingTask.AuthToken,
                    UserLogin = scrapingTask.UserLogin
                });
            }
        }

        private async static Task ScrapeRepositoryAsync(
            ScrapingTask scrapingTask, IGitHubClient gitHubClient,
            ITransaction tx, IUserRepoSearchActor userRepoSearchActor,
            IReliableConcurrentQueue<ScrapingTask> secondTaskQueue)
        {
            ScheduledRepository scheduledRepository =
                scrapingTask.ScheduledRepositories[0];

            Result<Repository> result = await gitHubClient
                .ScrapeRepositoryAsync(
                    authToken: scrapingTask.AuthToken,
                    owner: scheduledRepository.Owner,
                    name: scheduledRepository.Name);

            if (scrapingTask.ScheduledRepositories.Count > 1)
            {
                ScrapingTask nextScrapingTask = Serializer.DeepCopy(scrapingTask);

                nextScrapingTask.ScheduledRepositories = nextScrapingTask
                    .ScheduledRepositories.Skip(1).ToList();

                await secondTaskQueue.EnqueueAsync(tx, nextScrapingTask);
            }

            if (result.Success)
            {
                result.Data.IsNew = false;
                await userRepoSearchActor.SetRepositoryAsync(result.Data);
            }
        }

        private async static Task ScrapeFollowingRepositoriesAsync(
            ScrapingTask scrapingTask,
            IGitHubClient gitHubClient,
            ITransaction tx,
            IUserRepoSearchActor userRepoSearchActor,
            IReliableConcurrentQueue<ScrapingTask> secondTaskQueue)
        {
            Result<UserInfo> userInfoResult = await gitHubClient
                .GetUserInfoAsync(scrapingTask.AuthToken);

            if (!userInfoResult.Success) return;

            Result<IEnumerable<Repository>> repositoriesResult = await gitHubClient
                .ScrapeFollowingRepositoriesAsync(scrapingTask.AuthToken);

            if (!repositoriesResult.Success) return;

            List<ScheduledRepository> scheduledRepositories = repositoriesResult.Data.Select(
                repository => new ScheduledRepository
                {
                    Name = repository.Name,
                    Owner = repository.Owner
                }).ToList();

            await secondTaskQueue.EnqueueAsync(tx, new ScrapingTask
            {
                Type = ScrapingTaskType.Repository,
                AuthToken = scrapingTask.AuthToken,
                UserLogin = scrapingTask.UserLogin,
                ScheduledRepositories = scheduledRepositories
            });
        }

        public async static Task<Result> RequestRepositoryScrapingAsync(
            IReliableStateManager stateManager,
            RequestRepositoryScrapingInput input)
        {
            var firstTaskQueue = await GetFirstTaskQueue(stateManager);

            using (var tx = stateManager.CreateTransaction())
            {
                await firstTaskQueue.EnqueueAsync(tx, new ScrapingTask
                {
                    AuthToken = input.AuthToken,
                    ScheduledRepositories = new List<ScheduledRepository>
                    {
                        new ScheduledRepository
                        {
                            Name = input.Name,
                            Owner = input.Owner
                        }
                    },
                    UserLogin = input.UserLogin,
                    Type = ScrapingTaskType.Repository
                });

                await tx.CommitAsync();
            }

            return new Result { Success = true };
        }

        public async static Task<Result> RequestUserInfoScrapingAsync(
            IReliableStateManager stateManager, RequestUserInfoScrapingInput input)
        {
            var firstTaskQueue = await GetFirstTaskQueue(stateManager);

            using (var tx = stateManager.CreateTransaction())
            {
                await firstTaskQueue.EnqueueAsync(tx, new ScrapingTask
                {
                    AuthToken = input.AuthToken,
                    UserLogin = input.UserLogin,
                    Type = ScrapingTaskType.UserInfo
                });

                await tx.CommitAsync();
            }

            return new Result { Success = true };
        }
    }
}
