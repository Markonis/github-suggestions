using Domain.Interop;
using Domain.V1.Messages.Scraper;
using GitHubAPI;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceInterfaces;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;

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
            IGitHubClient gitHubClient = new GitHubClient();
            IUserRepoSearchActorProvider userRepoSearchActorProvider =
                new UserRepoSearchActorProvider { Context = Context };

            await Scraper.RunAsync(
                cancellationToken: cancellationToken,
                gitHubClient: gitHubClient,
                stateManager: StateManager,
                userRepoSearchActorProvider: userRepoSearchActorProvider);
        }

        public Task<Result> RequestRepositoryScrapingAsync(RequestRepositoryScrapingInput input)
        {
            return Scraper.RequestRepositoryScrapingAsync(StateManager, input);
        }

        public Task<Result> RequestUserInfoScrapingAsync(RequestUserInfoScrapingInput input)
        {
            return Scraper.RequestUserInfoScrapingAsync(StateManager, input);
        }
    }
}
