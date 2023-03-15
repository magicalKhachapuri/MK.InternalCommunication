using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureADIntegration.Helpers.Domain
{
    public class UserPayload
    {
        public Guid CurrentUserId { get; set; }
        public string CurrentUserEmail { get; set; }
        public string CurrentUserFullname { get; set; }
        public Guid CurrentUserManagerInCRM { get; set; }
        public Guid CurrentUserManagerInAzure { get; set; }
        public string AzureManagerEmail { get; set; }
        public string AzureManagerFullname { get; set; }

        public bool UpdateRequired
        {
            get
            {
                return !CurrentUserManagerInAzure.Equals(CurrentUserManagerInCRM);
            }
        }

    }
}
