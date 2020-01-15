using IdentityModel.Client;
using Newtonsoft.Json.Linq;
using Simple.OData.Client;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Example.ConsoleClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
           

            // discover endpoints from metadata
            var client = new HttpClient();

            var disco = await client.GetDiscoveryDocumentAsync("http://localhost:5000");
            if (disco.IsError)
            {
                Console.WriteLine(disco.Error);
                return;
            }

            // request token
            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret",

                Scope = "readAccess writeAccess"
            });

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                return;
            }

            Console.WriteLine(tokenResponse.Json);
            Console.WriteLine("\n\n");

            // call api
            var apiClient = new HttpClient();
            apiClient.BaseAddress = new Uri("http://localhost:5001");
            apiClient.SetBearerToken(tokenResponse.AccessToken);


            var odataClient = new ODataClient(new ODataClientSettings(apiClient, new Uri("api", UriKind.Relative))
            {
                IgnoreResourceNotFoundException = true,
                OnTrace = (x, y) => Console.WriteLine(string.Format(x, y))

            });
            var orders = await odataClient.For("Orders").Select("Customer").QueryOptions(new Dictionary<string, object> { { "api-version", 3.0 } }).FindEntriesAsync();


            var response = await apiClient.GetAsync("/api/Orders?api-version=3.0&$count=true");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(content);
            }
            Console.ReadKey();
        }
    }
}
