﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using nikstra.CoreShopping.Web.Controllers;
using nikstra.CoreShopping.Web.Models;
using nikstra.CoreShopping.Web.Models.AccountViewModels;
using nikstra.CoreShopping.Web.Services;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace nikstra.CoreShopping.Web.Tests
{
    [TestFixture, SetCulture("en-US")]
    class AccountControllerTests : ControllerTestBase
    {
        private AccountController CreateControllerInstance(SignInManager<ApplicationUser> signInManager) =>
            new AccountController(
                signInManager.UserManager,
                signInManager,
                Substitute.For<IEmailSender>(),
                Substitute.For<ILogger<AccountController>>()
                );

        #region Login tests
        [Test]
        public void Get_Login_ShouldHaveHttpGetAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.Login), new[] { typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpGetAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Get_Login_ShouldHaveAllowAnonymousAttribute()
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
        public async Task Get_Login_ReturnsView_WhenCalled()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(AccountController.Login));

            // Act
            var result = await controller.Login("url");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(controller.ViewData["ReturnUrl"], Is.EqualTo("url"));
        }

        [Test]
        public void Post_Login_ShouldHaveHttpPostAttribute()
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
        public void Post_Login_ShouldHaveAllowAnonymousAttribute()
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
        public async Task Post_Login_RedirectsToReturnUrl_WhenLoginIsSuccessful()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Success));

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(AccountController.Login));

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
        public async Task Post_Login_RedirectsToLoginWith2fa_WhenTwoFactorIsRequired()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired));

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(AccountController.Login));

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
        public async Task Post_Login_RedirectsToLockout_WhenAccountIsLockedOut()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.LockedOut));

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(AccountController.Login));

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
        public async Task Post_Login_AddModelErrorAndReturnsViewAndModel_WhenLoginAttemptIsInvalid()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Failed));

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(AccountController.Login));

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
        public async Task Post_Login_ReturnsViewAndModel_WhenModelStateIsInvalid()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.PasswordSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Failed));

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(AccountController.Login));
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

        #region LoginWith2fa tests
        [Test]
        public void Get_LoginWith2fa_ShouldHaveHttpGetAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.LoginWith2fa), new[] { typeof(bool), typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpGetAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Get_LoginWith2fa_ShouldHaveAllowAnonymousAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.LoginWith2fa), new[] { typeof(bool), typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public async Task Get_LoginWith2fa_ReturnsViewAndModel_WhenUserWithTwoFactorAuthExists()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetTwoFactorAuthenticationUserAsync()
                .Returns(Task.FromResult(CreateGoodApplicationUser()));

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.LoginWith2fa(false, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).ViewData["ReturnUrl"], Is.EqualTo("url"));
            Assert.That((result as ViewResult).Model, Is.InstanceOf<LoginWith2faViewModel>());
            Assert.That(((result as ViewResult).Model as LoginWith2faViewModel).RememberMe, Is.False);
        }

        [Test]
        public void Get_LoginWith2fa_ThrowsApplicationException_WhenUserWithTwoFactorAuthCanNotBeLoaded()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetTwoFactorAuthenticationUserAsync()
                .Returns(Task.FromResult((ApplicationUser)null));

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.LoginWith2fa(false, "url");
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load two-factor authentication user."));
        }

        [Test]
        public void Post_LoginWith2fa_ShouldHaveHttpPostAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.LoginWith2fa),
                new[] { typeof(LoginWith2faViewModel), typeof(bool), typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpPostAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Post_LoginWith2fa_ShouldHaveAllowAnonymousAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.LoginWith2fa),
                new[] { typeof(LoginWith2faViewModel), typeof(bool), typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public async Task Post_LoginWith2fa_RedirectsToReturnUrl_WhenTwoFactorLoginIsSuccessful()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetTwoFactorAuthenticationUserAsync()
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            signInManager.TwoFactorAuthenticatorSignInAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Success));

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(AccountController.LoginWith2fa));

            var model = new LoginWith2faViewModel
            {
                RememberMachine = false,
                RememberMe = false,
                TwoFactorCode = "code"
            };

            // Act
            var result = await controller.LoginWith2fa(model, false, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectResult>());
            Assert.That((result as RedirectResult).Url, Is.EqualTo("url"));
        }

        [Test]
        public async Task Post_LoginWith2fa_ReturnsViewAndModel_WhenModelStateIsNotValid()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);
            controller.ModelState.AddModelError("", "");

            var model = new LoginWith2faViewModel
            {
                RememberMachine = false,
                RememberMe = false,
                TwoFactorCode = "code"
            };

            // Act
            var result = await controller.LoginWith2fa(model, false, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<LoginWith2faViewModel>());
            Assert.That((result as ViewResult).Model, Is.EqualTo(model));
        }

        [Test]
        public async Task Post_LoginWith2fa_RedirectsToLockout_WhenAccountIsLockedOut()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetTwoFactorAuthenticationUserAsync()
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            signInManager.TwoFactorAuthenticatorSignInAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.LockedOut));

            var controller = CreateControllerInstance(signInManager);

            var model = new LoginWith2faViewModel
            {
                RememberMachine = false,
                RememberMe = false,
                TwoFactorCode = "code"
            };

            // Act
            var result = await controller.LoginWith2fa(model, false, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(AccountController.Lockout)));
        }

        [Test]
        public async Task Post_LoginWith2fa_AddModelErrorAndReturnsView_WhenAuthenticatorCodeIsInvalid()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetTwoFactorAuthenticationUserAsync()
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            signInManager.TwoFactorAuthenticatorSignInAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Failed));

            var controller = CreateControllerInstance(signInManager);

            var model = new LoginWith2faViewModel
            {
                RememberMachine = false,
                RememberMe = false,
                TwoFactorCode = "code"
            };

            // Act
            var result = await controller.LoginWith2fa(model, false, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).ViewData.ModelState.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Post_LoginWith2fa_ThrowsApplicationException_WhenUserWithTwoFactorAuthCanNotBeLoaded()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetTwoFactorAuthenticationUserAsync()
                .Returns(Task.FromResult((ApplicationUser)null));

            var controller = CreateControllerInstance(signInManager);

            var model = new LoginWith2faViewModel();

            // Act
            async Task Act()
            {
                var result = await controller.LoginWith2fa(model, false, "url");
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }
        #endregion

        #region LoginWithRecoveryCode tests
        [Test]
        public void Get_LoginWithRecoveryCode_ShouldHaveHttpGetAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.LoginWithRecoveryCode), new[] { typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpGetAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Get_LoginWithRecoveryCode_ShouldHaveAllowAnonymousAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.LoginWithRecoveryCode), new[] { typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public async Task Get_LoginWithRecoveryCode_ReturnsView_WhenUserWithTwoFactorAuthExists()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetTwoFactorAuthenticationUserAsync()
                .Returns(Task.FromResult(CreateGoodApplicationUser()));

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.LoginWithRecoveryCode("url");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).ViewData["ReturnUrl"], Is.EqualTo("url"));
        }

        [Test]
        public void Get_LoginWithRecoveryCode_ThrowsApplicationException_WhenUserWithTwoFactorAuthCanNotBeLoaded()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetTwoFactorAuthenticationUserAsync()
                .Returns(Task.FromResult((ApplicationUser)null));

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.LoginWithRecoveryCode("url");
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load two-factor authentication user."));
        }

        [Test]
        public void Post_LoginWithRecoveryCode_ShouldHaveHttpPostAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.LoginWithRecoveryCode),
                new[] { typeof(LoginWithRecoveryCodeViewModel), typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpPostAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Post_LoginWithRecoveryCode_ShouldHaveAllowAnonymousAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.LoginWithRecoveryCode),
                new[] { typeof(LoginWithRecoveryCodeViewModel), typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public async Task Post_LoginWithRecoveryCode_RedirectsToReturnUrl_WhenLoginWithRecoveryCodeIsSuccessful()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetTwoFactorAuthenticationUserAsync()
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            signInManager.TwoFactorRecoveryCodeSignInAsync(Arg.Any<string>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Success));

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(AccountController.LoginWithRecoveryCode));

            var model = new LoginWithRecoveryCodeViewModel
            {
                RecoveryCode = "code"
            };

            // Act
            var result = await controller.LoginWithRecoveryCode(model, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectResult>());
            Assert.That((result as RedirectResult).Url, Is.EqualTo("url"));
        }

        [Test]
        public async Task Post_LoginWithRecoveryCode_ReturnsViewAndModel_WhenModelStateIsNotValid()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);
            controller.ModelState.AddModelError("", "");

            var model = new LoginWithRecoveryCodeViewModel();

            // Act
            var result = await controller.LoginWithRecoveryCode(model, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<LoginWithRecoveryCodeViewModel>());
            Assert.That((result as ViewResult).Model, Is.EqualTo(model));
        }

        [Test]
        public async Task Post_LoginWithRecoveryCode_RedirectsToLockout_WhenAccountIsLockedOut()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetTwoFactorAuthenticationUserAsync()
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            signInManager.TwoFactorRecoveryCodeSignInAsync(Arg.Any<string>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.LockedOut));

            var controller = CreateControllerInstance(signInManager);

            var model = new LoginWithRecoveryCodeViewModel
            {
                RecoveryCode = "code"
            };

            // Act
            var result = await controller.LoginWithRecoveryCode(model, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(AccountController.Lockout)));
        }

        [Test]
        public async Task Post_LoginWithRecoveryCode_AddModelErrorAndReturnsView_WhenRecoveryCodeIsInvalid()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetTwoFactorAuthenticationUserAsync()
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            signInManager.TwoFactorRecoveryCodeSignInAsync(Arg.Any<string>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Failed));

            var controller = CreateControllerInstance(signInManager);

            var model = new LoginWithRecoveryCodeViewModel
            {
                RecoveryCode = "code"
            };

            // Act
            var result = await controller.LoginWithRecoveryCode(model, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).ViewData.ModelState.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Post_LoginWithRecoveryCode_ThrowsApplicationException_WhenUserWithTwoFactorAuthCanNotBeLoaded()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetTwoFactorAuthenticationUserAsync()
                .Returns(Task.FromResult((ApplicationUser)null));

            var controller = CreateControllerInstance(signInManager);

            var model = new LoginWithRecoveryCodeViewModel();

            // Act
            async Task Act()
            {
                var result = await controller.LoginWithRecoveryCode(model, "url");
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load two-factor authentication user."));
        }
        #endregion

        #region Lockout tests
        [Test]
        public void Get_Lockout_ShouldHaveHttpGetAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.Lockout));
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpGetAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Get_Lockout_ShouldHaveAllowAnonymousAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.Lockout));
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Get_Lockout_ReturnsView_WhenCalled()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = controller.Lockout();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }
        #endregion

        #region Register tests
        [Test]
        public void Get_Register_ShouldHaveHttpGetAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.Register), new[] { typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpGetAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Get_Register_ShouldHaveAllowAnonymousAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.Register), new[] { typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Get_Register_ReturnsView_WhenCalled()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = controller.Register("url");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).ViewData["ReturnUrl"], Is.EqualTo("url"));
        }

        [Test]
        public void Post_Register_ShouldHaveHttpPostAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.Register),
                new[] { typeof(RegisterViewModel), typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpPostAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Post_Register_ShouldHaveAllowAnonymousAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.Register),
                new[] { typeof(RegisterViewModel), typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public async Task Post_Register_RedirectsToReturnUrl_WhenUserSuccessfullyRegisters()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(Task.FromResult(IdentityResult.Success));
            userManager.GenerateEmailConfirmationTokenAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult("code"));
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.SignInAsync(Arg.Any<ApplicationUser>(), Arg.Any<bool>())
                .Returns(Task.FromResult(0));

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(AccountController.Register));

            var model = new RegisterViewModel
            {
                Email = "user@domain.tld",
                Password = "password",
                ConfirmPassword = "password"
            };

            // Act
            var result = await controller.Register(model, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectResult>());
            Assert.That((result as RedirectResult).Url, Is.EqualTo("url"));
        }

        [Test]
        public async Task Post_Register_AddModelErrorAndReturnsView_WhenCreateUserFails()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(Task.FromResult(IdentityResult.Failed(
                    new IdentityError { Code = "code", Description = "description" })));

            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            var model = new RegisterViewModel();

            // Act
            var result = await controller.Register(model, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<RegisterViewModel>());
            Assert.That(controller.ModelState.ErrorCount, Is.GreaterThan(0));
        }

        [Test]
        public async Task Post_Register_ReturnsViewAndModel_WhenModelStateIsNotValid()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);
            controller.ModelState.AddModelError("", "");

            var model = new RegisterViewModel();

            // Act
            var result = await controller.Register(model, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<RegisterViewModel>());
        }
        #endregion

        #region Logout tests
        [Test]
        public void Post_Logout_ShouldHaveHttpPostAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.Logout));
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpPostAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public async Task Post_Logout_RedirectsToHomeIndex_WhenUserLogsOut()
        {
            // Arrange
            var name = nameof(HomeController);
            var redirectControllerName = name.Substring(0, name.LastIndexOf("Controller"));

            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.SignOutAsync()
                .Returns(Task.FromResult(0));

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.Logout();

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ControllerName, Is.EqualTo(redirectControllerName));
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(HomeController.Index)));
        }
        #endregion

        #region ExternalLogin tests
        [Test]
        public void Post_ExternalLogin_ShouldHaveHttpPostAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ExternalLogin),
                new[] { typeof(string), typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpPostAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Post_ExternalLogin_ShouldHaveAllowAnonymousAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ExternalLogin),
                new[] { typeof(string), typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Post_ExternalLogin_RedirectsToExternalProvider_WhenCalled()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.ConfigureExternalAuthenticationProperties(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new AuthenticationProperties { RedirectUri = "url" });

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(AccountController.Register));

            // Act
            var result = controller.ExternalLogin("provider", "url");

            // Assert
            Assert.That(result, Is.InstanceOf<ChallengeResult>());
            Assert.That((result as ChallengeResult).Properties.RedirectUri, Is.EqualTo("url"));
        }
        #endregion

        #region ExternalLoginCallback tests
        [Test]
        public void Get_ExternalLoginCallback_ShouldHaveHttpGetAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ExternalLoginCallback),
                new[] { typeof(string), typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpGetAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Get_ExternalLoginCallback_ShouldHaveAllowAnonymousAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ExternalLoginCallback),
                new[] { typeof(string), typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public async Task Get_ExternalLoginCallback_RedirectsToReturnUrl_WhenLoginIsSuccessful()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetExternalLoginInfoAsync()
                .Returns(Task.FromResult(new ExternalLoginInfo(new ClaimsPrincipal(), "provider", "key", "name")));
            signInManager.ExternalLoginSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Success));

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(AccountController.Register));

            // Act
            var result = await controller.ExternalLoginCallback("url");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectResult>());
            Assert.That((result as RedirectResult).Url, Is.EqualTo("url"));
        }

        [Test]
        public async Task Get_ExternalLoginCallback_RedirectsToLogin_WhenThereIsARemoteError()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.ExternalLoginCallback("url", "remote error");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(AccountController.Login)));
            Assert.That(controller.ErrorMessage, Does.StartWith("Error from external provider:"));
        }

        [Test]
        public async Task Get_ExternalLoginCallback_RedirectsToLogin_WhenFailingToGetExternalLoginInfo()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetExternalLoginInfoAsync()
                .Returns(Task.FromResult((ExternalLoginInfo)null));

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.ExternalLoginCallback("url");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(AccountController.Login)));
        }

        [Test]
        public async Task Get_ExternalLoginCallback_RedirectsToLockout_WhenAccountIsLockedOut()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetExternalLoginInfoAsync()
                .Returns(Task.FromResult(new ExternalLoginInfo(new ClaimsPrincipal(), "provider", "key", "name")));
            signInManager.ExternalLoginSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.LockedOut));

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.ExternalLoginCallback("url");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(AccountController.Lockout)));
        }

        [Test]
        public async Task Get_ExternalLoginCallback_ReturnsViewAndModel_WhenUserDoesNotHaveAnAccount()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetExternalLoginInfoAsync()
                .Returns(Task.FromResult(new ExternalLoginInfo(new ClaimsPrincipal(), "provider", "key", "name")));
            signInManager.ExternalLoginSignInAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Failed));

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.ExternalLoginCallback("url");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<ExternalLoginViewModel>());
            Assert.That((result as ViewResult).ViewName, Is.EqualTo("ExternalLogin"));
            Assert.That(controller.ViewData["ReturnUrl"], Is.EqualTo("url"));
            Assert.That(controller.ViewData["LoginProvider"], Is.EqualTo("provider"));
        }
        #endregion

        #region ExternalLoginConfirmation tests
        [Test]
        public void Post_ExternalLoginConfirmation_ShouldHaveHttpPostAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ExternalLoginConfirmation),
                new[] { typeof(ExternalLoginViewModel), typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpPostAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Post_ExternalLoginConfirmation_ShouldHaveAllowAnonymousAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ExternalLoginConfirmation),
                new[] { typeof(ExternalLoginViewModel), typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public async Task Post_ExternalLoginConfirmation_RedirectsToReturnUrl_WhenLoginIsSuccessfullyAdded()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.CreateAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(IdentityResult.Success));
            userManager.AddLoginAsync(Arg.Any<ApplicationUser>(), Arg.Any<UserLoginInfo>())
                .Returns(Task.FromResult(IdentityResult.Success));
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetExternalLoginInfoAsync()
                .Returns(Task.FromResult(new ExternalLoginInfo(new ClaimsPrincipal(), "provider", "key", "name")));
            signInManager.SignInAsync(Arg.Any<ApplicationUser>(), Arg.Any<bool>())
                .Returns(Task.FromResult(0));

            var model = new ExternalLoginViewModel
            {
                Email = "user@domain.tld"
            };

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(AccountController.Register));

            // Act
            var result = await controller.ExternalLoginConfirmation(model, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectResult>());
            Assert.That((result as RedirectResult).Url, Is.EqualTo("url"));
        }

        [Test]
        public async Task Post_ExternalLoginConfirmation_ReturnsViewAndModel_WhenModelStateIsNotValid()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var model = new ExternalLoginViewModel();

            var controller = CreateControllerInstance(signInManager);
            controller.ModelState.AddModelError("", "");

            // Act
            var result = await controller.ExternalLoginConfirmation(model, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).ViewName, Is.EqualTo(nameof(AccountController.ExternalLogin)));
            Assert.That((result as ViewResult).Model, Is.InstanceOf< ExternalLoginViewModel>());
            Assert.That(controller.ViewData["ReturnUrl"], Is.EqualTo("url"));
        }

        [Test]
        public async Task Post_ExternalLoginConfirmation_AddsModelErrorsAndReturnsViewAndModel_WhenFailingToCreateUser()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.CreateAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "code", Description = "description" })));
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetExternalLoginInfoAsync()
                .Returns(Task.FromResult(new ExternalLoginInfo(new ClaimsPrincipal(), "provider", "key", "name")));

            var model = new ExternalLoginViewModel
            {
                Email = "user@domain.tld"
            };

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(AccountController.Register));

            // Act
            var result = await controller.ExternalLoginConfirmation(model, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(controller.ViewData["ReturnUrl"], Is.EqualTo("url"));
            Assert.That(controller.ModelState.ErrorCount, Is.GreaterThan(0));
        }

        [Test]
        public async Task Post_ExternalLoginConfirmation_AddsModelErrorsAndReturnsViewAndModel_WhenFailingToAddLogin()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.CreateAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(IdentityResult.Success));
            userManager.AddLoginAsync(Arg.Any<ApplicationUser>(), Arg.Any<UserLoginInfo>())
                .Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "code", Description = "description" })));
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetExternalLoginInfoAsync()
                .Returns(Task.FromResult(new ExternalLoginInfo(new ClaimsPrincipal(), "provider", "key", "name")));

            var model = new ExternalLoginViewModel
            {
                Email = "user@domain.tld"
            };

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(AccountController.Register));

            // Act
            var result = await controller.ExternalLoginConfirmation(model, "url");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(controller.ViewData["ReturnUrl"], Is.EqualTo("url"));
            Assert.That(controller.ModelState.ErrorCount, Is.GreaterThan(0));
        }

        [Test]
        public void Post_ExternalLoginConfirmation_ThrowsApplicationException_WhenFailingToGetExternalLoginInfo()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetExternalLoginInfoAsync()
                .Returns(Task.FromResult((ExternalLoginInfo)null));

            var model = new ExternalLoginViewModel();

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.ExternalLoginConfirmation(model, "url");
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Is.EqualTo("Error loading external login information during confirmation."));
        }
        #endregion

        #region ConfirmEmail tests
        [Test]
        public void Get_ConfirmEmail_ShouldHaveHttpGetAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ConfirmEmail),
                new[] { typeof(string), typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpGetAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Get_ConfirmEmail_ShouldHaveAllowAnonymousAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ConfirmEmail),
                new[] { typeof(string), typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public async Task Get_ConfirmEmail_ReturnsView_WhenConfirmEmailSucceeds()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.FindByIdAsync(Arg.Any<string>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.ConfirmEmailAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(Task.FromResult(IdentityResult.Success));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.ConfirmEmail("id", "code");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).ViewName, Is.EqualTo(nameof(AccountController.ConfirmEmail)));
        }

        [Test]
        public async Task Get_ConfirmEmail_ReturnsErrorView_WhenConfirmEmailFails()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.FindByIdAsync(Arg.Any<string>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.ConfirmEmailAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "code", Description = "description" })));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.ConfirmEmail("id", "code");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).ViewName, Is.EqualTo("Error"));
        }

        [Test]
        public async Task Get_ConfirmEmail_RedirectsToHomeIndex_WhenAnArgumentIsNull()
        {
            // Arrange
            var name = nameof(HomeController);
            var redirectControllerName = name.Substring(0, name.LastIndexOf("Controller"));

            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.ConfirmEmail("id", null);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ControllerName, Is.EqualTo(redirectControllerName));
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(HomeController.Index)));
        }

        [Test]
        public void Get_ConfirmEmail_ThrowsApplicationException_WhenUserIsNotFound()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.FindByIdAsync(Arg.Any<string>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.ConfirmEmail("id", "code");
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }
        #endregion

        #region ForgotPassword tests
        [Test]
        public void Get_ForgotPassword_ShouldHaveHttpGetAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ForgotPassword), Type.EmptyTypes);
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpGetAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Get_ForgotPassword_ShouldHaveAllowAnonymousAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ForgotPassword), Type.EmptyTypes);
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Get_ForgotPassword_ReturnsView_WhenCalled()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = controller.ForgotPassword();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public void Post_ForgotPassword_ShouldHaveHttpPostAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ForgotPassword),
                new[] { typeof(ForgotPasswordViewModel) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpPostAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Post_ForgotPassword_ShouldHaveAllowAnonymousAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ForgotPassword),
                new[] { typeof(ForgotPasswordViewModel) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public async Task Post_ForgotPassword_RedirectsToForgotPasswordConfirmation_WhenEmailIsSent()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.FindByEmailAsync(Arg.Any<string>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.IsEmailConfirmedAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(true));
            userManager.GeneratePasswordResetTokenAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult("token"));
            var signInManager = CreateSignInManagerStub(userManager);

            var model = new ForgotPasswordViewModel
            {
                Email = "user@domain.tld"
            };

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(AccountController.ForgotPassword));

            // Act
            var result = await controller.ForgotPassword(model);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(AccountController.ForgotPasswordConfirmation)));
        }

        [Test]
        public async Task Post_ForgotPassword_ReturnsViewAndModel_WhenModelStateIsNotValid()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var model = new ForgotPasswordViewModel();

            var controller = CreateControllerInstance(signInManager);
            controller.ModelState.AddModelError("", "");

            // Act
            var result = await controller.ForgotPassword(model);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf< ForgotPasswordViewModel>());
        }

        [Test]
        public async Task Post_ForgotPassword_RedirectsToForgotPasswordConfirmation_WhenUserIsNotFound()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.FindByEmailAsync(Arg.Any<string>())
                .Returns(Task.FromResult((ApplicationUser)null));
            userManager.IsEmailConfirmedAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(true));
            var signInManager = CreateSignInManagerStub(userManager);

            var model = new ForgotPasswordViewModel();

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.ForgotPassword(model);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(AccountController.ForgotPasswordConfirmation)));
        }

        [Test]
        public async Task Post_ForgotPassword_RedirectsToForgotPasswordConfirmation_WhenEmailIsNotConfirmed()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.FindByEmailAsync(Arg.Any<string>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.IsEmailConfirmedAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(false));
            var signInManager = CreateSignInManagerStub(userManager);

            var model = new ForgotPasswordViewModel();

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.ForgotPassword(model);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(AccountController.ForgotPasswordConfirmation)));
        }
        #endregion

        #region ForgotPasswordConfirmation tests
        [Test]
        public void Get_ForgotPasswordConfirmation_ShouldHaveHttpGetAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ForgotPasswordConfirmation), Type.EmptyTypes);
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpGetAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Get_ForgotPasswordConfirmation_ShouldHaveAllowAnonymousAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ForgotPasswordConfirmation), Type.EmptyTypes);
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Get_ForgotPasswordConfirmation_ReturnsView_WhenCalled()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = controller.ForgotPasswordConfirmation();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }
        #endregion

        #region ResetPassword tests
        [Test]
        public void Get_ResetPassword_ShouldHaveHttpGetAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ResetPassword), new [] { typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpGetAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Get_ResetPassword_ShouldHaveAllowAnonymousAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ResetPassword), new[] { typeof(string) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Get_ResetPassword_ReturnsViewAndModel_WhenCalled()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = controller.ResetPassword("code");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<ResetPasswordViewModel>());
            Assert.That(((result as ViewResult).Model as ResetPasswordViewModel).Code, Is.EqualTo("code"));
        }

        [Test]
        public void Get_ResetPassword_ThrowsApplicationException_WhenCodeIsNull()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            void Act()
            {
                var result = controller.ResetPassword((string)null);
            }

            // Assert
            var ex = Assert.Throws<ApplicationException>(Act);
            Assert.That(ex.Message, Is.EqualTo("A code must be supplied for password reset."));
        }

        [Test]
        public void Post_ResetPassword_ShouldHaveHttpPostAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ResetPassword),
                new[] { typeof(ResetPasswordViewModel) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpPostAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Post_ResetPassword_ShouldHaveAllowAnonymousAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ResetPassword),
                new[] { typeof(ResetPasswordViewModel) });
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public async Task Post_ResetPassword_RedirectsToResetPasswordConfirmation_WhenPasswordIsReset()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.FindByEmailAsync(Arg.Any<string>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.ResetPasswordAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(IdentityResult.Success));
            var signInManager = CreateSignInManagerStub(userManager);

            var model = new ResetPasswordViewModel
            {
                Code = "code",
                Email = "user@domain.tld",
                Password = "password"
            };

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(AccountController.ForgotPassword));

            // Act
            var result = await controller.ResetPassword(model);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(AccountController.ResetPasswordConfirmation)));
        }

        [Test]
        public async Task Post_ResetPassword_ReturnsViewAndModel_WhenModelStateIsNotValid()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var model = new ResetPasswordViewModel();

            var controller = CreateControllerInstance(signInManager);
            controller.ModelState.AddModelError("", "");

            // Act
            var result = await controller.ResetPassword(model);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf< ResetPasswordViewModel>());
        }

        [Test]
        public async Task Post_ResetPassword_RedirectsToResetPasswordConfirmation_WhenUserIsNotFound()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.FindByEmailAsync(Arg.Any<string>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var model = new ResetPasswordViewModel
            {
                Code = "code",
                Email = "user@domain.tld",
                Password = "password"
            };

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.ResetPassword(model);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(AccountController.ResetPasswordConfirmation)));
        }

        [Test]
        public async Task Post_ResetPassword_AddsModelErrorsAndReturnsView_WhenResetPasswordFails()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.FindByEmailAsync(Arg.Any<string>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.ResetPasswordAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "code", Description = "description" })));
            var signInManager = CreateSignInManagerStub(userManager);

            var model = new ResetPasswordViewModel
            {
                Code = "code",
                Email = "user@domain.tld",
                Password = "password"
            };

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(AccountController.ForgotPassword));

            // Act
            var result = await controller.ResetPassword(model);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(controller.ModelState.ErrorCount, Is.GreaterThan(0));
        }
        #endregion

        #region ResetPasswordConfirmation tests
        [Test]
        public void Get_ResetPasswordConfirmation_ShouldHaveHttpGetAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ResetPasswordConfirmation), Type.EmptyTypes);
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpGetAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Get_ResetPasswordConfirmation_ShouldHaveAllowAnonymousAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.ResetPasswordConfirmation), Type.EmptyTypes);
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(AllowAnonymousAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Get_ResetPasswordConfirmation_ReturnsView_WhenCalled()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = controller.ResetPasswordConfirmation();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }
        #endregion

        #region AccessDenied tests
        [Test]
        public void Get_AccessDenied_ShouldHaveHttpGetAttribute()
        {
            // Arrange
            var type = typeof(AccountController);
            var method = type.GetMethod(nameof(AccountController.AccessDenied), Type.EmptyTypes);
            var attributes = method.GetCustomAttributes(false);
            var wantedAttributeType = typeof(HttpGetAttribute);

            // Act
            var result = attributes.FirstOrDefault(a => a.GetType() == wantedAttributeType);

            // Assert
            Assert.That(result, Is.Not.Null, $"No {wantedAttributeType.Name} found.");
        }

        [Test]
        public void Get_AccessDenied_ReturnsView_WhenCalled()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = controller.AccessDenied();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }
        #endregion
    }
}
