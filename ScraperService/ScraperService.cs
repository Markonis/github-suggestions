using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Interop;
using Domain.V1.Entities;
using Domain.V1.Messages.Scraper;
using GitHubAPI;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceDiscovery;
using ServiceInterfaces;

namespace ScraperService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class ScraperService : StatefulService, IScraperService
    {
        public ScraperService(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var firstTaskQueue = await GetFirstTaskQueue();
            var secondTaskQueue = await GetSecondTaskQueue();
            IGitHubClient gitHubClient = new GitHubClient();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (firstTaskQueue.Count > 0 || secondTaskQueue.Count > 0)
                {
                    using (var tx = StateManager.CreateTransaction())
                    {
                        var dequeued = await firstTaskQueue.TryDequeueAsync(tx, cancellationToken);

                        if (!dequeued.HasValue)
                            dequeued = await secondTaskQueue.TryDequeueAsync(tx, cancellationToken);

                        await ProcessScrapingTask(
                            scrapingTask: dequeued.Value,
                            gitHubClient: gitHubClient, tx: tx,
                            firstTaskQueue: firstTaskQueue,
                            secondTaskQueue: secondTaskQueue);

                        await tx.CommitAsync();
                    }
                }
                else
                {
                    await Task.Delay(Constants.EMPTY_DELAY, cancellationToken);
                }
            }
        }

        private Task<IReliableConcurrentQueue<ScrapingTask>> GetFirstTaskQueue()
        {
            return StateManager
                .GetOrAddAsync<IReliableConcurrentQueue<ScrapingTask>>(
                    Constants.FIRST_TASK_QUEUE);
        }

        private Task<IReliableConcurrentQueue<ScrapingTask>> GetSecondTaskQueue()
        {
            return StateManager
                .GetOrAddAsync<IReliableConcurrentQueue<ScrapingTask>>(
                    Constants.SECOND_TASK_QUEUE);
        }

        private async Task ProcessScrapingTask(
            ScrapingTask scrapingTask,
            IGitHubClient gitHubClient,
            ITransaction tx,
            IReliableConcurrentQueue<ScrapingTask> firstTaskQueue,
            IReliableConcurrentQueue<ScrapingTask> secondTaskQueue)
        {
            IUserRepoSearchActor userRepoSearchActor =
                ServiceFinder.GetUserRepoSearchActor(Context, scrapingTask.UserLogin);

            await Scraper.PerformTaskAsync(
                scrapingTask: scrapingTask, gitHubClient: gitHubClient,
                tx: tx, userRepoSearchActor: userRepoSearchActor,
                firstTaskQueue: firstTaskQueue,
                secondTaskQueue: secondTaskQueue);
        }

        public async Task<Result> RequestRepositoryScrapingAsync(RequestRepositoryScrapingInput input)
        {
            var firstTaskQueue = await GetFirstTaskQueue();

            using (var tx = StateManager.CreateTransaction())
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

        public async Task<Result> RequestUserInfoScrapingAsync(RequestUserInfoScrapingInput input)
        {
            var firstTaskQueue = await GetFirstTaskQueue();

            using (var tx = StateManager.CreateTransaction())
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
