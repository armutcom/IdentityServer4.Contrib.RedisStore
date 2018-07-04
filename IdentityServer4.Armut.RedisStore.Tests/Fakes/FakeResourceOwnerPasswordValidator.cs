using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Validation;

namespace IdentityServer4.Armut.RedisStore.Tests.Fakes
{
    class FakeResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            context.Result = new GrantValidationResult(subject: "1",
                authenticationMethod: OidcConstants.AuthenticationMethods.Password,
                claims: new List<Claim> { });

            return Task.CompletedTask;
        }
    }
}
