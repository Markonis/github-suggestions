using Domain.V1.Entities;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Domain.V1.Messages.UserRepoSearch
{
    [DataContract]
    public class AutoCompleteOutput
    {
        [DataMember]
        public List<CachedItem<Repository>> Items =
            new List<CachedItem<Repository>>();
    }
}
