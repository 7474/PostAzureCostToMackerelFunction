using Koudenpa.Mackerel.Api.Api;
using Koudenpa.Mackerel.Api.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.Consumption;
using Microsoft.Azure.Management.Consumption.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PostAzureCostToMackerelFunction
{
    public static class Function
    {
        private static Authentication authentication = new Authentication();

        [FunctionName("PostAzureCostToMackerelFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] PostAzureCostToMackerelRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            log.LogInformation(JsonConvert.SerializeObject(req));

            IPage<UsageDetail> usageDetailList = await QueryUsageDetails(log, req.SubscriptionId);
            if (usageDetailList.Count() == 0)
            {
                log.LogInformation("Usage not found.");
                return new NoContentResult();
            }
            var serviceName = req.ServiceName;

            await PostServiceMetrics(log, usageDetailList, serviceName);

            return new NoContentResult();
        }

        private static async Task<IPage<UsageDetail>> QueryUsageDetails(ILogger log, string subscriptionId)
        {
            // https://docs.microsoft.com/ja-jp/rest/api/consumption/
            string token = await authentication.AcquireTokenAsync(log);
            var consumptionClient = new ConsumptionManagementClient(new TokenCredentials(token))
            {
                SubscriptionId = subscriptionId,
            };
            var usageDetailList = await consumptionClient.UsageDetails.ListAsync();
            return usageDetailList;
        }

        private static async Task PostServiceMetrics(ILogger log, IPage<UsageDetail> usageDetailList, string serviceName)
        {
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

            // openapi-gen
            var config = new Configuration();
            config.ApiKey.Add("X-Api-Key", Environment.GetEnvironmentVariable("MackerelApiKey"));
            var serviceMetricApi = new ServiceMetricApi(config);
            var serviceMetrics = costPerService.Select(x => new Koudenpa.Mackerel.Api.Model.ServiceMetricValue(
                 string.Join(".", subscriptionName, "costPerService", x.ConsumedService.Replace(".", "")), usageTime, x.PretaxCost.Value))
            .Append(new Koudenpa.Mackerel.Api.Model.ServiceMetricValue(
                 string.Join(".", subscriptionName, "totalCost"), usageTime, totalCost.Value))
            .ToList();
            serviceMetrics.ForEach(x => log.LogInformation(JsonConvert.SerializeObject(x)));
            await serviceMetricApi.PostServiceMetricAsync(serviceName, serviceMetrics);
        }

        public class PostAzureCostToMackerelRequest
        {
            public string SubscriptionId { get; set; }
            public string ServiceName { get; set; }
        }
    }
}
