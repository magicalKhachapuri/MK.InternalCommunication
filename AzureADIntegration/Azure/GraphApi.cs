using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using System.Configuration;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Web.Script.Serialization;
using System.Web.Helpers;
using AzureADIntegration.Helpers.Domain;
using AzureADIntegration.Helpers;
using AzureADIntegration.CRM.Actions;

namespace AzureADIntegration.Azure
{
    public static class GraphApi
    {
        #region Private Properties 

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static GraphServiceClient _graphServiceClient;
        private static HttpClient _httpClient;

        private static string _tenantId = ConfigurationManager.AppSettings["tenantId"];
        private static string _applicationId = ConfigurationManager.AppSettings["applicationId"];
        private static string _applicationSecret = ConfigurationManager.AppSettings["applicationSecret"];
        private static string _redirectUri = ConfigurationManager.AppSettings["redirectUri"];

        #endregion

        #region CTOR

        static GraphApi()
        {
            if (_graphServiceClient == null)
                _graphServiceClient = GetAuthenticatedGraphClient();

            if (_httpClient == null)
                _httpClient = GetAuthenticatedHTTPClient();
        }

        #endregion

        #region Private Methods 

        private static GraphServiceClient GetAuthenticatedGraphClient()
        {
            var authenticationProvider = CreateAuthorizationProvider();
            _graphServiceClient = new GraphServiceClient(authenticationProvider);
            return _graphServiceClient;
        }

        private static HttpClient GetAuthenticatedHTTPClient()
        {
            var authenticationProvider = CreateAuthorizationProvider();
            _httpClient = new HttpClient(new AuthHandler(authenticationProvider, new HttpClientHandler()));
            return _httpClient;
        }

        private static IAuthenticationProvider CreateAuthorizationProvider()
        {
            var clientId = _applicationId;
            var clientSecret = _applicationSecret.Decrypt();
            var redirectUri = _redirectUri;
            var authority = $"https://login.microsoftonline.com/{_tenantId}/v2.0";

            //this specific scope means that application will default to what is defined in the application registration rather than using dynamic scopes
            List<string> scopes = new List<string>();
            scopes.Add("https://graph.microsoft.com/.default");

            var cca = ConfidentialClientApplicationBuilder.Create(clientId)
                                                    .WithAuthority(authority)
                                                    .WithRedirectUri(redirectUri)
                                                    .WithClientSecret(clientSecret)
                                                    .Build();
            return new MsalAuthenticationProvider(cca, scopes.ToArray());
        }

        #endregion

        #region Public Methods 

        public static IGraphServiceUsersCollectionPage GetAzureADUsers()
        {
            List<QueryOption> options = new List<QueryOption>
            {
                new QueryOption("$top", "1")
            };

            var graphResult = _graphServiceClient.Users.Request().GetAsync().Result;

            return graphResult;
        }

        private static string GetAzureADUsersQueryRaw(string baseLink = "")
        {
            Uri uri = null;
            if (string.IsNullOrEmpty(baseLink))
                uri = new Uri("https://graph.microsoft.com/v1.0/users?$select=userPrincipalName&$expand=manager($select=userPrincipalName)");
            else
                uri = new Uri(baseLink);

            var httpResult = _httpClient.GetStringAsync(uri).Result;

            return httpResult;
        }

        public static List<Helpers.Domain.Root> GetAzureADUsersQuery(string baseLink = "")
        {
            Logger.Trace("Fetching users from Azure AD");
            string azureRaw = string.Empty;

            if (!string.IsNullOrEmpty(baseLink))
            {
                azureRaw = GetAzureADUsersQueryRaw(baseLink);
            }
            else
            {
                azureRaw = GetAzureADUsersQueryRaw();
            }

            var tmp = new List<Helpers.Domain.Root>();

            var data = JsonConvert.DeserializeObject<Helpers.Domain.Root>(azureRaw);
            tmp.Add(data);

            var fetchData = true;
            string nextLink = data.OdataNextLink;
            var lastData = new Helpers.Domain.Root();

            lastData = data;
            while (fetchData) // data.value > 0 
            {
                var response = GetAzureADUsersQueryRaw(lastData.OdataNextLink);
                var nextData = JsonConvert.DeserializeObject<Helpers.Domain.Root>(response);

                if (string.IsNullOrEmpty(nextData.OdataNextLink))
                {
                    fetchData = false;
                    tmp.Add(nextData);
                }
                else
                {
                    tmp.Add(nextData);
                    lastData = nextData;
                }
            }

            return tmp;

            #endregion

        }
    }
}
