using Domain.Interop;
using Domain.V1.Messages;
using Domain.V1.Messages.FullTextSearch;
using Microsoft.ServiceFabric.Services.Remoting;
using System.Threading.Tasks;

namespace ServiceInterfaces
{
    public interface IFullTextSearchService : IService
    {
        Task<Result<SearchOutput>> SearchAsync(SearchInput searchInput);
    }
}
