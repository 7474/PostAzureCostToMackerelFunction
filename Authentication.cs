using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;

namespace PostAzureCostToMackerelFunction
{
    public class Authentication
    {
        private TokenCache tokenCache;
        private AuthenticationContext authenticationContext;
        private ClientCredential clientCredential;

        public Authentication()
        {
            var aadDomain = Environment.GetEnvironmentVariable("AADDomain");
            var clientId = Environment.GetEnvironmentVariable("ClientId");
            var clientSecret = Environment.GetEnvironmentVariable("ClientSecret");

            tokenCache = new TokenCache();
            authenticationContext = new AuthenticationContext(
               string.Format("{0}/{1}", "https://login.microsoftonline.com", aadDomain),
               tokenCache);
            clientCredential = new ClientCredential(clientId, clientSecret);
        }

        public async Task<string> AcquireTokenAsync()
        {
            var result = await authenticationContext.AcquireTokenAsync(
                "https://management.azure.com/", clientCredential);

            return result.AccessToken;
        }
    }
}
