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

        public async Task<Result<SearchOutput>> SearchAsync(SearchInput input)
        {
            if (await ShouldRequestUserInfoScrapingAsync())
                await RequestUserInfoScrapingAsync(input.AuthToken);

            var result = await PerformFullTextSearchAsync(input);

            List<SearchOutput.Item> items = new List<SearchOutput.Item>();

            foreach (var resultRepository in result.Data.Repositories)
            {
                CachedItem<RepositoryScore> item = await GetRepositoryScoreAsync(resultRepository);
                Repository repository = item != null ? item.Data.Repository : resultRepository;

                items.Add(new SearchOutput.Item
                {
                    Score = CalculateScore(item),
                    Repository = repository
                });


                if (ShouldRequestRepositoryScraping(item))
                    await RequestRepositoryScrapingAsync(input.AuthToken, repository);

                await AddQueryToRepositoryScoreAsync(repository, input.Query);
            }

            items = items.OrderByDescending(item => item.Score).ToList();

            await AddRepositoriesToSearchHistory(
                items.Select(item => item.Repository).ToList());

            return new Result<SearchOutput>
            {
                Success = true,
                Data = new SearchOutput { Items = items }
            };
        }

        private async Task<bool> ShouldRequestUserInfoScrapingAsync()
        {
            CachedItem<UserInfo> item = await GetUserInfoAsync();
            if (item == null) return true;
            return item.CachedAt + Constants.USER_INFO_LIFESPAN < DateTime.UtcNow;
        }

        private async Task<CachedItem<UserInfo>> GetUserInfoAsync()
        {
            var result = await StateManager
                .TryGetStateAsync<CachedItem<UserInfo>>(Constants.USER_INFO_KEY);
            return result.Value;
        }

        private async Task RequestUserInfoScrapingAsync(string authToken)
        {
            string userLogin = this.GetActorId().ToString();

            // Save the basic user info we have for now
            await SetUserInfoAsync(new UserInfo
            {
                Login = userLogin
            });

            IScraperService scraperService = ServiceFinder
                .GetScraperService(ActorService.Context);

            await scraperService.RequestUserInfoScrapingAsync(
                new RequestUserInfoScrapingInput
                {
                    AuthToken = authToken,
                    UserLogin = userLogin
                });
        }

        private async Task<Result<FullTextSearch.SearchOutput>>
            PerformFullTextSearchAsync(SearchInput input)
        {
            IFullTextSearchService fullTextSearchService = ServiceFinder
                .GetFullTextSearchService(ActorService.Context);

            return await fullTextSearchService.SearchAsync(new SearchInput
            {
                AuthToken = input.AuthToken,
                Query = input.Query
            });
        }

        private float CalculateScore(CachedItem<RepositoryScore> item)
        {
            if (item == null) return 0;
            return ScoreCalculator.Calculate(item.Data);
        }

        private bool ShouldRequestRepositoryScraping(CachedItem<RepositoryScore> item)
        {
            if (item == null || item.Data.Repository.IsNew) return true;
            return item.CachedAt + Constants.REPO_SCORE_LIFESPAN < DateTime.UtcNow;
        }

        private async Task RequestRepositoryScrapingAsync(
            string authToken, Repository repository)
        {
            string userLogin = this.GetActorId().ToString();

            // Save the basic score we have for now
            await SetRepositoryScore(new RepositoryScore
            {
                Repository = repository
            });

            IScraperService scraperService = ServiceFinder
                .GetScraperService(ActorService.Context);

            await scraperService.RequestRepositoryScrapingAsync(
                new RequestRepositoryScrapingInput
                {
                    AuthToken = authToken,
                    Name = repository.Name,
                    Owner = repository.Owner,
                    UserLogin = userLogin
                });
        }

        private async Task AddQueryToRepositoryScoreAsync(Repository repository, string query)
        {
            CachedItem<RepositoryScore> oldScore = await GetRepositoryScoreAsync(repository);

            RepositoryScore newScore = oldScore != null ?
                Serializer.DeepCopy(oldScore.Data) : new RepositoryScore();

            newScore.Repository = repository;
            if (!newScore.FoundInQueries.Contains(query))
                newScore.FoundInQueries.Add(query);

            await SetRepositoryScore(newScore);
        }

        private async Task<CachedItem<RepositoryScore>> GetRepositoryScoreAsync(Repository repository)
        {
            var result = await StateManager
                .TryGetStateAsync<CachedItem<RepositoryScore>>(RepositoryKey(repository));
            return result.Value;
        }

        private string RepositoryKey(Repository repository)
        {
            return $"{Constants.REPO_SCORE_KEY}-{repository.Owner}/{repository.Name}";
        }

        private async Task SetRepositoryScore(RepositoryScore repositoryScore)
        {
            await StateManager.SetStateAsync(
                RepositoryKey(repositoryScore.Repository),
                new CachedItem<RepositoryScore> { Data = repositoryScore });
        }

        private async Task AddRepositoriesToSearchHistory(List<Repository> repositories)
        {
            SearchHistory oldHistory = await GetSearchHistoryAsync();
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

            await SetSearchHistoryAsync(newHistory);
        }

        private string RepositoryId(Repository repository)
        {
            return $"{repository.Owner}/{repository.Name}";
        }

        private async Task<SearchHistory> GetSearchHistoryAsync()
        {
            var result = await StateManager
                .TryGetStateAsync<SearchHistory>(Constants.SEARCH_HISTORY_KEY);

            return result.HasValue ? result.Value : new SearchHistory();
        }

        private async Task SetSearchHistoryAsync(SearchHistory searchHistory)
        {
            await StateManager.SetStateAsync(Constants.SEARCH_HISTORY_KEY, searchHistory);
        }

        public async Task SetUserInfoAsync(UserInfo userInfo)
        {
            await StateManager.SetStateAsync(
                Constants.USER_INFO_KEY,
                new CachedItem<UserInfo> { Data = userInfo });
        }

        public async Task SetRepositoryAsync(Repository repository)
        {
            CachedItem<UserInfo> userInfo = await GetUserInfoAsync();
            CachedItem<RepositoryScore> oldScore = await GetRepositoryScoreAsync(repository);

            await SetRepositoryScore(CreateRepositoryScore(
                oldScore: oldScore?.Data,
                repository: repository,
                userInfo: userInfo.Data));
        }

        private RepositoryScore CreateRepositoryScore(
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

        public async Task<AutoCompleteOutput> AutoCompleteAsync(SearchInput input)
        {
            SearchHistory searchHistory = await GetSearchHistoryAsync();

            List<CachedItem<Repository>> matchingItems = searchHistory.Repositories
                .Where(item => RepositoryId(item.Data).IndexOf(input.Query) > -1)
                .Take(Constants.MAX_AUTO_COMPLETES).ToList();

            return new AutoCompleteOutput { Items = matchingItems };
        }

        public async Task<SearchOutput> GetSuggestionsAsync(SearchInput input)
        {
            IEnumerable<string> stateNames = await StateManager.GetStateNamesAsync();
            stateNames = stateNames.Where(name => IsRepoScoreState(name));

            List<SearchOutput.Item> items = new List<SearchOutput.Item>();

            foreach (string name in stateNames)
            {
                CachedItem<RepositoryScore> scoreItem = await StateManager
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

        private bool IsRepoScoreState(string stateName)
        {
            return stateName.StartsWith(Constants.REPO_SCORE_KEY);
        }
    }
}
