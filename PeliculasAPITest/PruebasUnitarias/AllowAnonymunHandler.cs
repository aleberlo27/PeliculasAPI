using Microsoft.AspNetCore.Authorization;

namespace PeliculasAPITest.PruebasUnitarias
{
    public class AllowAnonymunHandler : IAuthorizationHandler
    {
        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            foreach (var requirement in context.PendingRequirements.ToList())
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
