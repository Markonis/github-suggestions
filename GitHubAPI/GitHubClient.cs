using Domain.Errors;
using Domain.Interop;
using Domain.V1.Entities;
using Domain.V1.Messages.FullTextSearch;
using GraphQL.Client;
using GraphQL.Common.Request;
using GraphQL.Common.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitHubAPI
{
    public class GitHubClient : IGitHubClient
    {
        private const string GIT_HUB_GRAPHQL_ENDPOINT = "https://api.github.com/graphql";

        #region Get Login

        public async Task<Result<string>> GetLoginAsync(string authToken)
        {
            var request = new GraphQLRequest
            {
                Query = @"
                    query {
                        viewer {
                            login
                        }
                    }
                "
            };

            var result = await SendRequest(request, authToken);
            if (result.Success)
            {
                return new Result<string>
                {
                    Success = true,
                    Data = result.Data.Data.viewer.login
                };
            }
            else
            {
                return new Result<string>
                {
                    Success = false,
                    Errors = result.Errors
                };
            }
        }

        #endregion

        #region Full Text Search

        public async Task<Result<SearchOutput>> SearchRepositoriesAsync(string authToken, string query)
        {
            var request = new GraphQLRequest
            {
                Query = @"
                    query SearchRepositories($query: String!) {
                        search(first: 10, type: REPOSITORY, query: $query) {
                            nodes {
                                ... on Repository {
                                    name
                                    description
                                    pullRequests { totalCount }
                                    issues(states: [OPEN]) { totalCount }
                                    watchers { totalCount }
                                    stargazers { totalCount }
                                    forks { totalCount }
                                    owner { login }
                                }
                            }
                        }
                    }
                ",
                Variables = new { query }
            };

            var result = await SendRequest(request, authToken);
            if (result.Success)
            {
                return new Result<SearchOutput>
                {
                    Success = true,
                    Data = CreateSearchOutput(result.Data)
                };
            }
            else
            {
                return new Result<SearchOutput>
                {
                    Success = false,
                    Errors = result.Errors
                };
            }
        }

        private SearchOutput CreateSearchOutput(GraphQLResponse response)
        {
            List<Repository> repositories = new List<Repository>();
            foreach (var repoNode in response.Data.search.nodes)
            {
                repositories.Add(new Repository
                {
                    Name = repoNode.name,
                    Description = repoNode.description,
                    PullRequestsCount = repoNode.pullRequests.totalCount,
                    OpenIssuesCount = repoNode.issues.totalCount,
                    StargazersCount = repoNode.stargazers.totalCount,
                    ForksCount = repoNode.forks.totalCount,
                    WatchersCount = repoNode.watchers.totalCount,
                    Owner = repoNode.owner.login
                });
            }
            return new SearchOutput { Repositories = repositories };
        }

        #endregion

        #region Get User Info

        public async Task<Result<UserInfo>> GetUserInfoAsync(string authToken)
        {
            var request = new GraphQLRequest
            {
                Query = @"
                    query {
                        viewer {
                            login
                            followers(first: 100) { nodes { login } }
                            following(first: 100) { nodes { login } }
                            repositoriesContributedTo(first: 100, orderBy: {direction: DESC, field: PUSHED_AT})
                            {
                                nodes {
                                    name
                                    description
                                    owner { login }
                                }
                            }
                            starredRepositories(first: 100, orderBy: {direction: DESC, field: STARRED_AT}) {
                                nodes {
                                    name
                                    description
                                    owner { login }
                                }
                            }
                        }
                    }
                "
            };

            var result = await SendRequest(request, authToken);
            if (result.Success)
            {
                return new Result<UserInfo>
                {
                    Success = true,
                    Data = CreateUserInfo(result.Data)
                };
            }
            else
            {
                return new Result<UserInfo>
                {
                    Success = false,
                    Errors = result.Errors
                };
            }
        }

        private UserInfo CreateUserInfo(GraphQLResponse response)
        {
            List<string> followers = new List<string>();
            foreach (var node in response.Data.viewer.followers.nodes)
            {
                string login = node.login;
                followers.Add(login);
            }

            List<string> following = new List<string>();
            foreach (var node in response.Data.viewer.following.nodes)
            {
                string login = node.login;
                following.Add(login);
            }

            List<Repository> repositoriesContributedTo = new List<Repository>();
            foreach (var node in response.Data.viewer.repositoriesContributedTo.nodes)
            {
                string name = node.name;
                string description = node.description;
                string owner = node.owner.login;
                repositoriesContributedTo.Add(new Repository
                {
                    Name = name,
                    Description = description,
                    Owner = owner
                });
            }

            List<Repository> starredRepositories = new List<Repository>();
            foreach (var node in response.Data.viewer.starredRepositories.nodes)
            {
                string name = node.name;
                string description = node.description;
                string owner = node.owner.login;
                starredRepositories.Add(new Repository
                {
                    Name = name,
                    Description = description,
                    Owner = owner
                });
            }

            return new UserInfo
            {
                Login = response.Data.viewer.login,
                Followers = followers,
                Following = following,
                RepositoriesContributedTo = repositoriesContributedTo,
                StarredRepositories = starredRepositories
            };
        }

        #endregion

        #region Repository Scraping

        public async Task<Result<Repository>> ScrapeRepositoryAsync(
            string authToken, string owner, string name)
        {
            var request = new GraphQLRequest
            {
                Query = @"
                    query Scrape($name: String!, $owner: String!) {
                        repository(name: $name, owner: $owner) {
                            pushedAt
                            description
                            pullRequests { totalCount }
                            issues(states: [OPEN]) { totalCount }
                            watchers(first: 100) {
                                totalCount
                                nodes { login }
                            }
                            languages(first: 10) { nodes { name } }
                            forks(first: 100, orderBy: { direction: DESC, field: PUSHED_AT}) {
                                totalCount
                                nodes { owner { login } }
                            }
                            stargazers(first: 100, orderBy: { direction: DESC, field: STARRED_AT}) {
                                totalCount
                                nodes { login }
                            }
                        }
                    }
                ",
                Variables = new { name, owner }
            };

            var result = await SendRequest(request, authToken);
            if (result.Success)
            {
                return new Result<Repository>
                {
                    Success = true,
                    Data = CreateRepository(owner, name, result.Data)
                };
            }
            else
            {
                return new Result<Repository>
                {
                    Success = false,
                    Errors = result.Errors
                };
            }
        }

        private Repository CreateRepository(
            string owner, string name, GraphQLResponse response)
        {
            List<string> languages = new List<string>();
            foreach (var node in response.Data.repository.languages.nodes)
            {
                string langName = node.name;
                languages.Add(langName);
            }

            List<string> stargazers = new List<string>();
            foreach (var node in response.Data.repository.stargazers.nodes)
            {
                string login = node.login;
                stargazers.Add(login);
            }

            List<string> forkers = new List<string>();
            foreach (var node in response.Data.repository.forks.nodes)
            {
                string login = node.owner.login;
                forkers.Add(login);
            }

            List<string> watchers = new List<string>();
            foreach (var node in response.Data.repository.watchers.nodes)
            {
                string login = node.login;
                watchers.Add(login);
            }

            string pushedAtString = response.Data.repository.pushedAt;
            DateTime.TryParse(pushedAtString, out DateTime pushedAt);

            return new Repository
            {
                Name = name,
                Owner = owner,
                Description = response.Data.repository.description,
                Languages = languages,
                Stargazers = stargazers,
                Forkers = forkers,
                Watchers = watchers,
                OpenIssuesCount = response.Data.repository.issues.totalCount,
                PullRequestsCount = response.Data.repository.pullRequests.totalCount,
                WatchersCount = response.Data.repository.pullRequests.totalCount,
                StargazersCount = response.Data.repository.stargazers.totalCount,
                ForksCount = response.Data.repository.forks.totalCount,
                PushedAt = pushedAt
            };
        }

        public async Task<Result<IEnumerable<Repository>>> ScrapeFollowingRepositoriesAsync(string authToken)
        {
            var request = new GraphQLRequest
            {
                Query = @"
                    query {
                        viewer {
                            following(last: 30) {
                                nodes {
                                    starredRepositories(first: 10, orderBy:{field: STARRED_AT, direction: DESC}) {
                                        nodes { name owner { login } }
                                    }
                                    watching(first: 10, orderBy:{field: STARGAZERS, direction: DESC}) {
                                        nodes { name owner { login } }
                                    }
                                }
                            }
                        }
                    }
                "
            };

            var result = await SendRequest(request, authToken);
            if (result.Success)
            {
                return new Result<IEnumerable<Repository>>
                {
                    Success = true,
                    Data = CreateFollowingRepositories(result.Data)
                };
            }
            else
            {
                return new Result<IEnumerable<Repository>>
                {
                    Success = false,
                    Errors = result.Errors
                };
            }
        }

        private IEnumerable<Repository> CreateFollowingRepositories(GraphQLResponse response)
        {
            List<Repository> result = new List<Repository>();
            foreach (var followingNode in response.Data.viewer.following.nodes)
            {
                foreach (var starredNode in followingNode.starredRepositories.nodes)
                    result.Add(new Repository { Name = starredNode.name, Owner = starredNode.owner.login });

                foreach (var watchingNode in followingNode.watching.nodes)
                    result.Add(new Repository { Name = watchingNode.name, Owner = watchingNode.owner.login });
            }
            return result;
        }

        #endregion

        #region Helper Methods

        private async Task<Result<GraphQLResponse>> SendRequest(
            GraphQLRequest request, string authToken)
        {
            using (var client = new GraphQLClient(GIT_HUB_GRAPHQL_ENDPOINT))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"bearer {authToken}");
                client.DefaultRequestHeaders.Add("User-Agent", "code-ideas");

                try
                {
                    var response = await client.PostAsync(request);
                    return new Result<GraphQLResponse>
                    {
                        Success = true,
                        Data = response
                    };
                }
                catch (Exception ex)
                {
                    return new Result<GraphQLResponse>
                    {
                        Success = false,
                        Errors = new[] { new Error(ErrorCode.GitHubAPIError, ex.Message) }
                    };
                }
            }
        }

        #endregion
    }
}
