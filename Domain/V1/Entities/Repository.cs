using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Domain.V1.Entities
{
    [DataContract]
    public class Repository
    {
        [DataMember]
        public string Name;

        [DataMember]
        public string Owner;

        [DataMember]
        public string Description;

        [DataMember]
        public List<string> Languages = new List<string>();

        [DataMember]
        public List<string> Stargazers = new List<string>();

        [DataMember]
        public List<string> Forkers = new List<string>();

        [DataMember]
        public List<string> Watchers = new List<string>();

        [DataMember]
        public int OpenIssuesCount;

        [DataMember]
        public int PullRequestsCount;

        [DataMember]
        public int StargazersCount;

        [DataMember]
        public int WatchersCount;

        [DataMember]
        public int ForksCount;

        [DataMember]
        public DateTime PushedAt;

        [DataMember]
        public bool IsNew = true;
    }
}
