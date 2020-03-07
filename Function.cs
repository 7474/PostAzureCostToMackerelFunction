using Koudenpa.Mackerel.Api.Api;
using Koudenpa.Mackerel.Api.Client;
using Koudenpa.Mackerel.Api.Model;
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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PostAzureCostToMackerelFunction
{
    public static class Function
    {
        public class PostAzureCostToMackerelRequest
        {
            public string SubscriptionId { get; set; }
            public string ServiceName { get; set; }
        }

        private static Authentication authentication = new Authentication();

        [FunctionName("PostAzureCostToMackerelFunction")]
        public static async Task<IActionResult> HttpTrigger(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] PostAzureCostToMackerelRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            await Run(req, log);
            return new NoContentResult();
        }

        private static async Task Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] PostAzureCostToMackerelRequest req,
            ILogger log)
        {
            log.LogInformation("PostAzureCostToMackerelFunction.Function.Run");
            log.LogInformation(JsonConvert.SerializeObject(req));

            var usageDetailList = await QueryUsageDetails(log, req.SubscriptionId);
            if (usageDetailList.Count() == 0)
            {
                log.LogInformation("Usage not found.");
                return;
            }
            var serviceName = req.ServiceName;
            var serviceMetrics = ToServiceMetrics(usageDetailList);

            serviceMetrics.ToList().ForEach(x => log.LogInformation(JsonConvert.SerializeObject(x)));

            await PostServiceMetrics(serviceMetrics, serviceName);
        }

        private static async Task<IList<UsageDetail>> QueryUsageDetails(ILogger log, string subscriptionId)
        {
            // https://docs.microsoft.com/ja-jp/rest/api/consumption/
            string token = await authentication.AcquireTokenAsync(log);
            var consumptionClient = new ConsumptionManagementClient(new TokenCredentials(token))
            {
                SubscriptionId = subscriptionId,
            };
            var usageDetailList = await consumptionClient.UsageDetails.ListAsync();
            return usageDetailList.ToList();
        }

        private static IEnumerable<ServiceMetricValue> ToServiceMetrics(IEnumerable<UsageDetail> usageDetailList)
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

            var subscriptionName = usageDetailList.First().SubscriptionName;
            var usageTime = usageTimestamp.Value.ToUniversalTime().AddYears(-1969).Ticks / 10000000;
            var serviceMetrics = costPerService.Select(x => new Koudenpa.Mackerel.Api.Model.ServiceMetricValue(
                     string.Join(".", subscriptionName, "costPerService", x.ConsumedService.Replace(".", "")), usageTime, x.PretaxCost.Value))
                .Append(new Koudenpa.Mackerel.Api.Model.ServiceMetricValue(
                     string.Join(".", subscriptionName, "totalCost"), usageTime, totalCost.Value))
                .ToList();

            return serviceMetrics;
        }

        private static async Task PostServiceMetrics(IEnumerable<ServiceMetricValue> serviceMetrics, string serviceName)
        {
            var config = new Configuration();
            config.ApiKey.Add("X-Api-Key", Environment.GetEnvironmentVariable("MackerelApiKey"));
            var serviceMetricApi = new ServiceMetricApi(config);
            await serviceMetricApi.PostServiceMetricAsync(serviceName, serviceMetrics.ToList());
        }
    }
}
