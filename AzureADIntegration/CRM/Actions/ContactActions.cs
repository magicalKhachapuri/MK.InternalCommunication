using AzureADIntegration.Helpers.Domain;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Identity.Client;

namespace AzureADIntegration.CRM.Actions
{
    public static class ContactActions
    {
        private static string _entityLogicalName = ConfigurationManager.AppSettings["Logical_Name_Contacts"];

        public static List<Entity> GetCRMIdForAzureUsers(IOrganizationService service, List<Root> azureUsers, bool canCreateNewContacts = false)
        {
            var fetchedContacts = new List<Entity>();
            var creationRequest = new List<ContactPayload>();

            foreach (var users in azureUsers)
            {
                foreach (var user in users.value)
                {
                    var contact = GetContact(service, user.userPrincipalName);
                    if (contact != null) fetchedContacts.Add(contact);
                    else {
                        creationRequest.Add(new ContactPayload
                        {
                            Email = user.userPrincipalName
                        }); ;
                    }
                }
            }

            var count = 1;
            foreach (var contact in creationRequest)
            {
                var newContact = new Entity(_entityLogicalName);
                newContact["emailaddress1"] = contact.Email;
                newContact["firstname"] = string.Format("test {0}", count);
                newContact["lastname"] = string.Format("tset {0}", count);
                count++;

                service.Create(newContact);
                fetchedContacts.Add(newContact);
            }

            return fetchedContacts;
        }

        private static Entity GetContact(IOrganizationService service, string email)
        {
            var queryExpression = new QueryExpression(_entityLogicalName);
            queryExpression.ColumnSet = new ColumnSet(true);
            queryExpression.Criteria.AddCondition("emailaddress1", ConditionOperator.Equal, email);

            return service.RetrieveMultiple(queryExpression).Entities.SingleOrDefault();
        }
    }
}
