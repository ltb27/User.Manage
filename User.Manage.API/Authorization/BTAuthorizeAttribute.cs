using Microsoft.AspNetCore.Authorization;

namespace User.Manage.API.Authorization
{
    public class BTAuthorizeAttribute : AuthorizeAttribute
    {
        public BTAuthorizeAttribute(string permission)
        {
            Permission = permission;
        }

        public string Permission { get; set; }
    }
}
