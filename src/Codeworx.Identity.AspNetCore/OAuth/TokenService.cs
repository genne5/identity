﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codeworx.Identity.Cache;
using Codeworx.Identity.OAuth;
using Codeworx.Identity.OAuth.Token;

namespace Codeworx.Identity.AspNetCore.OAuth
{
    public class TokenService : ITokenService
    {
        private readonly IClientAuthenticationService _clientAuthenticationService;
        private readonly IRequestValidator<TokenRequest, TokenErrorResponse> _requestValidator;
        private readonly IEnumerable<ITokenResultService> _tokenResultServices;
        private readonly IAuthorizationCodeCache _cache;

        public TokenService(IAuthorizationCodeCache cache, IEnumerable<ITokenResultService> tokenResultServices, IRequestValidator<TokenRequest, TokenErrorResponse> requestValidator, IClientAuthenticationService clientAuthenticationService)
        {
            _tokenResultServices = tokenResultServices;
            _requestValidator = requestValidator;
            _clientAuthenticationService = clientAuthenticationService;
            _cache = cache;
        }

        public async Task<ITokenResult> AuthorizeRequest(
            TokenRequest request,
            string clientId,
            string clientSecret)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var validationResult = await _requestValidator.IsValid(request)
                                                          .ConfigureAwait(false);
            if (validationResult != null)
            {
                return new InvalidRequestResult(validationResult);
            }

            var tokenResultService = _tokenResultServices.FirstOrDefault(p => p.SupportedGrantType == request.GrantType);
            if (tokenResultService == null)
            {
                return new UnsupportedGrantTypeResult();
            }

            var clientAuthenticationResult = await _clientAuthenticationService.AuthenticateClient(request, clientId, clientSecret)
                                                                                 .ConfigureAwait(false);
            if (clientAuthenticationResult.TokenResult != null)
            {
                return clientAuthenticationResult.TokenResult;
            }

            if (clientAuthenticationResult.ClientRegistration == null)
            {
                return new InvalidClientResult();
            }

            if (!clientAuthenticationResult.ClientRegistration.SupportedFlow.Any(p => p.IsSupported(request.GrantType)))
            {
                return new UnauthorizedClientResult();
            }

            var authorizationCodeTokenRequest = request as AuthorizationCodeTokenRequest;

            if (request == null || authorizationCodeTokenRequest == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var deserializedGrantInformation = await _cache.GetAsync(authorizationCodeTokenRequest.Code)
                .ConfigureAwait(false);

            var accessToken = await tokenResultService.CreateAccessToken(deserializedGrantInformation);

            return new SuccessfulTokenResult(accessToken, Identity.OAuth.Constants.TokenType.Bearer);
        }
    }
}