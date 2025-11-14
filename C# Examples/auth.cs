// Synopsis: Demonstrates client-credentials authentication against Dataverse using MSAL
// and performs a simple GET on /accounts to show authenticated access.
using Microsoft.Identity.Client;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Authentication details (replace with your own)
        string clientId = "your_client_id";
        string clientSecret = "your_client_secret";
        string tenantId = "your_tenant_id";
        string resourceUrl = "https://yourorganization.crm.dynamics.com";

        // Create a Confidential Client Application
        IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
            .Build();

        // Acquire token
        var result = await app.AcquireTokenForClient(new string[] { resourceUrl + "/.default" })
            .ExecuteAsync();

        // Example: Retrieve Account records
        string apiUrl = $"{resourceUrl}/api/data/v9.0/accounts";
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

            // Display retrieved data
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Retrieved Data: {content}");
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
    }
}
