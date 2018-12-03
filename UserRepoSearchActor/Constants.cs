using System;

namespace UserRepoSearchActor
{
    public static class Constants
    {
        public const string REPO_SCORE_KEY = "RepositoryScore";
        public const string USER_INFO_KEY = "UserInfo";
        public const string SEARCH_HISTORY_KEY = "SearchHistory";

        public static readonly TimeSpan REPO_SCORE_LIFESPAN = new TimeSpan(3, 0, 0); // 3 hours
        public static readonly TimeSpan USER_INFO_LIFESPAN = new TimeSpan(3, 0, 0); // 3 hours

        public const int MAX_AUTO_COMPLETES = 10;
        public const int MAX_SUGGESTIONS = 10;
        public const int MAX_HISTORY_COUNT = 60;
    }
}
