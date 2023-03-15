using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureADIntegration
{
    using AzureADIntegration.Azure;
    using AzureADIntegration.CRM.Actions;
    using Microsoft.Graph;
    using AzureADIntegration.Helpers;
    using AzureADIntegration.Helpers.Domain;
    using System.Web.Services.Description;
    using System.Configuration;

    class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static string _GraphQueryOdata = ConfigurationManager.AppSettings["GraphQueryOdata"];

        static void Main(string[] args)
        {
            Logger.Trace("Main method started");

            IOrganizationService service;
            try
            {
                service = CRM.CRM.GetService();

                ManagerUpdateInit(service);

                InternalCommunicationsInit(service);
            }
            catch (Exception ex)
            {
                Logger.Error($"OUTER: {ex.Message}");
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }

        static void ManagerUpdateInit(IOrganizationService service)
        {
            var usersFromAD = GraphApi.GetAzureADUsersQuery();

            var normalizedAzureADUsers = SystemUserActions.GetCRMIdForAzureUsers(service, usersFromAD);

            Update(service, normalizedAzureADUsers);
        }

        static void Update(IOrganizationService service, List<UserPayload> payload)
        {
            foreach (var user in payload)
            {
                try
                {
                    if (!user.CurrentUserManagerInAzure.Equals(Guid.Empty))
                        SystemUserActions.UpdateUser(service, user.CurrentUserId, user.CurrentUserManagerInAzure);
                }
                catch (Exception e)
                {
                    Logger.Error($"INNER: when processing: {user.CurrentUserEmail}, with ID: {user.CurrentUserId}. Error:{e.Message}, New Manager: {user.AzureManagerEmail}, New Manager Id: {user.CurrentUserManagerInAzure}");
                }
            }
        }

        static void InternalCommunicationsInit(IOrganizationService service)
        {
            var usersFromAD = GraphApi.GetAzureADUsersQuery(_GraphQueryOdata);

            var crmContacts = ContactActions.GetCRMIdForAzureUsers(service, usersFromAD, true);

            MarketingListActions.AddToMarketingList(service, crmContacts);
        }

    }
}
