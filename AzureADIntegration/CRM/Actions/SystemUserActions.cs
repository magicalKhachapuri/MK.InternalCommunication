using AzureADIntegration.Helpers.Domain;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceModel.Configuration;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AzureADIntegration.CRM.Actions
{
    public static class SystemUserActions
    {

        #region Private Properties 

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static string _entityLogicalName = ConfigurationManager.AppSettings["Logical_Name_SystemUsers"];

        #endregion

        #region Public Methods 

        public static List<UserPayload> GetCRMIdForAzureUsers(IOrganizationService service, List<Helpers.Domain.Root> collection)
        {
            var fetchedUserCache = new Dictionary<string, UserCache>();

            var usersPayload = new List<UserPayload>();
            var count = 0;
            foreach (var users in collection)
            {
                try
                {
                    foreach (var user in users.value)
                    {
                        var addToPayload = false;
                        var userPayload = new UserPayload();

                        // will get the current manager in CRM for this user
                        var currentUserAndManager = fetchedUserCache.ContainsKey(user.userPrincipalName) ?
                                                    fetchedUserCache[user.userPrincipalName] :
                                                    RetrieveSystemUserId(service, user.userPrincipalName);

                        if (currentUserAndManager != null && currentUserAndManager.CurrentUserId != null)
                        {
                            if (!fetchedUserCache.ContainsKey(currentUserAndManager.CurrentUserEmail))
                            {
                                fetchedUserCache.Add(currentUserAndManager.CurrentUserEmail, currentUserAndManager);
                            }

                            userPayload.CurrentUserId = currentUserAndManager.CurrentUserId;
                            userPayload.CurrentUserEmail = currentUserAndManager.CurrentUserEmail;
                            userPayload.CurrentUserFullname = currentUserAndManager.CurrentUserFullname;
                            userPayload.CurrentUserManagerInCRM = currentUserAndManager.CurrentUserManagerInCRM;

                            addToPayload = true;
                        }
                        else
                        {
                            Logger.Warn($"The User: {user.userPrincipalName} - doesn't exist in CRM");
                            addToPayload = false;
                        }

                        UserCache manager;
                        if (user.manager != null)
                        {
                            // This is the manager in Azure AD
                            manager = fetchedUserCache.ContainsKey(user.manager.userPrincipalName) ?
                                        fetchedUserCache[user.manager.userPrincipalName] :
                                        RetrieveSystemUserId(service, user.manager.userPrincipalName);

                            if (manager != null && manager.CurrentUserId != null)
                            {
                                if (!fetchedUserCache.ContainsKey(manager.CurrentUserEmail))
                                {
                                    fetchedUserCache.Add(manager.CurrentUserEmail, manager);
                                }

                                userPayload.CurrentUserManagerInAzure = manager.CurrentUserId;
                                userPayload.AzureManagerEmail = manager.CurrentUserEmail;
                                userPayload.AzureManagerFullname = manager.CurrentUserFullname;
                            }
                            else
                                addToPayload = false;
                        }

                        if (addToPayload && userPayload.UpdateRequired)
                        {
                            count++;
                            Logger.Trace($"User: {userPayload.CurrentUserFullname}, IsUpdateNeeded: {userPayload.UpdateRequired}, newManager: {userPayload.AzureManagerFullname}, {count}-TH record");

                            usersPayload.Add(userPayload);

                            if (count % 100 == 0)
                                Console.Clear();
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"GetCRMIdForAzureUsers: {e.Message}");
                }
            }

            return usersPayload;
        }

        public static void UpdateUser(IOrganizationService service, Guid userGuid, Guid newManagerGuid)
        {
            var userToUpdate = new Entity(_entityLogicalName);
            userToUpdate.Id = userGuid;
            userToUpdate["parentsystemuserid"] = new EntityReference(_entityLogicalName, newManagerGuid);

            service.Update(userToUpdate);
        }

        #endregion

        #region Private Methods 

        private static UserCache RetrieveSystemUserId(IOrganizationService service, string principalName) // same as the username 
        {
            var fetch = string.Format(@"<fetch distinct='false' mapping='logical' version='1.0'>
                                            <entity name='systemuser'>
                                            <attribute name='domainname' />
                                            <attribute name='fullname' />
                                            <attribute name='parentsystemuserid' />
                                            <filter type='and'>
                                            <condition attribute='domainname' value='{0}' operator='eq'/>
                                            </filter>
                                            </entity>
                                            </fetch>", principalName);

            var fetchRes = service.RetrieveAllRecordsWithFetchNew(fetch);

            if (fetchRes.Count == 1)
            {
                var user = fetchRes.SingleOrDefault();

                var userNormalized = new UserCache()
                {
                    CurrentUserId = user.Id,
                    CurrentUserEmail = user.GetAttributeValue<string>("domainname"),
                    CurrentUserFullname = user.GetAttributeValue<string>("fullname")
                };

                var currentUserManagerInCrm = user.GetAttributeValue<EntityReference>("parentsystemuserid");
                userNormalized.CurrentUserManagerInCRM = currentUserManagerInCrm != null ? currentUserManagerInCrm.Id : Guid.Empty;

                return userNormalized;
            }

            return null;
        }

        #endregion

    }
}
