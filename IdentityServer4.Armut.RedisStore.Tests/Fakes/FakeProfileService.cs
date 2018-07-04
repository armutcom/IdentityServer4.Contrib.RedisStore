using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;

namespace IdentityServer4.Armut.RedisStore.Tests.Fakes
{
    public class FakeProfileService : IProfileService
    {
        public IEnumerable<Claim> Claims = new List<Claim>();

        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            context.IssuedClaims = Claims.ToList();
            return Task.CompletedTask;
        }

        public Action<IsActiveContext> IsActive;

        public Task IsActiveAsync(IsActiveContext context)
        {
            IsActive?.Invoke(context);
            return Task.CompletedTask;
        }
    }
}
