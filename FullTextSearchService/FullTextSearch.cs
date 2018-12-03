using Domain.Interop;
using Domain.V1.Entities;
using Domain.V1.Messages;
using Domain.V1.Messages.FullTextSearch;
using GitHubAPI;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System;
using System.Threading.Tasks;

namespace FullTextSearchService
{
    public static class FullTextSearch
    {
        public static readonly TimeSpan CACHE_LIFESPAN = new TimeSpan(3, 0, 0); // 3 hours

        public async static Task<Result<SearchOutput>> SearchAsync(
            SearchInput input,
            IReliableDictionary<string, CachedItem<SearchOutput>> cache,
            ITransaction tx,
            IGitHubClient gitHubClient)
        {
            if (string.IsNullOrWhiteSpace(input.Query))
                return new Result<SearchOutput> { Success = false };

            input.Query = input.Query.ToLowerInvariant();

            SearchOutput searchOutput = await FindInCache(input.Query, cache, tx);

            if (searchOutput == null)
            {
                var result = await SearchOverAPI(input, gitHubClient);
                if (result.Success)
                {
                    searchOutput = result.Data;
                    await SaveToCache(input.Query, cache, searchOutput, tx);
                    await tx.CommitAsync();
                }
            }

            return new Result<SearchOutput>
            {
                Success = searchOutput != null,
                Data = searchOutput
            };
        }

        public async static Task<SearchOutput> FindInCache(
            string query,
            IReliableDictionary<string, CachedItem<SearchOutput>> cache,
            ITransaction tx)
        {
            var cachedResult = await cache.TryGetValueAsync(tx, query);
            if (cachedResult.HasValue)
            {
                var cachedItem = cachedResult.Value;
                if (IsFresh(cachedItem))
                    return cachedItem.Data;
                else
                    return null;
            }
            else
            {
                return null;
            }
        }

        public static bool IsFresh(CachedItem<SearchOutput> cachedItem)
        {
            return cachedItem.CachedAt.Add(CACHE_LIFESPAN) > DateTime.UtcNow;
        }

        public static async Task SaveToCache(
            string query,
            IReliableDictionary<string, CachedItem<SearchOutput>> cache,
            SearchOutput output,
            ITransaction tx)
        {
            await cache.AddAsync(tx, query, new CachedItem<SearchOutput>
            {
                CachedAt = DateTime.UtcNow,
                Data = output
            });
        }

        public static async Task<Result<SearchOutput>> SearchOverAPI(
            SearchInput input, IGitHubClient gitHubClient)
        {
            return await gitHubClient.SearchRepositoriesAsync(
                input.AuthToken, input.Query);
        }
    }
}
