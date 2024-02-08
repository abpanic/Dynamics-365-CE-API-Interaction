using Microsoft.Identity.Client;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // ... (Same authentication details as in the previous example)

        // Function to check if the token is expired
        bool IsTokenExpired(AuthenticationResult tokenResult)
        {
            return tokenResult.ExpiresOn <= DateTimeOffset.UtcNow;
        }

        // Function to refresh the token
        async Task<string> RefreshTokenAsync(IConfidentialClientApplication app, AuthenticationResult tokenResult)
        {
            return (await app.AcquireTokenForClient(new string[] { resourceUrl + "/.default" })
                .ExecuteAsync()).AccessToken;
        }

        // ... (Same code for acquiring initial token as in the previous example)

        // Example: Retrieve Account records
        string apiUrl = $"{resourceUrl}/api/data/v9.0/accounts";
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

            // Check if token is expired and refresh if necessary
            if (IsTokenExpired(result))
            {
                Console.WriteLine("Token expired. Refreshing...");
                result = await app.AcquireTokenForClient(new string[] { resourceUrl + "/.default" })
                    .ExecuteAsync();
                Console.WriteLine("Token refreshed.");
            }

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
