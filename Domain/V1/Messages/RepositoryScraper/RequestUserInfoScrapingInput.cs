using System.Runtime.Serialization;

namespace Domain.V1.Messages.Scraper
{
    [DataContract]
    public class RequestUserInfoScrapingInput
    {
        [DataMember]
        public string AuthToken;

        [DataMember]
        public string UserLogin;
    }
}
