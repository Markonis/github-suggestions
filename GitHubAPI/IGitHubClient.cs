using Domain.Interop;
using Domain.V1.Entities;
using Domain.V1.Messages.FullTextSearch;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitHubAPI
{
    public interface IGitHubClient
    {
        Task<Result<string>> GetLoginAsync(string authToken);
        Task<Result<SearchOutput>> SearchRepositoriesAsync(string authToken, string query);
        Task<Result<UserInfo>> GetUserInfoAsync(string authToken);
        Task<Result<Repository>> ScrapeRepositoryAsync(
            string authToken, string owner, string name);
        Task<Result<IEnumerable<Repository>>> ScrapeFollowingRepositoriesAsync(string authToken);
    }
}
