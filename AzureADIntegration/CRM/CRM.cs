using AzureADIntegration.Helpers;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;


namespace AzureADIntegration.CRM
{
    static class CRM
    {

        #region Private Properties 

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static string _OrgName = ConfigurationManager.AppSettings["OrgName"];
        private static string _OrgZone = ConfigurationManager.AppSettings["OrgZone"];
        private static string _ClientId = ConfigurationManager.AppSettings["ClientId"];
        private static string _ClientSecret = ConfigurationManager.AppSettings["ClientSecret"];

        private static CrmServiceClient _crmConnection;
        private static IOrganizationService _organizationService;

        #endregion

        #region Public Methods 

        public static void TestConnection(this IOrganizationService service)
        {
            Guid userid = ((WhoAmIResponse)service.Execute(new WhoAmIRequest())).UserId;

            if (userid == Guid.Empty)
            {
                throw new Exception("Connection is not established with the CRM");
            }

            Logger.Trace("Successfully got the CRM SERVICE");
        }

        public static IOrganizationService GetService()
        {
            Logger.Trace("Getting CRM service");
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var connectionString = string.Format(@"AuthType=ClientSecret;Url=https://{0}.{1}.dynamics.com;ClientId={2};ClientSecret={3}", _OrgName, _OrgZone, _ClientId, _ClientSecret.Decrypt());
            if (_crmConnection == null)
            {
                _crmConnection = new CrmServiceClient(connectionString);
            }

            _organizationService = (IOrganizationService)_crmConnection.OrganizationWebProxyClient != null ? (IOrganizationService)_crmConnection.OrganizationWebProxyClient : (IOrganizationService)_crmConnection.OrganizationServiceProxy;

            if (_organizationService == null)
                return null;
            else
                _organizationService.TestConnection();

            return _organizationService;
        }

        public static List<Entity> RetrieveAllRecords(this IOrganizationService service, QueryExpression query)
        {
            var pageNumber = 1;
            var queryCount = 200;
            var pagingCookie = string.Empty;
            var result = new List<Entity>();
            EntityCollection resp;
            do
            {
                if (pageNumber != 1)
                {
                    query.PageInfo.PageNumber = pageNumber;
                    query.PageInfo.PagingCookie = pagingCookie;
                    query.PageInfo.Count = queryCount;
                }
                resp = service.RetrieveMultiple(query);
                if (resp.MoreRecords)
                {
                    pageNumber++;
                    pagingCookie = resp.PagingCookie;
                }
                result.AddRange(resp.Entities);
            }
            while (resp.MoreRecords);

            return result;
        }

        public static List<Entity> RetrieveAllRecordsWithFetch(this IOrganizationService service, string fetch)
        {
            var conversionRequest = new FetchXmlToQueryExpressionRequest
            {
                FetchXml = fetch
            };

            var conversionResponse =
                (FetchXmlToQueryExpressionResponse)service.Execute(conversionRequest);

            QueryExpression queryExpression = conversionResponse.Query;

            var pageNumber = 1;
            var queryCount = 200;
            var pagingCookie = string.Empty;
            var result = new List<Entity>();
            EntityCollection resp;
            do
            {
                if (pageNumber != 1)
                {
                    queryExpression.PageInfo.PageNumber = pageNumber;
                    queryExpression.PageInfo.PagingCookie = pagingCookie;
                    queryExpression.PageInfo.Count = queryCount;
                }
                resp = service.RetrieveMultiple(queryExpression);
                if (resp.MoreRecords)
                {
                    pageNumber++;
                    pagingCookie = resp.PagingCookie;
                }
                result.AddRange(resp.Entities);
            }
            while (resp.MoreRecords);

            return result;
        }

        public static List<Entity> RetrieveAllRecordsWithFetchNew(this IOrganizationService service, string fetch, int fetchCount = 200)
        {
            var records = new List<Entity>();

            string pagingCookie = null;
            int pageNumber = 1;
            int recordsCount = 0;

            while (true)
            {
                var res = service.RetrieveMultiple(new FetchExpression(CreateXml(fetch, pagingCookie, pageNumber, fetchCount)));

                records.AddRange(res.Entities);

                if (res.MoreRecords)
                {
                    pageNumber++;
                    pagingCookie = res.PagingCookie;
                }
                else
                {
                    break;
                }
            }

            return records;
        }

        #endregion

        #region Private Methods

        private static string CreateXml(string xml, string cookie, int page, int count)
        {
            StringReader stringReader = new StringReader(xml);
            var reader = new XmlTextReader(stringReader);

            // Load document
            XmlDocument doc = new XmlDocument();
            doc.Load(reader);

            XmlAttributeCollection attrs = doc.DocumentElement.Attributes;

            if (cookie != null)
            {
                XmlAttribute pagingAttr = doc.CreateAttribute("paging-cookie");
                pagingAttr.Value = cookie;
                attrs.Append(pagingAttr);
            }

            XmlAttribute pageAttr = doc.CreateAttribute("page");
            pageAttr.Value = System.Convert.ToString(page);
            attrs.Append(pageAttr);

            XmlAttribute countAttr = doc.CreateAttribute("count");
            countAttr.Value = System.Convert.ToString(count);
            attrs.Append(countAttr);

            StringBuilder sb = new StringBuilder(1024);
            StringWriter stringWriter = new StringWriter(sb);

            XmlTextWriter writer = new XmlTextWriter(stringWriter);
            doc.WriteTo(writer);
            writer.Close();

            return sb.ToString();
        }

        #endregion

    }
}
