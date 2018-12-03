using Domain.V1.Entities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Domain.V1.Messages.FullTextSearch
{
    [DataContract]
    public class SearchOutput
    {
        [DataMember]
        public List<Repository> Repositories { get; set; }
    }
}
