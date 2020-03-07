# PostAzureCostToMackerelFunction

An Azure Function that posts the cost of 
[Miscrosoft Azure](https://azure.microsoft.com/) collected using the 
[Azure Consumption API](https://docs.microsoft.com/ja-jp/rest/api/consumption/) as a 
[service metric of Mackerel](https://mackerel.io/ja/api-docs/entry/service-metrics#post).

## Usage

1. Create Function App.
    - You can use [ARM templates](arm/template.json).
      This template configure [Management ID](https://docs.microsoft.com/ja-jp/azure/active-directory/managed-identities-azure-resources/overview).
1. [Add Role to Application](https://docs.microsoft.com/ja-jp/azure/role-based-access-control/role-assignments-portal#add-a-role-assignment)
    - e.g. [Cost Management Reader](https://docs.microsoft.com/ja-jp/azure/role-based-access-control/built-in-roles#cost-management-reader)
1. Deploy the Function App by any way. 
1. Execute the function every day.
    - Azure costs are updated daily.

