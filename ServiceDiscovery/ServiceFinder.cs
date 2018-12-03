using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using ServiceInterfaces;
using System;
using System.Fabric;

namespace ServiceDiscovery
{
    public static class ServiceFinder
    {
        public static IUserRepoSearchActor GetUserRepoSearchActor(
            ServiceContext context, string gitHubLogin)
        {
            string applicationName = context
                    .CodePackageActivationContext.ApplicationName;

            Uri uri = new Uri($"{applicationName}/UserRepoSearchActorService");

            return ActorProxy.Create<IUserRepoSearchActor>(
                new ActorId(gitHubLogin), uri);
        }

        public static IFullTextSearchService GetFullTextSearchService(ServiceContext context)
        {
            string applicationName = context
                    .CodePackageActivationContext.ApplicationName;

            Uri uri = new Uri($"{applicationName}/FullTextSearchService");
            ServicePartitionKey partitionKey = new ServicePartitionKey(0);
            return ServiceProxy.Create<IFullTextSearchService>(uri, partitionKey);
        }

        public static IScraperService GetScraperService(ServiceContext context)
        {
            string applicationName = context
                        .CodePackageActivationContext.ApplicationName;

            Uri uri = new Uri($"{applicationName}/ScraperService");

            ServicePartitionKey partitionKey = new ServicePartitionKey(0);
            return ServiceProxy.Create<IScraperService>(uri, partitionKey);
        }
    }
}
