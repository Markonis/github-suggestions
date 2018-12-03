using Domain.V1.Entities;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Domain.V1.Messages.UserRepoSearch
{
    [DataContract]
    public class SearchOutput
    {
        [DataMember]
        public List<Item> Items;

        [DataContract]
        public class Item
        {
            [DataMember]
            public Repository Repository;

            [DataMember]
            public float Score;
        }
    }
}
