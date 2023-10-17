using Flurl;
using Flurl.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Text;

namespace ConsoleApp11
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var service = new ServiceCollection();
            service.AddHttpClient();
            var serviceProvider = service.BuildServiceProvider();

            List<GogsRepListItem> gitRepos = await GetGogsGitRepos(serviceProvider);
            foreach (var item in gitRepos)
            {
                try
                {
                    Console.WriteLine($"开始处理: {item.clone_url}");
                    await CreateGiteaOrg(serviceProvider, item);

                    await MigrateRep(serviceProvider, item);

                    Console.WriteLine($"{item.clone_url} 处理完成");

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{item.clone_url} 处理异常, {ex.Message}");
                }
                Console.WriteLine($"按任意键继续");
                Console.ReadLine();
            }
        }
        public class MigrateRepRequest
        {
            public string clone_addr { get; set; }
            public string auth_token { get; set; }
            [JsonProperty("private")]
            public bool Private { get; set; }
            public bool mirror { get; set; }

            public string repo_name { get; set; }
            public string repo_owner { get; set; }
            public string description { get; set; }
        }
        private static async Task CreateGiteaOrg(ServiceProvider serviceProvider, GogsRepListItem item)
        {
            HttpClient httpCLient = CreateGiteaHttpClient(serviceProvider);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orgs");
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", "ccc");

            var orgCreateRequest = new GiteaOrgCreate { username = item.owner.username };
            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(orgCreateRequest), Encoding.UTF8, "application/json");

            var rsp = await httpCLient.SendAsync(requestMessage);
            var statusCode = (int)rsp.StatusCode;
            switch (statusCode)
            {
                case 201:
                    Console.WriteLine($"创建Org:[{orgCreateRequest.username}] 成功");
                    break;
                case 422:
                    Console.WriteLine($"创建Org:[{orgCreateRequest.username}] 已存在");
                    break;
                default:
                    throw new Exception($"创建Org:[{orgCreateRequest.username}]失败, StatusCode: {statusCode}");
            }
        }
        private static async Task MigrateRep(ServiceProvider serviceProvider, GogsRepListItem item)
        {
            HttpClient httpCLient = CreateGiteaHttpClient(serviceProvider);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/repos/migrate");
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", "xxx");

            var orgCreateRequest = new MigrateRepRequest
            {
                clone_addr = item.clone_url,
                mirror = item.mirror,
                Private = item.Private,
                repo_name = item.name,
                description = item.description,
                repo_owner = item.owner.username,
                auth_token = "d4f9013697d2837df122706d0c29e8911001e650"
            };
            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(orgCreateRequest), Encoding.UTF8, "application/json");

            var rsp = await httpCLient.SendAsync(requestMessage);
            var statusCode = (int)rsp.StatusCode;
            switch (statusCode)
            {
                case 201:
                    Console.WriteLine($"Rep: [{item.clone_url}] migrate 成功");
                    break;
                case 409:
                    Console.WriteLine($"Rep: [{item.clone_url}] 已存在");
                    break;
                default:
                    throw new Exception($"Rep: [{item.clone_url}] migrate 失败, StatusCode: {statusCode}");
            }
        }

        private static HttpClient CreateGiteaHttpClient(ServiceProvider serviceProvider)
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpCLient = httpClientFactory.CreateClient();
            httpCLient.BaseAddress = new Uri("https://gitea");
            return httpCLient;
        }

        private static async Task<List<GogsRepListItem>> GetGogsGitRepos(ServiceProvider serviceProvider)
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpCLient = httpClientFactory.CreateClient();
            var requestMessage = new HttpRequestMessage();
            requestMessage.Method = HttpMethod.Get;
            requestMessage.RequestUri = new Uri("https://gogs/api/v1/user/repos");
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", "d4f9013697d2837df122706d0c29e8911001e650");
            var rsp = await httpCLient.SendAsync(requestMessage);
            var content = await rsp.Content.ReadAsStringAsync();
            var gitRepos = JsonConvert.DeserializeObject<List<GogsRepListItem>>(content);
            return gitRepos;
        }
    }
    public class GiteaOrgCreate
    {
        public string username { get; set; }
    }

    public class GogsRepListItem
    {
        public GogsRepOwner owner { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        [JsonProperty("private")]
        public bool Private { get; set; }
        public bool mirror { get; set; }
        public string clone_url { get; set; }

    }
    public class GogsRepOwner
    {
        public string username { get; set; }
    }
}
