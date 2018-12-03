using Domain.Interop;
using Domain.V1.Messages.Scraper;
using Microsoft.ServiceFabric.Services.Remoting;
using System.Threading.Tasks;

namespace ServiceInterfaces
{
    public interface IScraperService : IService
    {
        Task<Result> RequestRepositoryScrapingAsync(RequestRepositoryScrapingInput input);
        Task<Result> RequestUserInfoScrapingAsync(RequestUserInfoScrapingInput input);
    }
}
