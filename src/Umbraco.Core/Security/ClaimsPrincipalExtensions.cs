using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Umbraco.Core;
using Umbraco.Core.Security;

namespace Umbraco.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Gets the required claim types for a back office identity
        /// </summary>
        /// <remarks>
        /// This does not include the role claim type or allowed apps type since that is a collection and in theory could be empty
        /// </remarks>
        public static IEnumerable<string> RequiredBackOfficeIdentityClaimTypes => new[]
        {
            ClaimTypes.NameIdentifier, // id
            ClaimTypes.Name,  // username
            ClaimTypes.GivenName,
            Constants.Security.StartContentNodeIdClaimType,
            Constants.Security.StartMediaNodeIdClaimType,
            ClaimTypes.Locality,
            Constants.Security.SecurityStampClaimType
        };

        /// <summary>
        /// This will return the current back office identity if the IPrincipal is the correct type
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static UmbracoBackOfficeIdentity GetUmbracoIdentity(this IPrincipal user)
        {
            // TODO: It would be nice to get rid of this and only rely on Claims, not a strongly typed identity instance

            // If it's already a UmbracoBackOfficeIdentity
            if (user.Identity is UmbracoBackOfficeIdentity backOfficeIdentity) return backOfficeIdentity;

            // Check if there's more than one identity assigned and see if it's a UmbracoBackOfficeIdentity and use that
            if (user is ClaimsPrincipal claimsPrincipal)
            {
                backOfficeIdentity = claimsPrincipal.Identities.OfType<UmbracoBackOfficeIdentity>().FirstOrDefault();
                if (backOfficeIdentity != null) return backOfficeIdentity;
            }

            // Otherwise convert to a UmbracoBackOfficeIdentity if it's auth'd
            if (user.Identity is ClaimsIdentity claimsIdentity
                && claimsIdentity.IsAuthenticated
                && UmbracoBackOfficeIdentity.FromClaimsIdentity(claimsIdentity, out var umbracoIdentity))
            {
                return umbracoIdentity;
            }

            return null;
        }

        /// <summary>
        /// Verifies that a principal objects contains a valid and authenticated ClaimsIdentity for backoffice.
        /// </summary>
        /// <param name="user">Extended principal</param>
        /// <returns>A valid and authenticated ClaimsIdentity</returns>
        public static ClaimsIdentity VerifyBackOfficeIdentity(this IPrincipal user)
        {
            if (!(user.Identity is ClaimsIdentity claimsIdentity))
            {
                // If the identity type is not ClaimsIdentity it's not a BackOfficeIdentity.
                return null;
            }

            if (!claimsIdentity.IsAuthenticated)
            {
                // If the identity isn't authenticated count it as invalid.
                return null;
            }

            foreach (var claimType in RequiredBackOfficeIdentityClaimTypes)
            {
                // If the identity doesn't have the claim or if the value is null it's not a valid BackOfficeIdentity.
                if (claimsIdentity.HasClaim(x => x.Type == claimType) == false
                    || claimsIdentity.HasClaim(x => x.Type == claimType && x.Value.IsNullOrWhiteSpace()))
                {
                    return null;
                }
            }

            return claimsIdentity;
        }

        /// <summary>
        /// Returns the remaining seconds on an auth ticket for the user based on the claim applied to the user durnig authentication
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static double GetRemainingAuthSeconds(this IPrincipal user) => user.GetRemainingAuthSeconds(DateTimeOffset.UtcNow);

        /// <summary>
        /// Returns the remaining seconds on an auth ticket for the user based on the claim applied to the user durnig authentication
        /// </summary>
        /// <param name="user"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public static double GetRemainingAuthSeconds(this IPrincipal user, DateTimeOffset now)
        {
            var claimsPrincipal = user as ClaimsPrincipal;
            if (claimsPrincipal == null)
            {
                return 0;
            }

            var ticketExpires = claimsPrincipal.FindFirst(Constants.Security.TicketExpiresClaimType)?.Value;
            if (ticketExpires.IsNullOrWhiteSpace())
            {
                return 0;
            }

            var utcExpired = DateTimeOffset.Parse(ticketExpires, null, DateTimeStyles.RoundtripKind);

            var secondsRemaining = utcExpired.Subtract(now).TotalSeconds;
            return secondsRemaining;
        }
    }
}