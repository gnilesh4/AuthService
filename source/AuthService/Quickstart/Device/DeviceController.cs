using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServerHost.Quickstart.UI
{
    [Authorize]
    [SecurityHeaders]
    public class DeviceController : Controller
    {
        private readonly IEventService _eventService;
        private readonly IDeviceFlowInteractionService _interactionService;
        private readonly IOptions<IdentityServerOptions> _options;

        public DeviceController
        (
            IEventService eventService,
            IDeviceFlowInteractionService interactionService,
            IOptions<IdentityServerOptions> options
        )
        {
            _eventService = eventService;
            _interactionService = interactionService;
            _options = options;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Callback(DeviceAuthorizationInputModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var result = await ProcessConsent(model);

            return View(result.HasValidationError ? "Error" : "Success");
        }

        public ScopeViewModel CreateScopeViewModel(ParsedScopeValue parsedScopeValue, ApiScope apiScope, bool check)
        {
            return new ScopeViewModel
            {
                Value = parsedScopeValue.RawValue,
                DisplayName = apiScope.DisplayName ?? apiScope.Name,
                Description = apiScope.Description,
                Emphasize = apiScope.Emphasize,
                Required = apiScope.Required,
                Checked = check || apiScope.Required
            };
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userCode = Request.Query[_options.Value.UserInteraction.DeviceVerificationUserCodeParameter];

            if (string.IsNullOrWhiteSpace(userCode))
            {
                return View("UserCodeCapture");
            }

            var vm = await BuildViewModelAsync(userCode);

            if (vm == null)
            {
                return View("Error");
            }

            vm.ConfirmUserCode = true;

            return View("UserCodeConfirmation", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserCodeCapture(string userCode)
        {
            var vm = await BuildViewModelAsync(userCode);

            return vm == null ? View("Error") : View("UserCodeConfirmation", vm);
        }

        private async Task<DeviceAuthorizationViewModel> BuildViewModelAsync(string userCode, DeviceAuthorizationInputModel model = null)
        {
            var request = await _interactionService.GetAuthorizationContextAsync(userCode);

            return request != null ? CreateConsentViewModel(userCode, model, request) : null;
        }

        private DeviceAuthorizationViewModel CreateConsentViewModel(string userCode, DeviceAuthorizationInputModel model, DeviceFlowAuthorizationRequest request)
        {
            var vm = new DeviceAuthorizationViewModel
            {
                UserCode = userCode,
                Description = model?.Description,
                RememberConsent = model?.RememberConsent ?? true,
                ScopesConsented = model?.ScopesConsented ?? Enumerable.Empty<string>(),
                ClientName = request.Client.ClientName ?? request.Client.ClientId,
                ClientUrl = request.Client.ClientUri,
                ClientLogoUrl = request.Client.LogoUri,
                AllowRememberConsent = request.Client.AllowRememberConsent
            };

            vm.IdentityScopes = request.ValidatedResources.Resources.IdentityResources.Select(resource => CreateScopeViewModel(resource, vm.ScopesConsented.Contains(resource.Name) || model == null)).ToArray();

            var apiScopes = new List<ScopeViewModel>();

            foreach (var parsedScope in request.ValidatedResources.ParsedScopes)
            {
                var apiScope = request.ValidatedResources.Resources.FindApiScope(parsedScope.ParsedName);

                if (apiScope == null)
                {
                    continue;
                }

                var scope = CreateScopeViewModel(parsedScope, apiScope, vm.ScopesConsented.Contains(parsedScope.RawValue) || model == null);

                apiScopes.Add(scope);
            }

            if (ConsentOptions.EnableOfflineAccess && request.ValidatedResources.Resources.OfflineAccess)
            {
                apiScopes.Add(GetOfflineAccessScope(vm.ScopesConsented.Contains(IdentityServerConstants.StandardScopes.OfflineAccess) || model == null));
            }

            vm.ApiScopes = apiScopes;

            return vm;
        }

        private static ScopeViewModel CreateScopeViewModel(IdentityResource identity, bool check)
        {
            return new ScopeViewModel
            {
                Value = identity.Name,
                DisplayName = identity.DisplayName ?? identity.Name,
                Description = identity.Description,
                Emphasize = identity.Emphasize,
                Required = identity.Required,
                Checked = check || identity.Required
            };
        }

        private static ScopeViewModel GetOfflineAccessScope(bool check)
        {
            return new ScopeViewModel
            {
                Value = IdentityServerConstants.StandardScopes.OfflineAccess,
                DisplayName = ConsentOptions.OfflineAccessDisplayName,
                Description = ConsentOptions.OfflineAccessDescription,
                Emphasize = true,
                Checked = check
            };
        }

        private async Task<ProcessConsentResult> ProcessConsent(DeviceAuthorizationInputModel model)
        {
            var result = new ProcessConsentResult();

            var request = await _interactionService.GetAuthorizationContextAsync(model.UserCode);

            if (request == null)
            {
                return result;
            }

            ConsentResponse consentResponse = null;

            if (model.Button == "no")
            {
                consentResponse = new ConsentResponse { Error = AuthorizationError.AccessDenied };

                await _eventService.RaiseAsync(new ConsentDeniedEvent(User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues));
            }
            else if (model.Button == "yes")
            {
                if (model.ScopesConsented != null && model.ScopesConsented.Any())
                {
                    var scopes = model.ScopesConsented;

                    if (ConsentOptions.EnableOfflineAccess == false)
                    {
                        scopes = scopes.Where(scope => scope != IdentityServerConstants.StandardScopes.OfflineAccess);
                    }

                    consentResponse = new ConsentResponse
                    {
                        RememberConsent = model.RememberConsent,
                        ScopesValuesConsented = scopes.ToArray(),
                        Description = model.Description
                    };

                    await _eventService.RaiseAsync(new ConsentGrantedEvent(User.GetSubjectId(), request.Client.ClientId, request.ValidatedResources.RawScopeValues, consentResponse.ScopesValuesConsented, consentResponse.RememberConsent));
                }
                else
                {
                    result.ValidationError = ConsentOptions.MustChooseOneErrorMessage;
                }
            }
            else
            {
                result.ValidationError = ConsentOptions.InvalidSelectionErrorMessage;
            }

            if (consentResponse != null)
            {
                await _interactionService.HandleRequestAsync(model.UserCode, consentResponse);

                result.RedirectUri = model.ReturnUrl;

                result.Client = request.Client;
            }
            else
            {
                result.ViewModel = await BuildViewModelAsync(model.UserCode, model);
            }

            return result;
        }
    }
}
