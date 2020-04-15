﻿namespace Codeworx.Identity.OAuth.Binding.AuthorizationCodeToken
{
    public class GrantTypeDuplicatedResult : IRequestBindingResult<AuthorizationCodeTokenRequest, ErrorResponse>
    {
        public GrantTypeDuplicatedResult()
        {
            this.Error = new ErrorResponse(Constants.Error.InvalidRequest, string.Empty, string.Empty);
        }

        public AuthorizationCodeTokenRequest Result => null;

        public ErrorResponse Error { get; }
    }
}
