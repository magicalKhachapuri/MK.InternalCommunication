using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureADIntegration.Helpers.Domain
{
    public class UserCache
    {
        public Guid CurrentUserId { get; set; }
        public string CurrentUserEmail { get; set; }
        public string CurrentUserFullname { get; set; }
        public Guid CurrentUserManagerInCRM { get; set; }
    }
}
