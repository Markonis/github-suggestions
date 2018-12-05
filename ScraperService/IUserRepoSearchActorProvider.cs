using ServiceInterfaces;

namespace ScraperService
{
    public interface IUserRepoSearchActorProvider
    {
        IUserRepoSearchActor Provide(string gitHubLogin);
    }
}
