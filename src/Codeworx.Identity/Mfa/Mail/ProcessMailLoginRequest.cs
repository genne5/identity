﻿using System.Security.Claims;

namespace Codeworx.Identity.Mfa.Mail
{
    public class ProcessMailLoginRequest : MailLoginRequest
    {
        public ProcessMailLoginRequest(string providerId, ClaimsIdentity identity, string returnUrl, string oneTimeCode, bool rememberMe)
            : base(providerId, identity, returnUrl, rememberMe)
        {
            OneTimeCode = oneTimeCode;
            SessionId = identity.FindFirst(Constants.Claims.Session)?.Value ?? identity.FindFirst(Constants.Claims.Subject).Value;
        }

        public string OneTimeCode { get; }

        public string SessionId { get; }
    }
}