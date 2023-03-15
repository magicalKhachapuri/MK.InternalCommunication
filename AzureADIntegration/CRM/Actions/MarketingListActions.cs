using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureADIntegration.CRM.Actions
{
    public class MarketingListActions
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void AddToMarketingList(IOrganizationService service, List<Entity> contacts)
        {
            var marketingList = ConfigurationManager.AppSettings["MarketingListPreProd"];

            var contactIds = contacts.Select(contact => contact.Id).ToArray();

            var addMemberListReq = new AddListMembersListRequest
            {
                MemberIds = contactIds,
                ListId = new Guid(marketingList)
            };

            service.Execute(addMemberListReq);

            Logger.Trace("Contact(s) have been added to the marketing List");
        }
    }
}
