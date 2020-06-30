using IdentityModel.Client;
using IdentityServer4;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AuthService
{
    [Route("/api/tests")]
    public class TestsController : ControllerBase
    {
        private static readonly string url = "https://localhost:5000";

        public async Task<IActionResult> Get()
        {
            var token = await GetClientCredentialsTokenAsync();

            var http = new HttpClient();

            http.SetBearerToken(token);

            var response = await http.GetStringAsync($"{url}/api/tests/date");

            return Ok(response);
        }

        [Authorize(IdentityServerConstants.LocalApi.PolicyName)]
        [Route("date")]
        public IActionResult GetDate()
        {
            return Ok(DateTime.Now);
        }

        private static async Task<string> GetClientCredentialsTokenAsync()
        {
            var http = new HttpClient();

            var disco = await http.GetDiscoveryDocumentAsync(url);

            var request = new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = "clientcredentials",
                ClientSecret = "secret",
                Scope = IdentityServerConstants.LocalApi.ScopeName
            };

            var response = await http.RequestClientCredentialsTokenAsync(request);

            return response.AccessToken;
        }
    }
}
