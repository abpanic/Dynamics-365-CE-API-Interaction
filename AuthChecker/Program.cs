using Microsoft.Identity.Client;
using System.Text.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace D365AuthChecker
{
    enum AuthenticationType
    {
        Basic = 1,
        OAuth = 2
    }

    class AuthenticationChecker
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private string _instanceUrl;
        private string? _username;
        private string? _password;
        private string? _clientId;
        private string? _clientSecret;
        private AuthenticationType _authType;
        private string? _tenantId;

        public AuthenticationChecker(string instanceUrl, AuthenticationType authType, string? username = null, string? password = null, string? clientId = null, string? clientSecret = null, string? tenantId = null)
        {
            _instanceUrl = instanceUrl;
            _authType = authType;
            _username = username;
            _password = password;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _tenantId = tenantId;
        }
        public async Task<bool> ValidateCredentials()
        {
            try
            {
                if (_authType == AuthenticationType.Basic)
                {
                    if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
                    {
                        Console.WriteLine("Username and password must be provided for Basic authentication.");
                        return false;
                    }
                    return await ValidateBasicCredentials();
                }
                else if (_authType == AuthenticationType.OAuth)
                {
                    if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
                    {
                        Console.WriteLine("ClientId and ClientSecret must be provided for OAuth authentication.");
                        return false;
                    }
                    return await ValidateOAuthCredentials();
                }
                else
                {
                    throw new ArgumentException("Unsupported authentication type");
                }
            }
            catch (MsalServiceException ex)
            {
                HandleOAuthError(ex);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating credentials: {ex.Message}");
                return false;
            }
        }
        private async Task<bool> ValidateBasicCredentials()
        {
            var byteArray = Encoding.ASCII.GetBytes($"{_username}:{_password}");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var response = await httpClient.GetAsync($"{_instanceUrl}/api/data/v9.1/WhoAmI()");

            return response.IsSuccessStatusCode;
        }
        public async Task<bool> ValidateOAuthCredentials()
        {     
            var app = ConfidentialClientApplicationBuilder.Create(_clientId)
                            .WithClientSecret(_clientSecret)
                            .WithAuthority(new Uri($"https://login.microsoftonline.com/{_tenantId}/"))
                            .Build();

            var scopes = new string[] { _instanceUrl + "/.default" };
            var authResult = await app.AcquireTokenForClient(scopes).ExecuteAsync();

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

            var response = await httpClient.GetAsync($"{_instanceUrl}/api/data/v9.1/WhoAmI()");
            return response.IsSuccessStatusCode;
        }
   
        private void HandleOAuthError(MsalServiceException ex)
        {
            // Handle common OAuth errors with more detailed messages
            switch (ex.ErrorCode)
            {
                case "invalid_client":
                    Console.WriteLine("The client ID or secret is incorrect. Please verify your client ID and secret.");
                    break;
                case "unauthorized_client":
                    Console.WriteLine("The client does not have permission to perform this action. Please check your app permissions in Azure.");
                    break;
                case "invalid_grant":
                    Console.WriteLine("The provided grant is invalid or expired. This could be due to an expired refresh token or an invalid authorization code.");
                    break;
                case "invalid_request":
                    Console.WriteLine("The request is missing a required parameter, includes an unsupported parameter or parameter value, or is otherwise malformed.");
                    break;
                case "invalid_scope":
                    Console.WriteLine("The requested scope is invalid, unknown, or malformed. Please verify the requested scopes.");
                    break;
                case "access_denied":
                    Console.WriteLine("The resource owner or authorization server denied the request. This may be due to lack of consent or insufficient permissions.");
                    break;
                case "unsupported_response_type":
                    Console.WriteLine("The authorization server does not support obtaining an authorization code using this method.");
                    break;
                case "server_error":
                    Console.WriteLine("The authorization server encountered an unexpected condition. Please try again later.");
                    break;
                case "temporarily_unavailable":
                    Console.WriteLine("The authorization server is temporarily unavailable. Please try again later.");
                    break;
                case "interaction_required":
                    Console.WriteLine("The request requires user interaction. Please ensure the user is prompted for consent or login.");
                    break;
                case "expired_token":
                    Console.WriteLine("The token has expired. A new token is required.");
                    break;
                default:
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    break;
            }
            // Log detailed error information for diagnostics
            // Consider implementing logging to a file or monitoring service here
        }
        public async Task<bool> CheckPermissions(string entityName, string operation) 
        {
            try
            {
                // Setting the endpoint to query contacts
                var apiEndpoint = $"{_instanceUrl}/api/data/v9.1/contacts"; 
                    
                // FetchXML query to check for any contact records (modify according to your requirements)
                var fetchXmlQuery = @"
                <fetch top='1'>
                    <entity name='contact'>
                        <attribute name='contactid' />
                    </entity>
                </fetch>";

                // Construct the full request URL with the FetchXML query
                var requestUrl = $"{apiEndpoint}?fetchXml={Uri.EscapeDataString(fetchXmlQuery)}";

                // Make the GET request
                var response = await httpClient.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    // Assuming a successful response means there are contact records
                    Console.WriteLine("Dynamics 365 permission check success");
                    return true;
                }
                else
                {
                    // Handle other HTTP status codes appropriately. this checked for http 403
                    Console.WriteLine($"Failed to check permissions, status code: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking permissions: {ex.Message}");
                return false;
            }              
        }
        public async Task<bool> TestConnectivity()
        {
            try
            {
                var response = await httpClient.GetAsync($"{_instanceUrl}/api/data/v9.1/"); 
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Connectivity error: {ex.Message}");
                return false;
            }
        }

    }

    class Configuration
    {
        public required string Dynamics365InstanceUrl { get; set; }
        public AuthenticationType AuthType { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? TenantId { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var config = LoadConfiguration("config.json");

            var checker = new AuthenticationChecker(
                            config.Dynamics365InstanceUrl,
                            config.AuthType,
                            config.Username,
                            config.Password,
                            config.ClientId,
                            config.ClientSecret,
                            config.TenantId);

            var isValid = await checker.ValidateCredentials();
            Console.WriteLine($"Credentials are valid: {isValid}");
        }

        private static Configuration LoadConfiguration(string filePath)
        {
            var jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Configuration>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
               ?? throw new InvalidOperationException("Failed to load configuration.");
        }
    }
}
