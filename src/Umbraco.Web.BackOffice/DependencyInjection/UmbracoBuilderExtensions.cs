using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Core.DependencyInjection;
using Umbraco.Core.Hosting;
using Umbraco.Core.IO;
using Umbraco.Core.Services;
using Umbraco.Extensions;
using Umbraco.Infrastructure.DependencyInjection;
using Umbraco.Web.BackOffice.Authorization;
using Umbraco.Web.BackOffice.Controllers;
using Umbraco.Web.BackOffice.Filters;
using Umbraco.Web.BackOffice.Middleware;
using Umbraco.Web.BackOffice.Routing;
using Umbraco.Web.BackOffice.Security;
using Umbraco.Web.BackOffice.Services;
using Umbraco.Web.BackOffice.Trees;
using Umbraco.Web.Common.Authorization;
using Umbraco.Web.Common.DependencyInjection;
using Umbraco.Web.WebAssets;

namespace Umbraco.Web.BackOffice.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="IUmbracoBuilder"/> for the Umbraco back office
    /// </summary>
    public static class UmbracoBuilderExtensions
    {
        /// <summary>
        /// Adds all required components to run the Umbraco back office
        /// </summary>
        public static IUmbracoBuilder AddBackOffice(this IUmbracoBuilder builder) => builder
                .AddConfiguration()
                .AddUmbracoCore()
                .AddWebComponents()
                .AddRuntimeMinifier()
                .AddBackOfficeCore()
                .AddBackOfficeAuthentication()
                .AddBackOfficeIdentity()
                .AddMembersIdentity()
                .AddBackOfficeAuthorizationPolicies()
                .AddUmbracoProfiler()
                .AddMvcAndRazor()
                .AddWebServer()
                .AddPreviewSupport()
                .AddHostedServices()
                .AddDistributedCache();

        /// <summary>
        /// Adds Umbraco back office authentication requirements
        /// </summary>
        public static IUmbracoBuilder AddBackOfficeAuthentication(this IUmbracoBuilder builder)
        {
            builder.Services.AddAntiforgery();

            builder.Services

                // This just creates a builder, nothing more
                .AddAuthentication()

                // Add our custom schemes which are cookie handlers
                .AddCookie(Core.Constants.Security.BackOfficeAuthenticationType)
                .AddCookie(Core.Constants.Security.BackOfficeExternalAuthenticationType, o =>
                {
                    o.Cookie.Name = Core.Constants.Security.BackOfficeExternalAuthenticationType;
                    o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                })

                // Although we don't natively support this, we add it anyways so that if end-users implement the required logic
                // they don't have to worry about manually adding this scheme or modifying the sign in manager
                .AddCookie(Core.Constants.Security.BackOfficeTwoFactorAuthenticationType, o =>
                {
                    o.Cookie.Name = Core.Constants.Security.BackOfficeTwoFactorAuthenticationType;
                    o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                });

            builder.Services.ConfigureOptions<ConfigureBackOfficeCookieOptions>();

            builder.Services.AddUnique<PreviewAuthenticationMiddleware>();
            builder.Services.AddUnique<BackOfficeExternalLoginProviderErrorMiddleware>();
            builder.Services.AddUnique<IBackOfficeAntiforgery, BackOfficeAntiforgery>();

            return builder;
        }

        /// <summary>
        /// Adds Identity support for Umbraco back office
        /// </summary>
        public static IUmbracoBuilder AddBackOfficeIdentity(this IUmbracoBuilder builder)
        {
            builder.Services.AddUmbracoBackOfficeIdentity();

            return builder;
        }

        /// <summary>
        /// Adds Identity support for Umbraco members
        /// </summary>
        public static IUmbracoBuilder AddMembersIdentity(this IUmbracoBuilder builder)
        {
            builder.Services.AddMembersIdentity();

            return builder;
        }

        /// <summary>
        /// Adds Umbraco back office authorization policies
        /// </summary>
        public static IUmbracoBuilder AddBackOfficeAuthorizationPolicies(this IUmbracoBuilder builder, string backOfficeAuthenticationScheme = Core.Constants.Security.BackOfficeAuthenticationType)
        {
            builder.Services.AddBackOfficeAuthorizationPolicies(backOfficeAuthenticationScheme);

            builder.Services.AddSingleton<IAuthorizationHandler, FeatureAuthorizeHandler>();

            builder.Services.AddAuthorization(options
                => options.AddPolicy(AuthorizationPolicies.UmbracoFeatureEnabled, policy
                    => policy.Requirements.Add(new FeatureAuthorizeRequirement())));

            return builder;
        }

        /// <summary>
        /// Adds Umbraco preview support
        /// </summary>
        public static IUmbracoBuilder AddPreviewSupport(this IUmbracoBuilder builder)
        {
            builder.Services.AddSignalR();

            return builder;
        }

        /// <summary>
        /// Adds support for external login providers in Umbraco
        /// </summary>
        public static IUmbracoBuilder AddBackOfficeExternalLogins(this IUmbracoBuilder umbracoBuilder, Action<BackOfficeExternalLoginsBuilder> builder)
        {
            builder(new BackOfficeExternalLoginsBuilder(umbracoBuilder.Services));
            return umbracoBuilder;
        }

        /// <summary>
        /// Gets the back office tree collection builder
        /// </summary>
        public static TreeCollectionBuilder Trees(this IUmbracoBuilder builder)
            => builder.WithCollectionBuilder<TreeCollectionBuilder>();

        public static IUmbracoBuilder AddBackOfficeCore(this IUmbracoBuilder builder)
        {
            builder.Services.AddUnique<ServerVariablesParser>();
            builder.Services.AddUnique<BackOfficeAreaRoutes>();
            builder.Services.AddUnique<PreviewRoutes>();
            builder.Services.AddUnique<BackOfficeServerVariables>();
            builder.Services.AddScoped<BackOfficeSessionIdValidator>();
            builder.Services.AddScoped<BackOfficeSecurityStampValidator>();

            // register back office trees
            // the collection builder only accepts types inheriting from TreeControllerBase
            // and will filter out those that are not attributed with TreeAttribute
            var umbracoApiControllerTypes = builder.TypeLoader.GetUmbracoApiControllers().ToList();
            builder.Trees()
                .AddTreeControllers(umbracoApiControllerTypes.Where(x => typeof(TreeControllerBase).IsAssignableFrom(x)));

            builder.AddWebMappingProfiles();

            builder.Services.AddUnique<IPhysicalFileSystem>(factory =>
            {
                var path = "~/";
                var hostingEnvironment = factory.GetRequiredService<IHostingEnvironment>();
                return new PhysicalFileSystem(
                    factory.GetRequiredService<IIOHelper>(),
                    hostingEnvironment,
                    factory.GetRequiredService<ILogger<PhysicalFileSystem>>(),
                    hostingEnvironment.MapPathContentRoot(path),
                    hostingEnvironment.ToAbsolute(path)
                );
            });

            builder.Services.AddUnique<IIconService, IconService>();
            builder.Services.AddUnique<UnhandledExceptionLoggerMiddleware>();

            return builder;
        }
    }
}
