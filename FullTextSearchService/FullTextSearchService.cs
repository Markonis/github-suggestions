using Domain.Interop;
using Domain.V1.Entities;
using Domain.V1.Messages;
using Domain.V1.Messages.FullTextSearch;
using GitHubAPI;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceInterfaces;
using System.Collections.Generic;
using System.Fabric;
using System.Threading.Tasks;

namespace FullTextSearchService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class FullTextSearchService : StatefulService, IFullTextSearchService
    {
        private const string CACHE_DICTIONARY_NAME = "CachedSearchOutputs";

        public FullTextSearchService(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Performs a full text search of public repositories on GitHub.
        /// </summary>
        /// <remarks>It maintains internal cache of the results for past queries.</remarks>
        /// <param name="input"></param>
        /// <returns>A SearchOutput object containing the Repositories</returns>
        public async Task<Result<SearchOutput>> SearchAsync(SearchInput input)
        {
            using (ITransaction tx = StateManager.CreateTransaction())
            {
                var cache = await GetCache();
                IGitHubClient gitHubClient = new GitHubClient();
                return await FullTextSearch.SearchAsync(
                    input, cache, tx, gitHubClient);
            }
        }

        private Task<IReliableDictionary<string, CachedItem<SearchOutput>>> GetCache()
        {
            return StateManager
                .GetOrAddAsync<IReliableDictionary<string, CachedItem<SearchOutput>>>(CACHE_DICTIONARY_NAME);
        }

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
    }
}
