using System;
using Octokit;
using Octokit.Helpers;
using System.Threading.Tasks;
using Octokit.Internal;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RebaseWorkflowStarterAction
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                string auth_token = args[0];
                string workflow_id = args[1];
                string pr_label_name = args[2];
                string current_repo_head_name = args[3];
                string full_repo_name = args[4];

                string current_branch_name = string.Join('/', current_repo_head_name.Split('/')[2..]);

                Console.WriteLine($"Rebase worflow id: {workflow_id}");
                Console.WriteLine($"PR lable name to check for autorebase: {pr_label_name}");
                Console.WriteLine($"Branch to rebase to: {current_branch_name}");
                Console.WriteLine($"Full repository name: {full_repo_name}");

                GitHubClient client = new GitHubClient(new Octokit.ProductHeaderValue("gmodnet-github-bot-rebase-workflow-starter"));

                Credentials tokenAuth = new Credentials(auth_token);

                client.Credentials = tokenAuth;

                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                httpClient.DefaultRequestHeaders.Add("User-Agent", "gmodnet-github-bot-rebase-workflow-starter");
                httpClient.DefaultRequestHeaders.Add("Authorization", "token " + auth_token);
                httpClient.BaseAddress = new Uri("https://api.github.com");

                var pull_requests = await client.PullRequest.GetAllForRepository(full_repo_name.Split('/')[0], full_repo_name.Split('/')[1]);

                foreach (PullRequest pr in pull_requests)
                {
                    if(pr.State.Value == ItemState.Open && pr.Labels.Any(label => label.Name == pr_label_name) && pr.Base.Ref == current_branch_name)
                    {
                        Console.WriteLine($"Triggering rebase workflow for Pull Request #{pr.Number}");

                        DispatchPostBody request_data = new DispatchPostBody(current_branch_name, new DispatchInputs(pr.Number.ToString()));

                        byte[] request_bytes = JsonSerializer.SerializeToUtf8Bytes<DispatchPostBody>(request_data);

                        HttpResponseMessage response = await httpClient.PostAsync($"/repos/{full_repo_name}/actions/workflows/{workflow_id}/dispatches",
                            new ByteArrayContent(request_bytes));

                        if(!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Failed to start rebase workflow for pr #{pr.Number}:");
                            Console.WriteLine($"Response status code: {response.StatusCode}");
                            Console.WriteLine($"Response body: {await response.Content.ReadAsStringAsync()}");
                        }
                    }
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception was thrown while executing action: {e}");
                return 1;
            }
        }
    }

    public record DispatchInputs(string prNumber);

    public record DispatchPostBody(string @ref, DispatchInputs inputs);
}
