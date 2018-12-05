using Domain.Interop;
using Domain.V1.Entities;
using Domain.V1.Messages;
using Domain.V1.Messages.FullTextSearch;
using GitHubAPI;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FullTextSearchService.Test
{
    public class FullTextSearchTest
    {
        [Fact]
        public void SearchAsync_ShouldFailIfQueryIsNullOrEmpty()
        {
            Mock<ITransaction> tx = new Mock<ITransaction>();

            Mock<IReliableDictionary<string, CachedItem<SearchOutput>>> cache =
                new Mock<IReliableDictionary<string, CachedItem<SearchOutput>>>();

            Mock<IGitHubClient> gitHubClient = new Mock<IGitHubClient>();

            SearchInput input = new SearchInput { Query = "" };

            Result<SearchOutput> result = FullTextSearch.SearchAsync(
                input: input, cache: cache.Object,
                tx: tx.Object, gitHubClient: gitHubClient.Object).Result;

            Assert.False(result.Success);
        }

        [Fact]
        public void SearchAsync_ShouldReturnCachedValueIfFresh()
        {
            Mock<ITransaction> tx = new Mock<ITransaction>();

            Mock<IReliableDictionary<string, CachedItem<SearchOutput>>> cache =
                new Mock<IReliableDictionary<string, CachedItem<SearchOutput>>>();

            CachedItem<SearchOutput> expectedResult = new CachedItem<SearchOutput>
            {
                Data = new SearchOutput { }
            };

            cache.Setup(dict => dict.TryGetValueAsync(It.IsAny<ITransaction>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new ConditionalValue<CachedItem<SearchOutput>>(true, expectedResult)));

            Mock<IGitHubClient> gitHubClient = new Mock<IGitHubClient>();

            Result<SearchOutput> result = FullTextSearch.SearchAsync(
               input: new SearchInput { Query = "Test" }, cache: cache.Object,
               tx: tx.Object, gitHubClient: gitHubClient.Object).Result;

            Assert.True(result.Success);
            Assert.Equal(expectedResult.Data, result.Data);
        }

        [Fact]
        public void SearchAsync_ShouldPerformApiSearchIfNotFresh()
        {
            Mock<ITransaction> tx = new Mock<ITransaction>();

            Mock<IReliableDictionary<string, CachedItem<SearchOutput>>> cache =
                new Mock<IReliableDictionary<string, CachedItem<SearchOutput>>>();

            Result<SearchOutput> expectedResult = new Result<SearchOutput>
            {
                Success = true,
                Data = new SearchOutput { }
            };

            cache.Setup(dict => dict.TryGetValueAsync(It.IsAny<ITransaction>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new ConditionalValue<CachedItem<SearchOutput>>(false, null)));

            Mock<IGitHubClient> gitHubClient = new Mock<IGitHubClient>();
            gitHubClient.Setup(client => client.SearchRepositoriesAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(expectedResult));

            Result<SearchOutput> result = FullTextSearch.SearchAsync(
               input: new SearchInput { Query = "Test" }, cache: cache.Object,
               tx: tx.Object, gitHubClient: gitHubClient.Object).Result;

            Assert.True(result.Success);
            Assert.Equal(expectedResult.Data, result.Data);
        }
    }
}
