using IdentityServer4.Models;
using IdentityServer4.Validation;
using System.Threading.Tasks;

namespace AuthService
{
    public class RedirectUriValidator : IRedirectUriValidator
    {
        /// TODO: NEVER USE IN PRODUCTION
        public Task<bool> IsPostLogoutRedirectUriValidAsync(string requestedUri, Client client)
        {
            return Task.FromResult(true);
        }

        /// TODO: NEVER USE IN PRODUCTION
        public Task<bool> IsRedirectUriValidAsync(string requestedUri, Client client)
        {
            return Task.FromResult(true);
        }
    }
}
