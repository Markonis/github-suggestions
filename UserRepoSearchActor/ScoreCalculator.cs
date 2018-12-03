using Domain.V1.Entities;

namespace UserRepoSearchActor
{
    public static class ScoreCalculator
    {
        public const float USER_STARRED_BOOST = 100;
        public const float FOLLOWING_FORKER_BOOST = 5;
        public const float FOLLOWING_STARGAZER_BOOST = 5;
        public const float FOLLOWING_WATCHER_BOOST = 2;
        public const float LANGUAGE_OVERLAP_BOOST = 80;
        public const float STARGAZER_BOOST = 1;
        public const float WATCHESR_BOOST = 1;
        public const float FORK_BOOST = 1;

        public static float Calculate(RepositoryScore repositoryScore)
        {
            float score = 0;
            if (repositoryScore.IsStarredByUser) score += 100;

            score += repositoryScore.FollowingForkersCount * FOLLOWING_FORKER_BOOST;
            score += repositoryScore.FollowingStargazersCount * FOLLOWING_STARGAZER_BOOST;
            score += repositoryScore.FollowingWatchersCount * FOLLOWING_WATCHER_BOOST;
            score += repositoryScore.LanguageOverlap * LANGUAGE_OVERLAP_BOOST;

            score += repositoryScore.Repository.StargazersCount * STARGAZER_BOOST;
            score += repositoryScore.Repository.WatchersCount * WATCHESR_BOOST;
            score += repositoryScore.Repository.ForksCount * FORK_BOOST;

            return score;
        }
    }
}
