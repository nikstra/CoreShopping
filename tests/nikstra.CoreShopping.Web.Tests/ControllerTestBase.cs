using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using nikstra.CoreShopping.Service.Models;
using nikstra.CoreShopping.Web.Models;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace nikstra.CoreShopping.Web.Tests
{
    // https://www.reddit.com/r/csharp/comments/7sxfkk/unit_testnet_core_how_to_mock_httpcontext_that_is/
    // https://gist.github.com/johnnyreilly/4959924
    // https://docs.microsoft.com/en-us/aspnet/core/migration/http-modules?view=aspnetcore-2.1
    // https://github.com/aspnet/Mvc/blob/dev/src/Microsoft.AspNetCore.Mvc.Core/ControllerBase.cs
    // https://github.com/aspnet/Mvc/blob/dev/src/Microsoft.AspNetCore.Mvc.Core/Routing/UrlHelper.cs

    [TestFixture]
    public abstract class ControllerTestBase
    {
        protected Uri _uri;
        protected string _controller;
        protected string _action;
        protected string _scheme;

        [SetUp]
        protected virtual void Setup()
        {
            _uri = null;
            _controller = null;
            _action = null;
            _scheme = null;
        }

        protected virtual ShopUser CreateGoodShopUser() =>
            new ShopUser
            {
                AccessFailedCount = 0,
                ConcurrencyStamp = "abc",
                Email = "user@domain.tld",
                EmailConfirmed = false,
                Id = "0001",
                LockoutEnabled = false,
                LockoutEnd = null,
                NormalizedEmail = "USER@DOMAIN.TLD",
                NormalizedUserName = "USER@DOMAIN.TLD",
                PasswordHash = "hash",
                PhoneNumber = "555-224466",
                PhoneNumberConfirmed = false,
                SecurityStamp = "stamp",
                TwoFactorEnabled = false,
                UserName = "user@domain.tld"
            };

        protected virtual UserManager<ShopUser> CreateUserManagerStub() =>
            Substitute.For<UserManager<ShopUser>>(
                Substitute.For<IUserStore<ShopUser>>(),
                Substitute.For<IOptions<IdentityOptions>>(),
                Substitute.For<IPasswordHasher<ShopUser>>(),
                Substitute.For<IEnumerable<IUserValidator<ShopUser>>>(),
                Substitute.For<IEnumerable<IPasswordValidator<ShopUser>>>(),
                Substitute.For<ILookupNormalizer>(),
                Substitute.For<IdentityErrorDescriber>(),
                Substitute.For<IServiceProvider>(),
                Substitute.For<ILogger<UserManager<ShopUser>>>()
                );

        protected virtual SignInManager<ShopUser> CreateSignInManagerStub(UserManager<ShopUser> userManager) =>
            Substitute.For<SignInManager<ShopUser>>(
                userManager,
                Substitute.For<IHttpContextAccessor>(),
                Substitute.For<IUserClaimsPrincipalFactory<ShopUser>>(),
                Substitute.For<IOptions<IdentityOptions>>(),
                Substitute.For<ILogger<SignInManager<ShopUser>>>(),
                Substitute.For<IAuthenticationSchemeProvider>()
                );

        protected virtual RouteData SetupRouteDataStub()
        {
            RouteData routeData = new RouteData();

            routeData.Values.Add("controller", _controller);
            routeData.Values.Add("action", _action);

            return routeData;
        }

        protected virtual RequestHeaders SetupRequestHeadersStub(RequestHeaders headers)
        {
            if(headers == null) throw new ArgumentNullException(nameof(headers));

            headers.Accept = MediaTypeHeaderValue.ParseList(new [] { "text/html, application/xhtml+xml, application/xml; q=0.9, */*; q=0.8" });
            headers.AcceptEncoding = StringWithQualityHeaderValue.ParseList(new[] { "gzip, deflate, br" });
            headers.AcceptLanguage = StringWithQualityHeaderValue.ParseList(new[] { "en-GB, en-US; q=0.8, en; q=0.6, sv; q=0.4, ru; q=0.2" });
            headers.Host = HostString.FromUriComponent(_uri.GetComponents(UriComponents.HostAndPort, UriFormat.Unescaped));
            headers.Set(HeaderNames.Connection, "Keep-Alive");
            headers.Set(HeaderNames.Referer, _uri.AbsoluteUri);
            headers.Set(HeaderNames.UserAgent, "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:60.0) Gecko/20100101 Firefox/60.0");

            return headers;
        }

        protected virtual HttpContext SetupHttpContextStub()
        {
            HttpContext httpContext = new DefaultHttpContext();

            RequestHeaders headers = httpContext.Request.GetTypedHeaders();
            SetupRequestHeadersStub(headers);

            httpContext.Request.Host = HostString.FromUriComponent(_uri);
            httpContext.Request.Method = "GET"; // TODO: Don't hardcode request method.
            httpContext.Request.Path = PathString.FromUriComponent(_uri);
            httpContext.Request.Protocol = "HTTP/1.1";
            httpContext.Request.Scheme = _scheme;

            httpContext.RequestServices = Substitute.For<IServiceProvider>();
            httpContext.RequestServices.GetService(typeof(IAuthenticationService))
                .Returns(Substitute.For<IAuthenticationService>());

            httpContext.Session = Substitute.For<ISession>();

            // TODO: This should relate to what is returned by CreateGoodShopUser().
            ClaimsPrincipal user = Substitute.For<ClaimsPrincipal>();
            user.Identity.Name.Returns("UserName");
            user.Identity.IsAuthenticated.Returns(true);
            httpContext.User = user;

            return httpContext;
        }

        protected virtual ControllerContext SetupControllerContextStub()
        {
            ControllerContext controllerContext = Substitute.For<ControllerContext>();

            controllerContext.ActionDescriptor = Substitute.For<ControllerActionDescriptor>();
            controllerContext.HttpContext = SetupHttpContextStub();
            controllerContext.RouteData = SetupRouteDataStub();
            controllerContext.ValueProviderFactories = Substitute.For<IList<IValueProviderFactory>>();

            return controllerContext;
        }

        protected virtual IUrlHelper SetupUrlHelperStub(ActionContext context, string url, bool isLocal = true)
        {
            IUrlHelper urlHelper = Substitute.For<IUrlHelper>();

            urlHelper.Action(Arg.Any<UrlActionContext>()).Returns(url);
            urlHelper.IsLocalUrl(Arg.Any<string>()).Returns(isLocal);
            urlHelper.RouteUrl(Arg.Any<string>(), Arg.Any<object>()).Returns(url);
            urlHelper.ActionContext.Returns(context);

            return urlHelper;
        }

        protected Uri CreateUri(string controller, string action, string host, string scheme = "http")
        {
            _controller = controller;
            _action = action;
            _scheme = scheme;

            var ub = new UriBuilder
            {
                Scheme = scheme,
                Host = host,
                Path = $"{controller.Substring(0, controller.LastIndexOf("Controller"))}/{action}"
            };

            return ub.Uri;
        }

        protected virtual T InjectControllerContextStub<T>(T controller, string action, string host = "localhost")
            where T : Controller
        {
            _uri = CreateUri(controller.GetType().Name, action, host);

            controller.ControllerContext = SetupControllerContextStub();
            controller.Url = SetupUrlHelperStub(controller.ControllerContext, "http://localhost/controller/action");
            controller.TempData = new TempDataDictionary(controller.HttpContext, Substitute.For<ITempDataProvider>());

            return controller;
        }
    }
}
