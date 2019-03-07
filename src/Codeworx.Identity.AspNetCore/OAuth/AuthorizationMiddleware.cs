﻿using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Codeworx.Identity.OAuth;
using Microsoft.AspNetCore.Http;

namespace Codeworx.Identity.AspNetCore.OAuth
{
    public class AuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Configuration.IdentityService _service;
        private readonly IRequestBinder<AuthorizationRequest, AuthorizationErrorResponse> _authorizationRequestBinder;
        private readonly IResponseBinder<AuthorizationErrorResponse> _authorizationErrorResponseBinder;
        private readonly IResponseBinder<AuthorizationCodeResponse> _authorizationCodeResponseBinder;

        public AuthorizationMiddleware(RequestDelegate next,
                                       Configuration.IdentityService service,
                                       IRequestBinder<AuthorizationRequest, AuthorizationErrorResponse> authorizationRequestBinder,
                                       IResponseBinder<AuthorizationErrorResponse> authorizationErrorResponseBinder,
                                       IResponseBinder<AuthorizationCodeResponse> authorizationCodeResponseBinder)
        {
            _next = next;
            _service = service;
            _authorizationRequestBinder = authorizationRequestBinder;
            _authorizationErrorResponseBinder = authorizationErrorResponseBinder;
            _authorizationCodeResponseBinder = authorizationCodeResponseBinder;
        }

        public async Task Invoke(HttpContext context, AuthenticatedUserInformation authenticatedUserInformation, IAuthorizationService authorizationService)
        {
            if (authenticatedUserInformation?.Principal == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var bindingResult = _authorizationRequestBinder.FromQuery(context.Request.Query.ToDictionary(p => p.Key, p => p.Value as IReadOnlyCollection<string>));

            if (bindingResult.Error != null)
            {
                await _authorizationErrorResponseBinder.RespondAsync(bindingResult.Error, context);
            }
            else if (bindingResult.Result != null)
            {
                var claimsPrincipal = authenticatedUserInformation.Principal as ClaimsPrincipal;
                var idClaim = claimsPrincipal?.Claims.FirstOrDefault(p => p.Type == Constants.IdClaimType);

                var result = await authorizationService.AuthorizeRequest(bindingResult.Result, idClaim?.Value);

                if (result.Error != null)
                {
                    await _authorizationErrorResponseBinder.RespondAsync(result.Error, context);
                }
                else if (result.Response != null)
                {
                    await _authorizationCodeResponseBinder.RespondAsync(result.Response, context);
                }
            }
        }
    }
}