﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Codeworx.Identity.Cache;

namespace Codeworx.Identity.Token.Reference
{
    public class ReferenceToken : IToken
    {
        private readonly ITokenCache _tokenCache;
        private string _key;

        public ReferenceToken(TokenType tokenType, ITokenCache tokenCache)
        {
            TokenType = tokenType;
            _tokenCache = tokenCache;

            if (tokenType == TokenType.IdToken)
            {
                throw new NotSupportedException("The id token can not be a reference token.");
            }
        }

        public TokenType TokenType { get; }

        public IdentityData IdentityData { get; private set; }

        public DateTime ValidUntil { get; private set; }

        public async Task ParseAsync(string value, CancellationToken token = default)
        {
            _key = value;
            var entry = await _tokenCache.GetAsync(TokenType, value, token).ConfigureAwait(false);
            IdentityData = entry.IdentityData;
            ValidUntil = entry.ValidUntil;
        }

        public async Task<string> SerializeAsync(CancellationToken token = default)
        {
            if (_key != null)
            {
                return _key;
            }

            var data = IdentityData ?? throw new ArgumentNullException(nameof(IdentityData));

            var result = await _tokenCache.SetAsync(TokenType, data, ValidUntil, token).ConfigureAwait(false);
            return result;
        }

        public Task SetPayloadAsync(IdentityData identityData, TimeSpan expiration, CancellationToken token = default)
        {
            IdentityData = identityData;
            ValidUntil = DateTime.UtcNow.Add(expiration);
            return Task.CompletedTask;
        }
    }
}
