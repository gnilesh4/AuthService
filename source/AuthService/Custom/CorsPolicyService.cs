using IdentityServer4.Services;
using System.Threading.Tasks;

namespace AuthService
{
    public class CorsPolicyService : ICorsPolicyService
    {
        /// TODO: NEVER USE IN PRODUCTION
        public Task<bool> IsOriginAllowedAsync(string origin)
        {
            return Task.FromResult(true);
        }
    }
}
