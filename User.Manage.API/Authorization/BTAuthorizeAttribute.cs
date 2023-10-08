using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace User.Manage.API.Authorization
{
    public class BTAuthorizeAttribute : AuthorizeAttribute
    {
        public BTAuthorizeAttribute()
        {
            //Choose authentication scheme which you want to use to authenticate to construct user identity information for this authorizatio
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme;
            // Choose Policy which you want to use to authorize for this resource
            Policy = "BTCheckPermission";
        }

        public string? Permission { get; set; }
    }
}
