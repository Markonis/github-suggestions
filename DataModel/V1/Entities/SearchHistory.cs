using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Domain.V1.Entities
{
    [DataContract]
    public class SearchHistory
    {
        [DataMember]
        public List<CachedItem<Repository>> Repositories = new List<CachedItem<Repository>>();
    }
}
