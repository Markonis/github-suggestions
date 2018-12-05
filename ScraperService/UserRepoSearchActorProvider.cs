using ServiceDiscovery;
using ServiceInterfaces;
using System.Fabric;

namespace ScraperService
{
    public class UserRepoSearchActorProvider : IUserRepoSearchActorProvider
    {
        public StatefulServiceContext Context { get; set; }

        public IUserRepoSearchActor Provide(string gitHubLogin)
        {
            return ServiceProvider.GetUserRepoSearchActor(Context, gitHubLogin);
        }
    }
}
