using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Domain.V1.Entities
{
    [DataContract]
    public class RepositoryScore
    {
        [DataMember]
        public Repository Repository;

        [DataMember]
        public bool IsStarredByUser;

        [DataMember]
        public int FollowingForkersCount;

        [DataMember]
        public int FollowingStargazersCount;

        [DataMember]
        public int FollowingWatchersCount;

        [DataMember]
        public List<string> FoundInQueries = new List<string>();

        [DataMember]
        public float LanguageOverlap;
    }
}
