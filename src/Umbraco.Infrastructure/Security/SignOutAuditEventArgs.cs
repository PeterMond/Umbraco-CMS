using Umbraco.Core;
using Umbraco.Core.Security;

namespace Umbraco.Infrastructure.Security
{
    /// <summary>
    /// Event args used when signing out
    /// </summary>
    public class SignOutAuditEventArgs : IdentityAuditEventArgs
    {
        public SignOutAuditEventArgs(AuditEvent action, string ipAddress, string comment = null, string performingUser = Constants.Security.SuperUserIdAsString, string affectedUser = Constants.Security.SuperUserIdAsString)
            : base(action, ipAddress, performingUser, comment, affectedUser, null)
        {
        }

        /// <summary>
        /// Allows event handlers to set a GET absolute URL to be redirected to after successful logout out of the back office. This
        /// can be used for external login providers.
        /// </summary>
        public string SignOutRedirectUrl { get; set; }
    }
}
