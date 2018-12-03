using Domain.Interop;
using Domain.V1.Entities;
using GitHubAPI;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Serialization;
using ServiceInterfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScraperService
{
    public static class Scraper
    {
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
    }
}
