using Domain.Interop;
using Domain.V1.Entities;
using Domain.V1.Messages;
using Domain.V1.Messages.Scraper;
using Domain.V1.Messages.UserRepoSearch;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Serialization;
using ServiceDiscovery;
using ServiceInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FullTextSearch = Domain.V1.Messages.FullTextSearch;

namespace UserRepoSearchActor
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class UserRepoSearchActor : Actor, IUserRepoSearchActor
    {
        /// <summary>
        /// Initializes a new instance of UserRepoSearchActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public UserRepoSearchActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");

            // The StateManager is this actor's private state store.
            // Data stored in the StateManager will be replicated for high-availability for actors that use volatile or persisted state storage.
            // Any serializable object can be saved in the StateManager.
            // For more information, see https://aka.ms/servicefabricactorsstateserialization

            return Task.CompletedTask;
        }

        public Task<Result<SearchOutput>> SearchAsync(SearchInput input)
        {
            return UserRepoSearch.SearchAsync(
                actorId: this.GetActorId().ToString(),
                stateManager: StateManager,
                context: ActorService.Context,
                input: input);
        }

        public Task SetUserInfoAsync(UserInfo userInfo)
        {
            return UserRepoSearch.SetUserInfoAsync(StateManager, userInfo);
        }

        public Task SetRepositoryAsync(Repository repository)
        {
            return UserRepoSearch.SetRepositoryAsync(StateManager, repository);
        }

        public Task<AutoCompleteOutput> AutoCompleteAsync(SearchInput input)
        {
            return UserRepoSearch.AutoCompleteAsync(StateManager, input);
        }

        public Task<SearchOutput> GetSuggestionsAsync(SearchInput input)
        {
            return UserRepoSearch.GetSuggestionsAsync(StateManager, input);
        }
    }
}
