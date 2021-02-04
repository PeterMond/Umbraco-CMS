using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Umbraco.Core;
using Umbraco.Core.Configuration.Models;
using Umbraco.Extensions;
using Umbraco.Tests.TestHelpers;
using Umbraco.Web;
using Umbraco.Web.Common.Controllers;
using Umbraco.Web.Common.Routing;
using Umbraco.Web.PublishedCache;
using Umbraco.Web.Routing;
using Umbraco.Web.Website.Controllers;
using Umbraco.Web.Website.Routing;

namespace Umbraco.Tests.UnitTests.Umbraco.Web.Website.Routing
{
    [TestFixture]
    public class UmbracoRouteValueTransformerTests
    {
        private IOptions<GlobalSettings> GetGlobalSettings() => Options.Create(new GlobalSettings());

        private UmbracoRouteValueTransformer GetTransformerWithRunState(
            IUmbracoContextAccessor ctx,
            IRoutableDocumentFilter filter = null,
            IPublishedRouter router = null,
            IUmbracoRouteValuesFactory routeValuesFactory = null)
            => GetTransformer(ctx, Mock.Of<IRuntimeState>(x => x.Level == RuntimeLevel.Run), filter, router, routeValuesFactory);

        private UmbracoRouteValueTransformer GetTransformer(
            IUmbracoContextAccessor ctx,
            IRuntimeState state,
            IRoutableDocumentFilter filter = null,
            IPublishedRouter router = null,
            IUmbracoRouteValuesFactory routeValuesFactory = null)
        {
            var transformer = new UmbracoRouteValueTransformer(
                new NullLogger<UmbracoRouteValueTransformer>(),
                ctx,
                router ?? Mock.Of<IPublishedRouter>(),
                GetGlobalSettings(),
                TestHelper.GetHostingEnvironment(),
                state,
                routeValuesFactory ?? Mock.Of<IUmbracoRouteValuesFactory>(),
                filter ?? Mock.Of<IRoutableDocumentFilter>(x => x.IsDocumentRequest(It.IsAny<string>()) == true),
                Mock.Of<IDataProtectionProvider>(),
                Mock.Of<IControllerActionSearcher>());

            return transformer;
        }

        private IUmbracoContext GetUmbracoContext(bool hasContent)
        {
            IPublishedContentCache publishedContent = Mock.Of<IPublishedContentCache>(x => x.HasContent() == hasContent);
            var uri = new Uri("http://example.com");

            IUmbracoContext umbracoContext = Mock.Of<IUmbracoContext>(x =>
                x.Content == publishedContent
                && x.OriginalRequestUrl == uri
                && x.CleanedUmbracoUrl == uri);

            return umbracoContext;
        }

        private UmbracoRouteValues GetRouteValues(IPublishedRequest request)
            => new UmbracoRouteValues(
                request,
                new ControllerActionDescriptor
                {
                    ControllerTypeInfo = typeof(TestController).GetTypeInfo(),
                    ControllerName = ControllerExtensions.GetControllerName<TestController>()
                });

        private IUmbracoRouteValuesFactory GetRouteValuesFactory(IPublishedRequest request)
            => Mock.Of<IUmbracoRouteValuesFactory>(x => x.Create(It.IsAny<HttpContext>(), It.IsAny<IPublishedRequest>()) == GetRouteValues(request));

        private IPublishedRouter GetRouter(IPublishedRequest request)
            => Mock.Of<IPublishedRouter>(x => x.RouteRequestAsync(It.IsAny<IPublishedRequestBuilder>(), It.IsAny<RouteRequestOptions>()) == Task.FromResult(request));

