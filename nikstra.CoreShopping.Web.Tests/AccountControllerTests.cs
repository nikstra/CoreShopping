using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nikstra.CoreShopping.Web.Controllers;
using nikstra.CoreShopping.Web.Models;
using nikstra.CoreShopping.Web.Models.AccountViewModels;
using nikstra.CoreShopping.Web.Services;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nikstra.CoreShopping.Web.Tests
{
    [TestFixture, SetCulture("en-US")]
    class AccountControllerTests
    {
        #region Login tests
        [Test]
        public void Login_ShouldHaveHttpGetAttribute_WhenParameterIsString()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.Login), new [] { typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpGetAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Login_HaveAllowAnonymousAttribute_WhenParameterIsString()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.Login), new[] { typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public async Task Login_ReturnsView_WhenCalled()
        {
            // Arrange
            var userManager = CreateUserManagerMock();
            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(controller.HttpContext, Substitute.For<ITempDataProvider>());
            controller.HttpContext.RequestServices = Substitute.For<IServiceProvider>();
            controller.HttpContext.RequestServices.GetService(typeof(IAuthenticationService))
                .Returns(Substitute.For<IAuthenticationService>());

            // Act
            var result = await controller.Login("url");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(controller.ViewData["ReturnUrl"], Is.EqualTo("url"));
        }

        [Test]
        public void Login_HaveHttpPostAttribute_WhenParametersAreLoginViewModelAndString()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.Login), new[] { typeof(LoginViewModel), typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpPostAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Login_HaveAllowAnonymousAttribute_WhenParametersAreLoginViewModelAndString()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.Login), new[] { typeof(LoginViewModel), typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public async Task Login_RedirectsToReturnUrl_WhenLoginIsSuccessful()
        {
            // Arrange
            var userManager = CreateUserManagerMock();
            var signInManager = CreateSignInManagerMock(userManager);
            signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Success));

            var controller = CreateControllerInstance(signInManager);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(controller.HttpContext, Substitute.For<ITempDataProvider>());
            controller.Url = Substitute.For<IUrlHelper>();
            controller.Url.IsLocalUrl(Arg.Any<string>()).Returns(true);

            var model = new LoginViewModel
            {
                Email = "user@domain.tld",
                Password = "password",
                RememberMe = false
            };

            // Act
            var result = await controller.Login(model, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectResult>());
            Assert.That((result as RedirectResult).Url, Is.EqualTo("url"));
            Assert.That(controller.ViewData["ReturnUrl"], Is.EqualTo("url"));
        }

        [Test]
        public async Task Login_RedirectsToLoginWith2fa_WhenTwoFactorIsRequired()
        {
            // Arrange
            var userManager = CreateUserManagerMock();
            var signInManager = CreateSignInManagerMock(userManager);
            signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired));

            var controller = CreateControllerInstance(signInManager);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(controller.HttpContext, Substitute.For<ITempDataProvider>());

            var model = new LoginViewModel
            {
                Email = "user@domain.tld",
                Password = "password",
                RememberMe = false
            };

            // Act
            var result = await controller.Login(model, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(AccountController.LoginWith2fa)));
            Assert.That(controller.ViewData["ReturnUrl"], Is.EqualTo("url"));
        }

        [Test]
        public async Task Login_RedirectsToLockout_WhenAccountIsLockedOut()
        {
            // Arrange
            var userManager = CreateUserManagerMock();
            var signInManager = CreateSignInManagerMock(userManager);
            signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.LockedOut));

            var controller = CreateControllerInstance(signInManager);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(controller.HttpContext, Substitute.For<ITempDataProvider>());

            var model = new LoginViewModel
            {
                Email = "user@domain.tld",
                Password = "password",
                RememberMe = false
            };

            // Act
            var result = await controller.Login(model, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(AccountController.Lockout)));
            Assert.That(controller.ViewData["ReturnUrl"], Is.EqualTo("url"));
        }

        [Test]
        public async Task Login_AddModelErrorAndReturnsViewAndModel_WhenLoginAttemptIsInvalid()
        {
            // Arrange
            var userManager = CreateUserManagerMock();
            var signInManager = CreateSignInManagerMock(userManager);
            signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Failed));

            var controller = CreateControllerInstance(signInManager);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(controller.HttpContext, Substitute.For<ITempDataProvider>());

            var model = new LoginViewModel
            {
                Email = "user@domain.tld",
                Password = "password",
                RememberMe = false
            };

            // Act
            var result = await controller.Login(model, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).ViewData.ModelState.ErrorCount, Is.GreaterThan(0));
            Assert.That(((result as ViewResult).Model as LoginViewModel)?.Email, Is.EqualTo("user@domain.tld"));
            Assert.That(controller.ViewData["ReturnUrl"], Is.EqualTo("url"));
        }

        [Test]
        public async Task Login_ReturnsViewAndModel_WhenModelStateIsInvalid()
        {
            // Arrange
            var userManager = CreateUserManagerMock();
            var signInManager = CreateSignInManagerMock(userManager);
            signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Failed));

            var controller = CreateControllerInstance(signInManager);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(controller.HttpContext, Substitute.For<ITempDataProvider>());
            controller.ModelState.AddModelError("", "");

            var model = new LoginViewModel
            {
                Email = "user@domain.tld",
                Password = "password",
                RememberMe = false
            };

            // Act
            var result = await controller.Login(model, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).ViewData.ModelState.ErrorCount, Is.GreaterThan(0));
            Assert.That(((result as ViewResult).Model as LoginViewModel)?.Email, Is.EqualTo("user@domain.tld"));
            Assert.That(controller.ViewData["ReturnUrl"], Is.EqualTo("url"));
        }
        #endregion

        private ApplicationUser CreateApplicationUser() =>
            new ApplicationUser
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

        private UserManager<ApplicationUser> CreateUserManagerMock() =>
            Substitute.For<UserManager<ApplicationUser>>(
                Substitute.For<IUserStore<ApplicationUser>>(),
                Substitute.For<IOptions<IdentityOptions>>(),
                Substitute.For<IPasswordHasher<ApplicationUser>>(),
                Substitute.For<IEnumerable<IUserValidator<ApplicationUser>>>(),
                Substitute.For<IEnumerable<IPasswordValidator<ApplicationUser>>>(),
                Substitute.For<ILookupNormalizer>(),
                Substitute.For<IdentityErrorDescriber>(),
                Substitute.For<IServiceProvider>(),
                Substitute.For<ILogger<UserManager<ApplicationUser>>>()
                );

        private SignInManager<ApplicationUser> CreateSignInManagerMock(UserManager<ApplicationUser> userManager) =>
            Substitute.For<SignInManager<ApplicationUser>>(
                userManager,
                Substitute.For<IHttpContextAccessor>(),
                Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>(),
                Substitute.For<IOptions<IdentityOptions>>(),
                Substitute.For<ILogger<SignInManager<ApplicationUser>>>(),
                Substitute.For<IAuthenticationSchemeProvider>()
                );

        private AccountController CreateControllerInstance(SignInManager<ApplicationUser> signInManager) =>
            new AccountController(
                signInManager.UserManager,
                signInManager,
                Substitute.For<IEmailSender>(),
                Substitute.For<ILogger<AccountController>>()
                );
    }
}
