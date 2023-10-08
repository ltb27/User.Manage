using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
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

        private async Task UnauthorizedHandler(
            AuthorizationHandlerContext context,
            HttpContext httpContext
        )
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.CompleteAsync();
            context.Fail();
            return;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            CheckPermissionRequirement requirement
        )
        {
            // todo: Logic to handler Authorization for resource
            // every time task complete and requirement is met context.Succeed(requirement); => user has permission to archive the resource, otherwise not be able to do so
            if (context.Resource is null)
            {
                if (context.User.Identity!.IsAuthenticated)
                    context.Succeed(requirement);
                return;
            }

            var httpContext = context.Resource as HttpContext;

            if (httpContext == null)
            {
                context.Fail();
                return;
            }

            if (!httpContext.User.Identity!.IsAuthenticated)
                await UnauthorizedHandler(context, httpContext);

            var securityStamp = context.User.Claims
                .Where(x => x.Type == "AspNet.Identity.SecurityStamp")
                .Select(x => x.Value)
                .FirstOrDefault();
            var userName = httpContext.User.Identity.Name;

            var isUserOk = await db.Users.AnyAsync(
                x =>
                    x.UserName == userName
                    && x.SecurityStamp == securityStamp
                    && x.LockoutEnd == null
            );

            if (!isUserOk)
                await UnauthorizedHandler(context, httpContext);

            if (httpContext.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            var customAttribute = httpContext
                .GetEndpoint()
                ?.Metadata.GetOrderedMetadata<BTAuthorizeAttribute>();

            if (!(customAttribute?.Any() == true))
                return;

            var requiredPermissions = customAttribute
                .Where(x => x.Permission != null)
                .SelectMany(
                    x =>
                        x.Permission!
                            .Split(
                                new char[] { ',', '|', ';' },
                                StringSplitOptions.RemoveEmptyEntries
                            )
                            .ToList()
                )
                .ToList();

            var isUserHasRequiredPermission = await db.Users.AnyAsync(
                x =>
                    x.UserName == userName
                    && x.Roles.Any(
                        xx =>
                            xx.Role.Claims.Any(
                                cl =>
                                    cl.ClaimType == "permission"
                                    && requiredPermissions.Contains(cl.ClaimValue)
                            )
                    )
            );

            if (!isUserHasRequiredPermission)
                return;

            context.Succeed(requirement);
        }
    }
}
