using Domain.V1.Entities;
using System.Runtime.Serialization;

namespace Domain.V1.Messages.Scraper
{
    [DataContract]
    public class RequestRepositoryScrapingInput
    {
        [DataMember]
        public string Name;

        [DataMember]
        public string Owner;

        [DataMember]
        public string AuthToken;

        [DataMember]
        public string UserLogin;
    }
}
