using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Interop;
using Domain.V1.Entities;
using Domain.V1.Messages;
using Domain.V1.Messages.Scraper;
using Domain.V1.Messages.UserRepoSearch;
using Microsoft.ServiceFabric.Actors.Runtime;
using Serialization;
using ServiceDiscovery;
using ServiceInterfaces;

namespace UserRepoSearchActor
{
    public static class UserRepoSearch
    {
        public async static Task<Result<SearchOutput>> SearchAsync(
            string actorId, IActorStateManager stateManager,
            StatefulServiceContext context,
            SearchInput input)
        {
            if (await ShouldRequestUserInfoScrapingAsync(stateManager))
                await RequestUserInfoScrapingAsync(
                    actorId: actorId,
                    stateManager: stateManager,
                    context: context,
                    authToken: input.AuthToken);

            var result = await PerformFullTextSearchAsync(context, input);

            List<SearchOutput.Item> items = new List<SearchOutput.Item>();

            foreach (var resultRepository in result.Data.Repositories)
            {
                CachedItem<RepositoryScore> item =
                    await GetRepositoryScoreAsync(stateManager, resultRepository);

                Repository repository = item != null ? item.Data.Repository : resultRepository;

                items.Add(new SearchOutput.Item
                {
                    Score = CalculateScore(item),
                    Repository = repository
                });

                if (ShouldRequestRepositoryScraping(item))
                    await RequestRepositoryScrapingAsync(
                        context: context,
                        actorId: actorId,
                        stateManager: stateManager,
                        authToken: input.AuthToken,
                        repository: repository);

                await AddQueryToRepositoryScoreAsync(stateManager, repository, input.Query);
            }

            items = items.OrderByDescending(item => item.Score).ToList();

            await AddRepositoriesToSearchHistory(
                stateManager, items.Select(item => item.Repository).ToList());

            return new Result<SearchOutput>
            {
                Success = true,
                Data = new SearchOutput { Items = items }
            };
        }

        private async static Task<bool> ShouldRequestUserInfoScrapingAsync(
            IActorStateManager stateManager)
        {
            CachedItem<UserInfo> item = await GetUserInfoAsync(stateManager);
            if (item == null) return true;
            return item.CachedAt + Constants.USER_INFO_LIFESPAN < DateTime.UtcNow;
        }

        private async static Task<CachedItem<UserInfo>> GetUserInfoAsync(
            IActorStateManager stateManager)
        {
            var result = await stateManager
                .TryGetStateAsync<CachedItem<UserInfo>>(Constants.USER_INFO_KEY);
            return result.Value;
        }

        private async static Task RequestUserInfoScrapingAsync(
            string actorId, IActorStateManager stateManager,
            StatefulServiceContext context, string authToken)
        {
            string userLogin = actorId;

            // Save the basic user info we have for now
            await SetUserInfoAsync(stateManager, new UserInfo
            {
                Login = userLogin
            });

            IScraperService scraperService = ServiceProvider
                .GetScraperService(context);

            await scraperService.RequestUserInfoScrapingAsync(
                new RequestUserInfoScrapingInput
                {
                    AuthToken = authToken,
                    UserLogin = userLogin
                });
        }

        public async static Task SetUserInfoAsync(
            IActorStateManager stateManager, UserInfo userInfo)
        {
            await stateManager.SetStateAsync(
                Constants.USER_INFO_KEY,
                new CachedItem<UserInfo> { Data = userInfo });
        }

        private async static Task<Result<Domain.V1.Messages.FullTextSearch.SearchOutput>>
            PerformFullTextSearchAsync(
            StatefulServiceContext context, SearchInput input)
        {
            IFullTextSearchService fullTextSearchService = ServiceProvider
                .GetFullTextSearchService(context);

            return await fullTextSearchService.SearchAsync(new SearchInput
            {
                AuthToken = input.AuthToken,
                Query = input.Query
            });
        }

        private async static Task<CachedItem<RepositoryScore>>
            GetRepositoryScoreAsync(IActorStateManager stateManager, Repository repository)
        {
            var result = await stateManager
                .TryGetStateAsync<CachedItem<RepositoryScore>>(RepositoryKey(repository));
            return result.Value;
        }

        private static string RepositoryKey(Repository repository)
        {
            return $"{Constants.REPO_SCORE_KEY}-{repository.Owner}/{repository.Name}";
        }

        private static float CalculateScore(CachedItem<RepositoryScore> item)
        {
            if (item == null) return 0;
            return ScoreCalculator.Calculate(item.Data);
        }

        private static bool ShouldRequestRepositoryScraping(CachedItem<RepositoryScore> item)
        {
            if (item == null || item.Data.Repository.IsNew) return true;
            return item.CachedAt + Constants.REPO_SCORE_LIFESPAN < DateTime.UtcNow;
        }

        private async static Task RequestRepositoryScrapingAsync(
            StatefulServiceContext context, IActorStateManager stateManager,
            string actorId, string authToken, Repository repository)
        {
            string userLogin = actorId;

            // Save the basic score we have for now
            await SetRepositoryScore(
                stateManager, new RepositoryScore
                {
                    Repository = repository
                });

            IScraperService scraperService = ServiceProvider
                .GetScraperService(context);

            await scraperService.RequestRepositoryScrapingAsync(
                new RequestRepositoryScrapingInput
                {
                    AuthToken = authToken,
                    Name = repository.Name,
                    Owner = repository.Owner,
                    UserLogin = userLogin
                });
        }

