using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using Microsoft.Azure.Management.Consumption;
using Microsoft.Rest;
using Koudenpa.Mackerel.Api.Api;
using Koudenpa.Mackerel.Api.Client;

namespace PostAzureCostToMackerelFunction
{
    public static class Function
    {
        [FunctionName("PostAzureCostToMackerelFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] PostAzureCostToMackerelRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            log.LogInformation(JsonConvert.SerializeObject(req));

            try
            {
                string token = await AcquireTokenAsync();
                var consumptionClient = new ConsumptionManagementClient(new TokenCredentials(token))
                {
                    SubscriptionId = req.SubscriptionId,
                };
                var usageDetailList = await consumptionClient.UsageDetails.ListAsync();
                if (usageDetailList.Count() == 0)
                {
                    log.LogInformation("Usage not found.");
                    return new NoContentResult();
                }
                var usageTimestamp = usageDetailList.Max(x => x.UsageEnd);
                var totalCost = usageDetailList.Sum(x => x.PretaxCost);
                // XXX resourceGroup ない？　APIリファレンス的には返していそうだけれど。
                // https://docs.microsoft.com/ja-jp/rest/api/consumption/usagedetails/list
                //var costPerGroup = usageDetailList.GroupBy(x => x.ResoupGroup)
                var costPerService = usageDetailList
                    .GroupBy(x => x.ConsumedService)
                    .Select(x => new
                    {
                        ConsumedService = x.Key,
                        PretaxCost = x.Sum(y => y.PretaxCost)
                    })
                    .ToList();

                log.LogInformation($"{usageTimestamp}: { totalCost}");
                costPerService.ForEach(x => log.LogInformation($"{x.ConsumedService}: {x.PretaxCost}"));

                var subscriptionName = usageDetailList.First().SubscriptionName;
                var usageTime = usageTimestamp.Value.ToUniversalTime().AddYears(-1969).Ticks / 10000000;

                // XXX Could not load file or assembly 'System.ComponentModel.Annotations, Version=4.3.1.0
                // 生成コードの参照ライブラリとランタイムの参照ライブラリのバージョンが合わない様子。
                //var mackerelHttpClient = new HttpClient();
                //mackerelHttpClient.DefaultRequestHeaders.Add("X-Api-Key", Environment.GetEnvironmentVariable("MackerelApiKey"));
                //var mackerel = new mackerel_apiClient(mackerelHttpClient);
                //var metrics = costPerService.Select(x => new ServiceMetricValue()
                //{
                //    Time = usageTime,
                //    Name = string.Join(".", subscriptionName, "costPerService", x.ConsumedService),
                //    Value = decimal.ToDouble(x.PretaxCost.Value)
                //}).Append(new ServiceMetricValue()
                //{
                //    Time = usageTime,
                //    Name = string.Join(".", subscriptionName, "totalCost"),
                //    Value = decimal.ToDouble(totalCost.Value)
                //});
                //await mackerel.PostServiceMetricAsync(req.ServiceName, metrics);

                // openapi-gen
                var config = new Configuration();
                config.ApiKey.Add("X-Api-Key", Environment.GetEnvironmentVariable("MackerelApiKey"));
                var serviceMetricApi = new ServiceMetricApi(config);
                var serviceMetrics = costPerService.Select(x => new Koudenpa.Mackerel.Api.Model.ServiceMetricValue(
                     string.Join(".", subscriptionName, "costPerService", x.ConsumedService), usageTime, x.PretaxCost.Value))
                .Append(new Koudenpa.Mackerel.Api.Model.ServiceMetricValue(
                     string.Join(".", subscriptionName, "totalCost"), usageTime, totalCost.Value))
                .ToList();
                serviceMetrics.ForEach(x => log.LogInformation(JsonConvert.SerializeObject(x)));
                await serviceMetricApi.PostServiceMetricAsync(req.ServiceName, serviceMetrics);
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                throw;
            }

            return new NoContentResult();
        }

        public class PostAzureCostToMackerelRequest
        {
            public string SubscriptionId { get; set; }
            public string ServiceName { get; set; }
        }

        static TokenCache tokenCache = new TokenCache();
        public static async Task<string> AcquireTokenAsync()
        {
            var aadDomain = Environment.GetEnvironmentVariable("AADDomain");
            var clientId = Environment.GetEnvironmentVariable("ClientId");
            var clientSecret = Environment.GetEnvironmentVariable("ClientSecret");

            var authenticationContext = new AuthenticationContext(
                string.Format("{0}/{1}", "https://login.microsoftonline.com", aadDomain),
                tokenCache);

            var result = await authenticationContext.AcquireTokenAsync(
                "https://management.azure.com/",
                new ClientCredential(
                    clientId,
                    clientSecret
                ));

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            return result.AccessToken;
        }
    }
}
