using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Domain.V1.Entities
{
    [DataContract]
    public class ScrapingTask
    {
        [DataMember]
        public ScrapingTaskType Type;

        [DataMember]
        public string AuthToken;

        [DataMember]
        public string UserLogin;

        [DataMember]
        public List<ScheduledRepository> ScheduledRepositories =
            new List<ScheduledRepository>();
    }
}
