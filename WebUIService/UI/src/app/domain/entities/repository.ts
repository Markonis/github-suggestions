export class Repository {
  constructor(
    public name: string,
    public owner: string,
    public description: string,
    public openIssuesCount: number,
    public stargazersCount: number,
    public forksCount: number,
    public watchersCount: number,
    public pullRequestsCount: number) { }
}
