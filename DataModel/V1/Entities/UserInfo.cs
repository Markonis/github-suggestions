using System;
using System.Collections.Generic;

namespace Domain.V1.Entities
{
    public class UserInfo
    {
        public string Login;
        public List<string> Followers = new List<string>();
        public List<string> Following = new List<string>();
        public List<Repository> RepositoriesContributedTo = new List<Repository>();
        public List<Repository> StarredRepositories = new List<Repository>();
    }
}
