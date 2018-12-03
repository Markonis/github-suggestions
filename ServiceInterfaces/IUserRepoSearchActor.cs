using Domain.Interop;
using Domain.V1.Entities;
using Domain.V1.Messages;
using Domain.V1.Messages.UserRepoSearch;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting;
using System.Threading.Tasks;

[assembly: FabricTransportActorRemotingProvider(RemotingListenerVersion = RemotingListenerVersion.V2_1, RemotingClientVersion = RemotingClientVersion.V2_1)]
namespace ServiceInterfaces
{
    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface IUserRepoSearchActor : IActor
    {
        Task<Result<SearchOutput>> SearchAsync(SearchInput input);
        Task SetUserInfoAsync(UserInfo userInfo);
        Task SetRepositoryAsync(Repository repository);
        Task<AutoCompleteOutput> AutoCompleteAsync(SearchInput input);
        Task<SearchOutput> GetSuggestionsAsync(SearchInput input);
    }
}
