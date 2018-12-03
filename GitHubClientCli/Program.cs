using Domain.Interop;
using Domain.V1.Entities;
using GitHubAPI;
using System;
using System.Collections.Generic;

namespace GitHubClientCli
{
    class Program
    {
        static void Main(string[] args)
        {
            IGitHubClient gitHubClient = new GitHubClient();

            Console.WriteLine("Enter auth token:");
            string authToken = Console.ReadLine();

            string command = "";
            while (command != "exit")
            {
                Console.WriteLine("Enter command:");
                command = Console.ReadLine();

                if (command == "UserInfo")
                {
                    Result<UserInfo> result = gitHubClient.GetUserInfoAsync(authToken).Result;
                    if (result.Success)
                        Console.WriteLine(result.Data.Login);
                    else
                        Console.WriteLine("Error!");
                }
                else if (command == "Repository")
                {
                    Console.WriteLine("Enter owner:");
                    string owner = Console.ReadLine();

                    Console.WriteLine("Enter name:");
                    string name = Console.ReadLine();

                    Result<Repository> result = gitHubClient.ScrapeRepositoryAsync(
                        authToken, owner, name).Result;

                    if (result.Success)
                        Console.WriteLine(result.Data.Name);
                    else
                        Console.WriteLine("Error!");
                }
                else if (command == "FollowingRepositories")
                {
                    Result<IEnumerable<Repository>> result = gitHubClient.ScrapeFollowingRepositoriesAsync(authToken).Result;
                    if (result.Success)
                    {
                        foreach (var repo in result.Data)
                            Console.WriteLine($"{repo.Owner}/{repo.Name}");
                    }
                    else
                    {
                        Console.WriteLine("Error!");
                    }
                }
            }
        }
    }
}