        [Test]
        public async Task Noop_When_Runtime_Level_Not_Run()
        {
            UmbracoRouteValueTransformer transformer = GetTransformer(
                Mock.Of<IUmbracoContextAccessor>(),
                Mock.Of<IRuntimeState>());

            RouteValueDictionary result = await transformer.TransformAsync(new DefaultHttpContext(), new RouteValueDictionary());
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task Noop_When_No_Umbraco_Context()
        {
            UmbracoRouteValueTransformer transformer = GetTransformerWithRunState(
                Mock.Of<IUmbracoContextAccessor>());

            RouteValueDictionary result = await transformer.TransformAsync(new DefaultHttpContext(), new RouteValueDictionary());
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task Noop_When_Not_Document_Request()
        {
            UmbracoRouteValueTransformer transformer = GetTransformerWithRunState(
                Mock.Of<IUmbracoContextAccessor>(x => x.UmbracoContext == Mock.Of<IUmbracoContext>()),
                Mock.Of<IRoutableDocumentFilter>(x => x.IsDocumentRequest(It.IsAny<string>()) == false));

            RouteValueDictionary result = await transformer.TransformAsync(new DefaultHttpContext(), new RouteValueDictionary());
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task NoContentController_Values_When_No_Content()
        {
            IUmbracoContext umbracoContext = GetUmbracoContext(false);

            UmbracoRouteValueTransformer transformer = GetTransformerWithRunState(
                Mock.Of<IUmbracoContextAccessor>(x => x.UmbracoContext == umbracoContext));

            RouteValueDictionary result = await transformer.TransformAsync(new DefaultHttpContext(), new RouteValueDictionary());
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(ControllerExtensions.GetControllerName<RenderNoContentController>(), result["controller"]);
            Assert.AreEqual(nameof(RenderNoContentController.Index), result["action"]);
        }

        [Test]
        public async Task Assigns_PublishedRequest_To_UmbracoContext()
        {
            IUmbracoContext umbracoContext = GetUmbracoContext(true);
            IPublishedRequest request = Mock.Of<IPublishedRequest>();

            UmbracoRouteValueTransformer transformer = GetTransformerWithRunState(
                Mock.Of<IUmbracoContextAccessor>(x => x.UmbracoContext == umbracoContext),
                router: GetRouter(request),
                routeValuesFactory: GetRouteValuesFactory(request));

            RouteValueDictionary result = await transformer.TransformAsync(new DefaultHttpContext(), new RouteValueDictionary());
            Assert.AreEqual(request, umbracoContext.PublishedRequest);
        }

        [Test]
        public async Task Assigns_UmbracoRouteValues_To_HttpContext_Feature()
        {
            IUmbracoContext umbracoContext = GetUmbracoContext(true);
            IPublishedRequest request = Mock.Of<IPublishedRequest>();

            UmbracoRouteValueTransformer transformer = GetTransformerWithRunState(
                Mock.Of<IUmbracoContextAccessor>(x => x.UmbracoContext == umbracoContext),
                router: GetRouter(request),
                routeValuesFactory: GetRouteValuesFactory(request));

            var httpContext = new DefaultHttpContext();
            RouteValueDictionary result = await transformer.TransformAsync(httpContext, new RouteValueDictionary());

            UmbracoRouteValues routeVals = httpContext.Features.Get<UmbracoRouteValues>();
            Assert.IsNotNull(routeVals);
            Assert.AreEqual(routeVals.PublishedRequest, umbracoContext.PublishedRequest);
        }

        [Test]
        public async Task Assigns_Values_To_RouteValueDictionary()
        {
            IUmbracoContext umbracoContext = GetUmbracoContext(true);
            IPublishedRequest request = Mock.Of<IPublishedRequest>();
            UmbracoRouteValues routeValues = GetRouteValues(request);

            UmbracoRouteValueTransformer transformer = GetTransformerWithRunState(
                Mock.Of<IUmbracoContextAccessor>(x => x.UmbracoContext == umbracoContext),
                router: GetRouter(request),
                routeValuesFactory: GetRouteValuesFactory(request));

            RouteValueDictionary result = await transformer.TransformAsync(new DefaultHttpContext(), new RouteValueDictionary());

            Assert.AreEqual(routeValues.ControllerName, result["controller"]);
            Assert.AreEqual(routeValues.ActionName, result["action"]);
        }

        private class TestController : RenderController
        {
            public TestController(ILogger<RenderController> logger, ICompositeViewEngine compositeViewEngine, IUmbracoContextAccessor umbracoContextAccessor)
                : base(logger, compositeViewEngine, umbracoContextAccessor)
            {
            }
        }
    }
}