        private async static Task SetRepositoryScore(
            IActorStateManager stateManager, RepositoryScore repositoryScore)
        {
            await stateManager.SetStateAsync(
                RepositoryKey(repositoryScore.Repository),
                new CachedItem<RepositoryScore> { Data = repositoryScore });
        }

        private async static Task AddQueryToRepositoryScoreAsync(
            IActorStateManager stateManager, Repository repository, string query)
        {
            CachedItem<RepositoryScore> oldScore =
                await GetRepositoryScoreAsync(stateManager, repository);

            RepositoryScore newScore = oldScore != null ?
                Serializer.DeepCopy(oldScore.Data) : new RepositoryScore();

            newScore.Repository = repository;
            if (!newScore.FoundInQueries.Contains(query))
                newScore.FoundInQueries.Add(query);

            await SetRepositoryScore(stateManager, newScore);
        }

        public static async Task SetRepositoryAsync(
            IActorStateManager stateManager, Repository repository)
        {
            CachedItem<UserInfo> userInfo =
                await GetUserInfoAsync(stateManager);

            CachedItem<RepositoryScore> oldScore =
                await GetRepositoryScoreAsync(stateManager, repository);

            await SetRepositoryScore(stateManager,
                CreateRepositoryScore(
                oldScore: oldScore?.Data,
                repository: repository,
                userInfo: userInfo.Data));
        }

        private static RepositoryScore CreateRepositoryScore(
            RepositoryScore oldScore, Repository repository, UserInfo userInfo)
        {
            bool isStarredByUser = repository.Stargazers.Contains(userInfo.Login);

            int followingForkersCount =
                userInfo.Following.Intersect(repository.Forkers).Count();

            int followingStargazersCount =
                userInfo.Following.Intersect(repository.Stargazers).Count();

            List<string> foundInQueries = oldScore != null ?
                oldScore.FoundInQueries : new List<string>();

            return new RepositoryScore
            {
                Repository = repository,
                IsStarredByUser = isStarredByUser,
                FollowingForkersCount = followingForkersCount,
                FollowingStargazersCount = followingStargazersCount,
                FoundInQueries = foundInQueries
            };
        }

        private async static Task AddRepositoriesToSearchHistory(
            IActorStateManager stateManager, List<Repository> repositories)
        {
            SearchHistory oldHistory = await GetSearchHistoryAsync(stateManager);
            SearchHistory newHistory = Serializer.DeepCopy(oldHistory);

            List<CachedItem<Repository>> newRepositories =
                new List<CachedItem<Repository>>();

            List<string> oldRepositoryIds = oldHistory.Repositories
                .Select(item => RepositoryId(item.Data)).ToList();

            foreach (var repository in repositories)
                if (!oldRepositoryIds.Contains(RepositoryId(repository)))
                    newRepositories.Add(new CachedItem<Repository> { Data = repository });

            int skip = newRepositories.Count - Constants.MAX_HISTORY_COUNT;
            newRepositories = newRepositories.Skip(skip).ToList();

            newRepositories.AddRange(oldHistory.Repositories);
            newHistory.Repositories = newRepositories;

            await SetSearchHistoryAsync(stateManager, newHistory);
        }

        private async static Task<SearchHistory> GetSearchHistoryAsync(
            IActorStateManager stateManager)
        {
            var result = await stateManager
                .TryGetStateAsync<SearchHistory>(Constants.SEARCH_HISTORY_KEY);

            return result.HasValue ? result.Value : new SearchHistory();
        }

        private static string RepositoryId(Repository repository)
        {
            return $"{repository.Owner}/{repository.Name}";
        }

        private async static Task SetSearchHistoryAsync(
            IActorStateManager stateManager, SearchHistory searchHistory)
        {
            await stateManager.SetStateAsync(Constants.SEARCH_HISTORY_KEY, searchHistory);
        }

        public async static Task<AutoCompleteOutput> AutoCompleteAsync(
            IActorStateManager stateManager, SearchInput input)
        {
            SearchHistory searchHistory = await GetSearchHistoryAsync(stateManager);

            List<CachedItem<Repository>> matchingItems = searchHistory.Repositories
                .Where(item => RepositoryId(item.Data).IndexOf(input.Query) > -1)
                .Take(Constants.MAX_AUTO_COMPLETES).ToList();

            return new AutoCompleteOutput { Items = matchingItems };
        }

        public async static Task<SearchOutput> GetSuggestionsAsync(
            IActorStateManager stateManager, SearchInput input)
        {
            IEnumerable<string> stateNames = await stateManager.GetStateNamesAsync();
            stateNames = stateNames.Where(name => IsRepoScoreState(name));

            List<SearchOutput.Item> items = new List<SearchOutput.Item>();

            foreach (string name in stateNames)
            {
                CachedItem<RepositoryScore> scoreItem = await stateManager
                    .GetStateAsync<CachedItem<RepositoryScore>>(name);
                items.Add(new SearchOutput.Item
                {
                    Repository = scoreItem.Data.Repository,
                    Score = ScoreCalculator.Calculate(scoreItem.Data)
                });
            }

            items = items.OrderByDescending(item => item.Score)
                .Take(Constants.MAX_SUGGESTIONS).ToList();

            return new SearchOutput { Items = items };
        }

        private static bool IsRepoScoreState(string stateName)
        {
            return stateName.StartsWith(Constants.REPO_SCORE_KEY);
        }
    }
}
