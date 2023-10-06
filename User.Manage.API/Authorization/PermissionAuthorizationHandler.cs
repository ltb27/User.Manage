using Microsoft.AspNetCore.Authorization;
using User.Manage.API.Models;

namespace User.Manage.API.Authorization
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<CheckPermissionRequirement>
    {
        private readonly ApplicationDbContext db;

        public PermissionAuthorizationHandler(ApplicationDbContext db)
        {
            this.db = db;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            CheckPermissionRequirement requirement
        )
        {
            // todo: Logic to handler Authorization for resource
            // every time task complete and requirement is met context.Succeed(requirement); => user has permission to archive the resource, otherwise not be able to do so
            if (context.Resource is null)
            {
                if (context.User.Identity.IsAuthenticated)
                    context.Succeed(requirement);
            }
            else
            {
                var authorizedResource =
                    context.Resource as Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext;

                if (authorizedResource is not null && context.User.Identity.IsAuthenticated)
                {
                    // OK if admin role is in the user's role list
                    if (context.User.IsInRole("Admin"))
                    {
                        context.Succeed(requirement);
                    }
                    else
                    {
                        // Handler for resource specific permission
                    }
                }
            }
            return Task.CompletedTask;
        }
    }
}
