﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Codeworx.Identity
{
    public static class CodeworxIdentityIdentityDataExtensions
    {
        public static ClaimsIdentity ToClaimsIdentity(this IdentityData data)
        {
            var identity = new ClaimsIdentity(Constants.ProductName, Constants.Claims.Name, Constants.Claims.Role);

            var tenants = data.ToTenantClaims();
            identity.AddClaims(tenants);
            var identityClaims = data.ToIdentityClaims();
            identity.AddClaims(identityClaims);

            var additionalClaims = data.ToLoginCookieClaims();
            identity.AddClaims(additionalClaims);

            return identity;
        }

        public static ClaimsPrincipal ToClaimsPrincipal(this IdentityData data)
        {
            var identity = data.ToClaimsIdentity();

            var principal = new ClaimsPrincipal(identity);

            return principal;
        }

        public static IdentityData ToIdentityData(this ClaimsIdentity identity)
        {
            string id = null;
            string login = null;
            string currentTenant = null;
            List<TenantInfo> tenants = new List<TenantInfo>();
            List<Claim> claims = new List<Claim>();

            foreach (var item in identity.Claims)
            {
                switch (item.Type)
                {
                    case Constants.Claims.Id:
                        id = item.Value;
                        break;

                    case Constants.Claims.Name:
                        login = item.Value;
                        break;

                    case Constants.Claims.CurrentTenant:
                        currentTenant = item.Value;
                        tenants.Add(new TenantInfo { Key = currentTenant, Name = item.Properties[Constants.TenantNameProperty] });
                        break;

                    case Constants.Claims.Tenant:
                        tenants.Add(new TenantInfo { Key = item.Value, Name = item.Properties[Constants.TenantNameProperty] });
                        break;

                    default:
                        claims.Add(item);
                        break;
                }
            }

            var assignedClaims = claims
                                    .GroupBy(p => p.Type)
                                    .Select(p => new AssignedClaim(p.Key, p.Select(x => x.Value), ClaimTarget.LoginCookie, AssignedClaim.AssignmentSource.Global))
                                    .ToList();

            return new IdentityData(id, login, tenants, assignedClaims, currentTenant);
        }

        public static IEnumerable<Claim> ToTenantClaims(this IdentityData data)
        {
            foreach (var item in data.Tenants)
            {
                var type = item.Key == data.TenantKey ? Constants.Claims.CurrentTenant : Constants.Claims.Tenant;
                var value = item.Key;

                var claim = new Claim(type, value, item.Name);
                claim.Properties.Add(Constants.TenantNameProperty, item.Name);
                yield return claim;
            }
        }

        private static AssignedClaim.AssignmentSource ParseClaimSource(string issuer)
        {
            var target = issuer.Substring(Constants.ClaimSourceUrl.Length);
            return (AssignedClaim.AssignmentSource)Enum.Parse(typeof(AssignedClaim.AssignmentSource), target, true);
        }

        private static ClaimTarget ParseClaimTarget(string originalIssuer)
        {
            var target = originalIssuer.Substring(Constants.ClaimTargetUrl.Length);
            return (ClaimTarget)Enum.Parse(typeof(ClaimTarget), target, true);
        }

        private static IEnumerable<Claim> ToIdentityClaims(this IdentityData data)
        {
            yield return new Claim(Constants.Claims.Id, data.Identifier);
            yield return new Claim(Constants.Claims.Name, data.Login);
        }

        private static IEnumerable<Claim> ToLoginCookieClaims(this IdentityData data)
        {
            var claims = from c in data.Claims.Where(p => p.Target.HasFlag(ClaimTarget.LoginCookie))
                         from v in c.Values
                         select new Claim(c.Type, v);

            return claims.ToList();
        }
    }
}