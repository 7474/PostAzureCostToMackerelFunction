using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;

namespace PostAzureCostToMackerelFunction
{
    public class Authentication
    {
        private AzureServiceTokenProvider azureServiceTokenProvider;
        private TokenCache tokenCache;
        private AuthenticationContext authenticationContext;
        private ClientCredential clientCredential;

        public Authentication()
        {
            // �}�l�[�W�hID�ł̔F�؂��\������B
            azureServiceTokenProvider = new AzureServiceTokenProvider();

            // �t�F�C���o�b�N��Ƃ���ADAL�ł̔F�؂��\������B
            var aadDomain = Environment.GetEnvironmentVariable("AADDomain") ?? "dumy";
            var clientId = Environment.GetEnvironmentVariable("ClientId") ?? "dumy";
            var clientSecret = Environment.GetEnvironmentVariable("ClientSecret") ?? "dumy";

            tokenCache = new TokenCache();
            authenticationContext = new AuthenticationContext(
               string.Format("{0}/{1}", "https://login.microsoftonline.com", aadDomain),
               tokenCache);
            clientCredential = new ClientCredential(clientId, clientSecret);
        }

        public async Task<string> AcquireTokenAsync(ILogger log)
        {
            try
            {
                string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync(
                    "https://management.azure.com/");
                return accessToken;
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                var result = await authenticationContext.AcquireTokenAsync(
                    "https://management.azure.com/", clientCredential);
                return result.AccessToken;
            }
        }
    }
}
