using Castle.Core.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nikstra.CoreShopping.Web.Controllers;
using nikstra.CoreShopping.Web.Models;
using nikstra.CoreShopping.Web.Models.ManageViewModels;
using nikstra.CoreShopping.Web.Services;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace nikstra.CoreShopping.Web.Tests
{
    [TestFixture, SetCulture("en-US")]
    class ManageControllerTests
    {
        #region Index() method tests
        [Test]
        public async Task GetIndex_SuccessfullyReturnsViewAndModel_WhenUserExists()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            var result = await controller.Index();

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(((result as ViewResult).Model as IndexViewModel)?.Username, Is.EqualTo("user@domain.tld"));
        }

        [Test]
        public void GetIndex_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            async Task Act()
            {
                var result = await controller.Index();
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public async Task PostIndex_UpdatesUserProfile_WhenModelDataDiffers()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));

            userManager.SetEmailAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(IdentityResult.Success);

            userManager.SetPhoneNumberAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(IdentityResult.Success);

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            var model = CreateIndexViewModel();
            model.Email = "user@domain.new";
            model.PhoneNumber = "999-224466";

            var result = await controller.Index(model);

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.Index)));
            Assert.That(controller.StatusMessage, Is.EqualTo("Your profile has been updated"));
        }

        [Test]
        public async Task PostIndex_ReturnsViewAndModel_WhenModelStateIsNotValid()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));

            userManager.SetEmailAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(IdentityResult.Success);

            userManager.SetPhoneNumberAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(IdentityResult.Success);

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            controller.ModelState.AddModelError("","");
            var model = CreateIndexViewModel();
            model.Email = "user@domain.new";
            model.PhoneNumber = "999-224466";

            var result = await controller.Index(model);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(((result as ViewResult).Model as IndexViewModel)?.Username, Is.EqualTo("user@domain.tld"));
        }

        [Test]
        public void PostIndex_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            var model = new IndexViewModel();

            async Task Act()
            {
                var result = await controller.Index(model);
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public void PostIndex_ThrowsAnApplicationException_WhenFailingToUpdateEmail()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));

            userManager.SetEmailAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(new IdentityResult());

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            var model = CreateIndexViewModel();
            model.Email = "user@domain.new";

            async Task Act()
            {
                var result = await controller.Index(model);
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unexpected error occurred setting email for user with ID"));
        }

        [Test]
        public void PostIndex_ThrowsAnApplicationException_WhenFailingToUpdatePhoneNumber()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));

            userManager.SetEmailAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(IdentityResult.Success);

            userManager.SetPhoneNumberAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(new IdentityResult());

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            var model = CreateIndexViewModel();
            model.PhoneNumber = "999-224466";

            async Task Act()
            {
                var result = await controller.Index(model);
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unexpected error occurred setting phone number for user with ID"));
        }
        #endregion

        #region SendVerificationEmail() method tests
        [Test]
        public async Task PostSendVerificationEmail_SuccessfullySendsEmail_WhenUserExists()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));

            userManager.GenerateEmailConfirmationTokenAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult("token"));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.Request.Scheme = "HTTP";
            controller.Url = Substitute.For<IUrlHelper>();
            controller.Url.EmailConfirmationLink(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns("dummy url");
            var model = CreateIndexViewModel();

            var result = await controller.SendVerificationEmail(model);

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.Index)));
            Assert.That(controller.StatusMessage, Is.EqualTo("Verification email sent. Please check your email."));
        }

        [Test]
        public async Task PostSendVerificationEmail_ReturnsViewAndModel_WhenModelStateIsNotValid()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            controller.ModelState.AddModelError("", "");
            var model = CreateIndexViewModel();

            var result = await controller.SendVerificationEmail(model);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(((result as ViewResult).Model as IndexViewModel)?.Username, Is.EqualTo("user@domain.tld"));
        }

        [Test]
        public void PostSendVerificationEmail_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            var model = new IndexViewModel();

            async Task Act()
            {
                var result = await controller.SendVerificationEmail(model);
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }
        #endregion

        #region ChangePassword() metod tests
        [Test]
        public async Task GetChangePassword_RedirectsToSetPassword_WhenUserDoesNotHaveAPassword()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));

            userManager.HasPasswordAsync(Arg.Any<ApplicationUser>())
                .Returns(false);

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            var result = await controller.ChangePassword();

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.SetPassword)));
        }

        [Test]
        public async Task GetChangePassword_ReturnsViewAndModel_WhenUserHasAPassword()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));

            userManager.HasPasswordAsync(Arg.Any<ApplicationUser>())
                .Returns(true);

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            var model = new ChangePasswordViewModel();

            var result = await controller.ChangePassword();

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<ChangePasswordViewModel>());
        }

        [Test]
        public void GetChangePassword_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            async Task Act()
            {
                var result = await controller.ChangePassword();
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public async Task PostChangePassword_RedirectsToChangePassword_WhenPasswordSuccessfullyChanged()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));
            userManager.ChangePasswordAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(IdentityResult.Success));

            var signInManager = CreateSignInManagerMock(userManager);
            signInManager.SignInAsync(Arg.Any<ApplicationUser>(), isPersistent: false)
                .Returns(Task.FromResult(0));

            var controller = CreateControllerInstance(signInManager);

            var model = new ChangePasswordViewModel
            {
                OldPassword = "password"
            };

            var result = await controller.ChangePassword(model);

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.ChangePassword)));
            Assert.That(controller.StatusMessage, Is.EqualTo("Your password has been changed."));
        }

        [Test]
        public async Task PostChangePassword_ReturnsViewAndModel_WhenFailingToChangePassword()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));
            userManager.ChangePasswordAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(IdentityResult.Failed()));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            var model = new ChangePasswordViewModel
            {
                OldPassword = "password"
            };

            var result = await controller.ChangePassword(model);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(((result as ViewResult).Model as ChangePasswordViewModel)?.OldPassword, Is.EqualTo("password"));
        }

        [Test]
        public async Task PostChangePassword_ReturnsViewAndModel_WhenModelStateIsNotValid()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            controller.ModelState.AddModelError("", "");
            var model = new ChangePasswordViewModel
            {
                OldPassword = "password"
            };

            var result = await controller.ChangePassword(model);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(((result as ViewResult).Model as ChangePasswordViewModel)?.OldPassword, Is.EqualTo("password"));
        }

        [Test]
        public void PostChangePassword_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            var model = new ChangePasswordViewModel();

            async Task Act()
            {
                var result = await controller.ChangePassword(model);
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }
        #endregion

        #region SetPassword() method tests
        [Test]
        public async Task GetSetPassword_SuccessfullyReturnsViewAndModel_WhenUserDoesNotHaveAPassword()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));

            userManager.HasPasswordAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(false));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            controller.StatusMessage = "message";

            var result = await controller.SetPassword();

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(((result as ViewResult).Model as SetPasswordViewModel).StatusMessage, Is.EqualTo("message"));
        }

        [Test]
        public async Task GetSetPassword_RedirectsToChangePassword_WhenUserHasAPassword()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));

            userManager.HasPasswordAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(true));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            var result = await controller.SetPassword();

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.ChangePassword)));
        }

        [Test]
        public void GetSetPassword_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            async Task Act()
            {
                var result = await controller.SetPassword();
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public async Task PostSetPassword_RedirectsToSetPassword_WhenPasswordIsSuccessfullySet()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));
            userManager.AddPasswordAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>()).
                Returns(Task.FromResult(IdentityResult.Success));

            var signInManager = CreateSignInManagerMock(userManager);
            signInManager.SignInAsync(Arg.Any<ApplicationUser>(), isPersistent: false)
                .Returns(Task.FromResult(0));

            var controller = CreateControllerInstance(signInManager);
            var model = new SetPasswordViewModel();

            var result = await controller.SetPassword(model);

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.SetPassword)));
            Assert.That(controller.StatusMessage, Is.EqualTo("Your password has been set."));
        }

        [Test]
        public async Task PostSetPassword_ReturnsViewAndModel_WhenModelStateIsNotValid()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            controller.ModelState.AddModelError("", "");
            var model = new SetPasswordViewModel();

            var result = await controller.SetPassword(model);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<SetPasswordViewModel>());
        }

        [Test]
        public async Task PostSetPassword_ReturnsViewAndModel_WhenPasswordCouldNotBeSet()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));
            userManager.AddPasswordAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>()).
                Returns(Task.FromResult(IdentityResult.Failed(
                    new IdentityError { Code = "code", Description = "description" })));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            var model = new SetPasswordViewModel();

            var result = await controller.SetPassword(model);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).ViewData.ModelState.ErrorCount, Is.GreaterThan(0));
        }

        [Test]
        public void PostSetPassword_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            var model = new SetPasswordViewModel();

            async Task Act()
            {
                var result = await controller.SetPassword(model);
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }
        #endregion

        #region ExternalLogins() method tests
        [Test]
        public async Task GetExternalLogins_ReturnsViewAndModel_WhenUserHasNoExternalLogins()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));
            userManager.GetLoginsAsync(Arg.Any<ApplicationUser>())
                .Returns(
                    Task.FromResult<IList<UserLoginInfo>>(new List<UserLoginInfo> { new UserLoginInfo("provider", "name", "key") })
                );
            userManager.HasPasswordAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(true));

            var signInManager = CreateSignInManagerMock(userManager);
            signInManager.GetExternalAuthenticationSchemesAsync()
                .Returns(
                    Task.FromResult<IEnumerable<AuthenticationScheme>>(new List<AuthenticationScheme>())
                );
            var controller = CreateControllerInstance(signInManager);

            var result = await controller.ExternalLogins();

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(((result as ViewResult).Model as ExternalLoginsViewModel).StatusMessage, Is.EqualTo(controller.StatusMessage));
            Assert.That(((result as ViewResult).Model as ExternalLoginsViewModel).ShowRemoveButton, Is.True);
        }

        [Test]
        public async Task GetExternalLogins_ReturnsViewAndModel_WhenUserHasExternalLogins()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));
            userManager.GetLoginsAsync(Arg.Any<ApplicationUser>())
                .Returns(
                    Task.FromResult<IList<UserLoginInfo>>(new List<UserLoginInfo> { new UserLoginInfo("provider", "name", "key") })
                );
            userManager.HasPasswordAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(true));

            var signInManager = CreateSignInManagerMock(userManager);
            signInManager.GetExternalAuthenticationSchemesAsync()
                .Returns(
                    Task.FromResult<IEnumerable<AuthenticationScheme>>(
                        new List<AuthenticationScheme>
                        {
                            new AuthenticationScheme("name", "displayName", Substitute.For<IAuthenticationHandler>().GetType())
                        })
                );
            var controller = CreateControllerInstance(signInManager);

            var result = await controller.ExternalLogins();

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(((result as ViewResult).Model as ExternalLoginsViewModel).StatusMessage, Is.EqualTo(controller.StatusMessage));
            Assert.That(((result as ViewResult).Model as ExternalLoginsViewModel).ShowRemoveButton, Is.True);
        }

        [Test]
        public async Task GetExternalLogins_SetsModelShowRemoveButtonToFalse_WhenUserHasOnlyOneLogin()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));
            userManager.GetLoginsAsync(Arg.Any<ApplicationUser>())
                .Returns(
                    Task.FromResult<IList<UserLoginInfo>>(new List<UserLoginInfo>())
                );
            userManager.HasPasswordAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(false));

            var signInManager = CreateSignInManagerMock(userManager);
            signInManager.GetExternalAuthenticationSchemesAsync()
                .Returns(
                    Task.FromResult<IEnumerable<AuthenticationScheme>>(
                        new List<AuthenticationScheme>())
                );
            var controller = CreateControllerInstance(signInManager);

            var result = await controller.ExternalLogins();

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(((result as ViewResult).Model as ExternalLoginsViewModel).StatusMessage, Is.EqualTo(controller.StatusMessage));
            Assert.That(((result as ViewResult).Model as ExternalLoginsViewModel).ShowRemoveButton, Is.False);
        }

        [Test]
        public void GetExternalLogins_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            async Task Act()
            {
                var result = await controller.ExternalLogins();
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }
        #endregion

        #region LinkLogin() method tests
        [Test]
        public async Task PostLinkLogin_ReturnsChallengeResult_WhenSuccessful()
        {
            var userManager = CreateUserManagerMock();

            var signInManager = CreateSignInManagerMock(userManager);
            signInManager.ConfigureExternalAuthenticationProperties(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new AuthenticationProperties());

            var controller = CreateControllerInstance(signInManager);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.RequestServices = Substitute.For<IServiceProvider>();
            controller.HttpContext.RequestServices.GetService(typeof(IAuthenticationService))
                .Returns(Substitute.For<IAuthenticationService>());
            controller.Url = Substitute.For<IUrlHelper>();
            controller.Url.EmailConfirmationLink(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns("dummy url");

            var result = await controller.LinkLogin("provider");

            Assert.That(result, Is.InstanceOf<ChallengeResult>());
            Assert.That((result as ChallengeResult).Properties, Is.InstanceOf<AuthenticationProperties>());
            Assert.That((result as ChallengeResult).AuthenticationSchemes[0], Is.EqualTo("provider"));
        }
        #endregion

        #region LinkLoginCallback() method tests
        [Test]
        public async Task GetLinkLoginCallback_RedirectsToExternalLogins_WhenLoginIsSuccessfullyAdded()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));
            userManager.AddLoginAsync(Arg.Any<ApplicationUser>(), Arg.Any<ExternalLoginInfo>())
                .Returns(Task.FromResult(IdentityResult.Success));

            var signInManager = CreateSignInManagerMock(userManager);
            signInManager.GetExternalLoginInfoAsync(Arg.Any<string>())
                .Returns(Task.FromResult(Substitute.For<ExternalLoginInfo>(
                    new ClaimsPrincipal(), "loginProvider", "providerKey", "displayName")));

            var controller = CreateControllerInstance(signInManager);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.HttpContext.RequestServices = Substitute.For<IServiceProvider>();
            controller.HttpContext.RequestServices.GetService(typeof(IAuthenticationService))
                .Returns(Substitute.For<IAuthenticationService>());
            controller.Url = Substitute.For<IUrlHelper>();

            var result = await controller.LinkLoginCallback();

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.ExternalLogins)));
            Assert.That(controller.StatusMessage, Is.EqualTo("The external login was added."));
        }

        [Test]
        public void GetLinkLoginCallback_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            async Task Act()
            {
                var result = await controller.LinkLoginCallback();
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public void GetLinkLoginCallback_ThrowsAnApplicationException_WhenFailingToGetExternalLoginInfo()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));

            var signInManager = CreateSignInManagerMock(userManager);
            signInManager.GetExternalLoginInfoAsync(Arg.Any<string>())
                .Returns((ExternalLoginInfo)null);
            var controller = CreateControllerInstance(signInManager);

            async Task Act()
            {
                var result = await controller.LinkLoginCallback();
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unexpected error occurred loading external login info for user with ID"));
        }

        [Test]
        public void GetLinkLoginCallback_ThrowsAnApplicationException_WhenFailingToAddLogin()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));
            userManager.AddLoginAsync(Arg.Any<ApplicationUser>(), Arg.Any<ExternalLoginInfo>())
                .Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "code", Description = "description" })));

            var signInManager = CreateSignInManagerMock(userManager);
            signInManager.GetExternalLoginInfoAsync(Arg.Any<string>())
                .Returns(Task.FromResult(Substitute.For<ExternalLoginInfo>(
                    new ClaimsPrincipal(), "loginProvider", "providerKey", "displayName")));
            var controller = CreateControllerInstance(signInManager);

            async Task Act()
            {
                var result = await controller.LinkLoginCallback();
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unexpected error occurred adding external login for user with ID"));
        }
        #endregion

        #region RemoveLogin() method tests
        [Test]
        public async Task PostRemoveLogin_RedirectsToExternalLogins_WhenLoginIsRemoved()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));
            userManager.RemoveLoginAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(IdentityResult.Success));

            var signInManager = CreateSignInManagerMock(userManager);
            signInManager.SignInAsync(Arg.Any<ApplicationUser>(), Arg.Any<bool>())
                .Returns(Task.FromResult(0));
            var controller = CreateControllerInstance(signInManager);
            var model = new RemoveLoginViewModel();

            var result = await controller.RemoveLogin(model);

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.ExternalLogins)));
            Assert.That(controller.StatusMessage, Is.EqualTo("The external login was removed."));
        }

        [Test]
        public void PostRemoveLogin_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            var model = new RemoveLoginViewModel();

            async Task Act()
            {
                var result = await controller.RemoveLogin(model);
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public void PostRemoveLogin_ThrowsAnApplicationException_WhenFailingToRemoveLogin()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));
            userManager.RemoveLoginAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "code", Description = "description" })));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            var model = new RemoveLoginViewModel();

            async Task Act()
            {
                var result = await controller.RemoveLogin(model);
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unexpected error occurred removing external login for user with ID"));
        }
        #endregion

        #region TwoFactorAuthentication() method tests
        [Test]
        public async Task GetTwoFactorAuthentication_ReturnsViewAndModel_WhenUserExists()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));
            userManager.GetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult("key"));
            userManager.CountRecoveryCodesAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(1));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            var result = await controller.TwoFactorAuthentication();

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<TwoFactorAuthenticationViewModel>());
            Assert.That(((result as ViewResult).Model as TwoFactorAuthenticationViewModel).HasAuthenticator, Is.True);
            Assert.That(((result as ViewResult).Model as TwoFactorAuthenticationViewModel).Is2faEnabled, Is.False);
            Assert.That(((result as ViewResult).Model as TwoFactorAuthenticationViewModel).RecoveryCodesLeft, Is.EqualTo(1));
        }

        public void GetTwoFactorAuthentication_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            async Task Act()
            {
                var result = await controller.TwoFactorAuthentication();
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }
        #endregion

        #region Disable2faWarning() method tests
        [Test]
        public async Task GetDisable2faWarning_ReturnsView_WhenUserExists()
        {
            var user = CreateApplicationUser();
            user.TwoFactorEnabled = true;
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(user));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            var result = await controller.Disable2faWarning();

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).ViewName, Is.EqualTo(nameof(ManageController.Disable2fa)));
        }

        [Test]
        public void GetDisable2faWarning_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            async Task Act()
            {
                var result = await controller.Disable2faWarning();
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public void GetDisable2faWarning_ThrowsAnApplicationException_WhenUserDoesNotHave2FactorAuthEnabled()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            async Task Act()
            {
                var result = await controller.Disable2faWarning();
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unexpected error occured disabling 2FA for user with ID"));
        }
        #endregion

        #region Disable2fa() method tests
        [Test]
        public async Task PostDisable2fa_RedirectsToTwoFactorAuthentication_WhenUseExists()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));
            userManager.SetTwoFactorEnabledAsync(Arg.Any<ApplicationUser>(), Arg.Any<bool>())
                .Returns(Task.FromResult(IdentityResult.Success));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            var result = await controller.Disable2fa();

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.TwoFactorAuthentication)));
        }

        [Test]
        public void PostDisable2fa_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            async Task Act()
            {
                var result = await controller.Disable2fa();
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public void PostDisable2fa_ThrowsAnApplicationException_WhenFailingToDisable2FactorAuth()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));
            userManager.SetTwoFactorEnabledAsync(Arg.Any<ApplicationUser>(), Arg.Any<bool>())
                .Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "code", Description = "description" })));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            async Task Act()
            {
                var result = await controller.Disable2fa();
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unexpected error occured disabling 2FA for user with ID"));
        }
        #endregion

        #region EnableAuthenticator() method tests
        [Test]
        public async Task GetEnableAuthenticator_ReturnsViewAndModel_WhenUserExists()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));
            userManager.GetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult("key"));
            userManager.ResetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(IdentityResult.Success));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            var result = await controller.EnableAuthenticator();

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<EnableAuthenticatorViewModel>());
        }

        [Test]
        public void GetEnableAuthenticator_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);

            async Task Act()
            {
                var result = await controller.EnableAuthenticator();
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public async Task PostEnableAuthenticator_RedirectsToShowRecoveryCodes_WhenUserExistsAndTwoFactorTokenIsValid()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));
            userManager.VerifyTwoFactorTokenAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(true));
            userManager.SetTwoFactorEnabledAsync(Arg.Any<ApplicationUser>(), Arg.Any<bool>())
                .Returns(Task.FromResult(IdentityResult.Success));
            userManager.GenerateNewTwoFactorRecoveryCodesAsync(Arg.Any<ApplicationUser>(), Arg.Any<int>())
                .Returns(Task.FromResult<IEnumerable<string>>(new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" }));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.TempData = new TempDataDictionary(controller.HttpContext, Substitute.For<ITempDataProvider>());
            var model = new EnableAuthenticatorViewModel { AuthenticatorUri = "uri", Code = "code", SharedKey = "key" };

            var result = await controller.EnableAuthenticator(model);

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.ShowRecoveryCodes)));
        }

        [Test]
        public async Task PostEnableAuthenticator_ReturnsViewAndModel_WhenModelStateIsNotValid()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));
            userManager.GetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult("key"));
            userManager.ResetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(IdentityResult.Success));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            controller.ModelState.AddModelError("", "");
            var model = new EnableAuthenticatorViewModel();

            var result = await controller.EnableAuthenticator(model);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<EnableAuthenticatorViewModel>());
        }

        [Test]
        public async Task PostEnableAuthenticator_ReturnsViewAndModel_WhenTwoFactorTokenIsNotValid()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateApplicationUser()));
            userManager.VerifyTwoFactorTokenAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(false));
            userManager.GetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult("key"));
            userManager.ResetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(IdentityResult.Success));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            var model = new EnableAuthenticatorViewModel { AuthenticatorUri = "uri", Code = "code", SharedKey = "key" };

            var result = await controller.EnableAuthenticator(model);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<EnableAuthenticatorViewModel>());
            Assert.That(controller.ModelState["Code"].Errors[0].ErrorMessage, Is.EqualTo("Verification code is invalid."));
        }

        [Test]
        public void PostEnableAuthenticator_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            var userManager = CreateUserManagerMock();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));

            var signInManager = CreateSignInManagerMock(userManager);
            var controller = CreateControllerInstance(signInManager);
            var model = new EnableAuthenticatorViewModel();

            async Task Act()
            {
                var result = await controller.EnableAuthenticator(model);
            }

            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
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

        private IndexViewModel CreateIndexViewModel() =>
            new IndexViewModel
            {
                Username = "user@domain.tld",
                Email = "user@domain.tld",
                PhoneNumber = "555-224466",
                IsEmailConfirmed = false,
                StatusMessage = ""
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

        private ManageController CreateControllerInstance(SignInManager<ApplicationUser> signInManager) =>
            new ManageController(
                signInManager.UserManager,
                signInManager,
                Substitute.For<IEmailSender>(),
                Substitute.For<ILogger<ManageController>>(),
                Substitute.For<UrlEncoder>()
                );
    }
}
