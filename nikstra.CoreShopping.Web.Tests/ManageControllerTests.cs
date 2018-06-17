using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using nikstra.CoreShopping.Web.Controllers;
using nikstra.CoreShopping.Web.Models;
using nikstra.CoreShopping.Web.Models.ManageViewModels;
using nikstra.CoreShopping.Web.Services;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace nikstra.CoreShopping.Web.Tests
{
    [TestFixture, SetCulture("en-US")]
    class ManageControllerTests : ControllerTestBase
    {
        private ManageController CreateControllerInstance(SignInManager<ApplicationUser> signInManager) =>
            new ManageController(
                signInManager.UserManager,
                signInManager,
                Substitute.For<IEmailSender>(),
                Substitute.For<ILogger<ManageController>>(),
                Substitute.For<UrlEncoder>()
                );

        #region Index() method tests
        [Test]
        public async Task Get_Index_SuccessfullyReturnsViewAndModel_WhenUserExists()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.Index();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(((result as ViewResult).Model as IndexViewModel)?.Username, Is.EqualTo("user@domain.tld"));
        }

        [Test]
        public void Get_Index_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.Index();
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public async Task Post_Index_UpdatesUserProfile_WhenModelDataDiffers()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.SetEmailAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(IdentityResult.Success);
            userManager.SetPhoneNumberAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(IdentityResult.Success);
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            var model = new IndexViewModel
            {
                Username = "user@domain.new",
                Email = "user@domain.tld",
                PhoneNumber = "999-224466",
                IsEmailConfirmed = false,
                StatusMessage = ""
            };

            // Act
            var result = await controller.Index(model);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.Index)));
            Assert.That(controller.StatusMessage, Is.EqualTo("Your profile has been updated"));
        }

        [Test]
        public async Task Post_Index_ReturnsViewAndModel_WhenModelStateIsNotValid()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.SetEmailAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(IdentityResult.Success);
            userManager.SetPhoneNumberAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(IdentityResult.Success);
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);
            controller.ModelState.AddModelError("", "");

            var model = new IndexViewModel
            {
                Username = "user@domain.tld",
                Email = "user@domain.new",
                PhoneNumber = "999-224466",
                IsEmailConfirmed = false,
                StatusMessage = ""
            };

            // Act
            var result = await controller.Index(model);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(((result as ViewResult).Model as IndexViewModel)?.Username, Is.EqualTo("user@domain.tld"));
        }

        [Test]
        public void Post_Index_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            var model = new IndexViewModel();

            // Act
            async Task Act()
            {
                var result = await controller.Index(model);
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public void Post_Index_ThrowsAnApplicationException_WhenFailingToUpdateEmail()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.SetEmailAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(new IdentityResult());
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            var model = new IndexViewModel
            {
                Username = "user@domain.tld",
                Email = "user@domain.new",
                PhoneNumber = "555-224466",
                IsEmailConfirmed = false,
                StatusMessage = ""
            };

            // Act
            async Task Act()
            {
                var result = await controller.Index(model);
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unexpected error occurred setting email for user with ID"));
        }

        [Test]
        public void Post_Index_ThrowsAnApplicationException_WhenFailingToUpdatePhoneNumber()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.SetEmailAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(IdentityResult.Success);
            userManager.SetPhoneNumberAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
                .Returns(new IdentityResult());
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            var model = new IndexViewModel
            {
                Username = "user@domain.tld",
                Email = "user@domain.tld",
                PhoneNumber = "999-224466",
                IsEmailConfirmed = false,
                StatusMessage = ""
            };

            // Act
            async Task Act()
            {
                var result = await controller.Index(model);
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unexpected error occurred setting phone number for user with ID"));
        }
        #endregion

        #region SendVerificationEmail() method tests
        [Test]
        public async Task Post_SendVerificationEmail_SuccessfullySendsEmail_WhenUserExists()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.GenerateEmailConfirmationTokenAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult("token"));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            controller.Request.Scheme = "HTTP";
            controller.Url = Substitute.For<IUrlHelper>();
            controller.Url.EmailConfirmationLink(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns("dummy url");

            var model = new IndexViewModel
            {
                Username = "user@domain.tld",
                Email = "user@domain.tld",
                PhoneNumber = "555-224466",
                IsEmailConfirmed = false,
                StatusMessage = ""
            };

            // Act
            var result = await controller.SendVerificationEmail(model);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.Index)));
            Assert.That(controller.StatusMessage, Is.EqualTo("Verification email sent. Please check your email."));
        }

        [Test]
        public async Task Post_SendVerificationEmail_ReturnsViewAndModel_WhenModelStateIsNotValid()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);
            controller.ModelState.AddModelError("", "");

            var model = new IndexViewModel
            {
                Username = "user@domain.tld",
                Email = "user@domain.tld",
                PhoneNumber = "555-224466",
                IsEmailConfirmed = false,
                StatusMessage = ""
            };

            // Act
            var result = await controller.SendVerificationEmail(model);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(((result as ViewResult).Model as IndexViewModel)?.Username, Is.EqualTo("user@domain.tld"));
        }

        [Test]
        public void Post_SendVerificationEmail_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            var model = new IndexViewModel();

            // Act
            async Task Act()
            {
                var result = await controller.SendVerificationEmail(model);
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }
        #endregion

        #region ChangePassword() metod tests
        [Test]
        public async Task Get_ChangePassword_RedirectsToSetPassword_WhenUserDoesNotHaveAPassword()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.HasPasswordAsync(Arg.Any<ApplicationUser>())
                .Returns(false);
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.ChangePassword();

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.SetPassword)));
        }

        [Test]
        public async Task Get_ChangePassword_ReturnsViewAndModel_WhenUserHasAPassword()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.HasPasswordAsync(Arg.Any<ApplicationUser>())
                .Returns(true);
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            var model = new ChangePasswordViewModel();

            // Act
            var result = await controller.ChangePassword();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<ChangePasswordViewModel>());
        }

        [Test]
        public void Get_ChangePassword_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.ChangePassword();
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public async Task Post_ChangePassword_RedirectsToChangePassword_WhenPasswordSuccessfullyChanged()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.ChangePasswordAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(IdentityResult.Success));
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.SignInAsync(Arg.Any<ApplicationUser>(), isPersistent: false)
                .Returns(Task.FromResult(0));

            var controller = CreateControllerInstance(signInManager);

            var model = new ChangePasswordViewModel
            {
                OldPassword = "password"
            };

            // Act
            var result = await controller.ChangePassword(model);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.ChangePassword)));
            Assert.That(controller.StatusMessage, Is.EqualTo("Your password has been changed."));
        }

        [Test]
        public async Task Post_ChangePassword_ReturnsViewAndModel_WhenFailingToChangePassword()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.ChangePasswordAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(IdentityResult.Failed()));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            var model = new ChangePasswordViewModel
            {
                OldPassword = "password"
            };

            // Act
            var result = await controller.ChangePassword(model);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(((result as ViewResult).Model as ChangePasswordViewModel)?.OldPassword, Is.EqualTo("password"));
        }

        [Test]
        public async Task Post_ChangePassword_ReturnsViewAndModel_WhenModelStateIsNotValid()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);
            controller.ModelState.AddModelError("", "");

            var model = new ChangePasswordViewModel
            {
                OldPassword = "password"
            };

            // Act
            var result = await controller.ChangePassword(model);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(((result as ViewResult).Model as ChangePasswordViewModel)?.OldPassword, Is.EqualTo("password"));
        }

        [Test]
        public void Post_ChangePassword_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            var model = new ChangePasswordViewModel();

            // Act
            async Task Act()
            {
                var result = await controller.ChangePassword(model);
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }
        #endregion

        #region SetPassword() method tests
        [Test]
        public async Task Get_SetPassword_SuccessfullyReturnsViewAndModel_WhenUserDoesNotHaveAPassword()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.HasPasswordAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(false));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);
            controller.StatusMessage = "message";

            // Act
            var result = await controller.SetPassword();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(((result as ViewResult).Model as SetPasswordViewModel).StatusMessage, Is.EqualTo("message"));
        }

        [Test]
        public async Task Get_SetPassword_RedirectsToChangePassword_WhenUserHasAPassword()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.HasPasswordAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(true));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.SetPassword();

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.ChangePassword)));
        }

        [Test]
        public void Get_SetPassword_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.SetPassword();
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public async Task Post_SetPassword_RedirectsToSetPassword_WhenPasswordIsSuccessfullySet()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.AddPasswordAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>()).
                Returns(Task.FromResult(IdentityResult.Success));
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.SignInAsync(Arg.Any<ApplicationUser>(), isPersistent: false)
                .Returns(Task.FromResult(0));

            var controller = CreateControllerInstance(signInManager);
            var model = new SetPasswordViewModel();

            // Act
            var result = await controller.SetPassword(model);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.SetPassword)));
            Assert.That(controller.StatusMessage, Is.EqualTo("Your password has been set."));
        }

        [Test]
        public async Task Post_SetPassword_ReturnsViewAndModel_WhenModelStateIsNotValid()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);
            controller.ModelState.AddModelError("", "");

            var model = new SetPasswordViewModel();

            // Act
            var result = await controller.SetPassword(model);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<SetPasswordViewModel>());
        }

        [Test]
        public async Task Post_SetPassword_ReturnsViewAndModel_WhenPasswordCouldNotBeSet()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.AddPasswordAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>()).
                Returns(Task.FromResult(IdentityResult.Failed(
                    new IdentityError { Code = "code", Description = "description" })));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            var model = new SetPasswordViewModel();

            // Act
            var result = await controller.SetPassword(model);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).ViewData.ModelState.ErrorCount, Is.GreaterThan(0));
        }

        [Test]
        public void Post_SetPassword_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            var model = new SetPasswordViewModel();

            // Act
            async Task Act()
            {
                var result = await controller.SetPassword(model);
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }
        #endregion

        #region ExternalLogins() method tests
        [Test]
        public async Task Get_ExternalLogins_ReturnsViewAndModel_WhenUserHasNoExternalLogins()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.GetLoginsAsync(Arg.Any<ApplicationUser>())
                .Returns(
                    Task.FromResult<IList<UserLoginInfo>>(new List<UserLoginInfo> { new UserLoginInfo("provider", "name", "key") })
                );
            userManager.HasPasswordAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(true));
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetExternalAuthenticationSchemesAsync()
                .Returns(
                    Task.FromResult<IEnumerable<AuthenticationScheme>>(new List<AuthenticationScheme>())
                );

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.ExternalLogins();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(((result as ViewResult).Model as ExternalLoginsViewModel).StatusMessage, Is.EqualTo(controller.StatusMessage));
            Assert.That(((result as ViewResult).Model as ExternalLoginsViewModel).ShowRemoveButton, Is.True);
        }

        [Test]
        public async Task Get_ExternalLogins_ReturnsViewAndModel_WhenUserHasExternalLogins()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.GetLoginsAsync(Arg.Any<ApplicationUser>())
                .Returns(
                    Task.FromResult<IList<UserLoginInfo>>(new List<UserLoginInfo> { new UserLoginInfo("provider", "name", "key") })
                );
            userManager.HasPasswordAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(true));
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetExternalAuthenticationSchemesAsync()
                .Returns(
                    Task.FromResult<IEnumerable<AuthenticationScheme>>(
                        new List<AuthenticationScheme>
                        {
                            new AuthenticationScheme("name", "displayName", Substitute.For<IAuthenticationHandler>().GetType())
                        })
                );

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.ExternalLogins();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(((result as ViewResult).Model as ExternalLoginsViewModel).StatusMessage, Is.EqualTo(controller.StatusMessage));
            Assert.That(((result as ViewResult).Model as ExternalLoginsViewModel).ShowRemoveButton, Is.True);
        }

        [Test]
        public async Task Get_ExternalLogins_SetsModelShowRemoveButtonToFalse_WhenUserHasOnlyOneLogin()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.GetLoginsAsync(Arg.Any<ApplicationUser>())
                .Returns(
                    Task.FromResult<IList<UserLoginInfo>>(new List<UserLoginInfo>())
                );
            userManager.HasPasswordAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(false));
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetExternalAuthenticationSchemesAsync()
                .Returns(
                    Task.FromResult<IEnumerable<AuthenticationScheme>>(
                        new List<AuthenticationScheme>())
                );

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.ExternalLogins();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That(((result as ViewResult).Model as ExternalLoginsViewModel).StatusMessage, Is.EqualTo(controller.StatusMessage));
            Assert.That(((result as ViewResult).Model as ExternalLoginsViewModel).ShowRemoveButton, Is.False);
        }

        [Test]
        public void Get_ExternalLogins_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.ExternalLogins();
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }
        #endregion

        #region LinkLogin() method tests
        [Test]
        public async Task Post_LinkLogin_ReturnsChallengeResult_WhenSuccessful()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.ConfigureExternalAuthenticationProperties(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(new AuthenticationProperties());

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(ManageController.LinkLogin));

            // Act
            var result = await controller.LinkLogin("provider");

            // Assert
            Assert.That(result, Is.InstanceOf<ChallengeResult>());
            Assert.That((result as ChallengeResult).Properties, Is.InstanceOf<AuthenticationProperties>());
            Assert.That((result as ChallengeResult).AuthenticationSchemes[0], Is.EqualTo("provider"));
        }
        #endregion

        #region LinkLoginCallback() method tests
        [Test]
        public async Task Get_LinkLoginCallback_RedirectsToExternalLogins_WhenLoginIsSuccessfullyAdded()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.AddLoginAsync(Arg.Any<ApplicationUser>(), Arg.Any<ExternalLoginInfo>())
                .Returns(Task.FromResult(IdentityResult.Success));
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetExternalLoginInfoAsync(Arg.Any<string>())
                .Returns(Task.FromResult(Substitute.For<ExternalLoginInfo>(
                    new ClaimsPrincipal(), "loginProvider", "providerKey", "displayName")));

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(ManageController.LinkLoginCallback));

            // Act
            var result = await controller.LinkLoginCallback();

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.ExternalLogins)));
            Assert.That(controller.StatusMessage, Is.EqualTo("The external login was added."));
        }

        [Test]
        public void Get_LinkLoginCallback_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.LinkLoginCallback();
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public void Get_LinkLoginCallback_ThrowsAnApplicationException_WhenFailingToGetExternalLoginInfo()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetExternalLoginInfoAsync(Arg.Any<string>())
                .Returns((ExternalLoginInfo)null);

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.LinkLoginCallback();
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unexpected error occurred loading external login info for user with ID"));
        }

        [Test]
        public void Get_LinkLoginCallback_ThrowsAnApplicationException_WhenFailingToAddLogin()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.AddLoginAsync(Arg.Any<ApplicationUser>(), Arg.Any<ExternalLoginInfo>())
                .Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "code", Description = "description" })));
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.GetExternalLoginInfoAsync(Arg.Any<string>())
                .Returns(Task.FromResult(Substitute.For<ExternalLoginInfo>(
                    new ClaimsPrincipal(), "loginProvider", "providerKey", "displayName")));

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.LinkLoginCallback();
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unexpected error occurred adding external login for user with ID"));
        }
        #endregion

        #region RemoveLogin() method tests
        [Test]
        public async Task Post_RemoveLogin_RedirectsToExternalLogins_WhenLoginIsRemoved()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.RemoveLoginAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(IdentityResult.Success));
            var signInManager = CreateSignInManagerStub(userManager);
            signInManager.SignInAsync(Arg.Any<ApplicationUser>(), Arg.Any<bool>())
                .Returns(Task.FromResult(0));

            var controller = CreateControllerInstance(signInManager);

            var model = new RemoveLoginViewModel();

            // Act
            var result = await controller.RemoveLogin(model);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.ExternalLogins)));
            Assert.That(controller.StatusMessage, Is.EqualTo("The external login was removed."));
        }

        [Test]
        public void Post_RemoveLogin_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            var model = new RemoveLoginViewModel();

            // Act
            async Task Act()
            {
                var result = await controller.RemoveLogin(model);
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public void Post_RemoveLogin_ThrowsAnApplicationException_WhenFailingToRemoveLogin()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.RemoveLoginAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "code", Description = "description" })));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            var model = new RemoveLoginViewModel();

            // Act
            async Task Act()
            {
                var result = await controller.RemoveLogin(model);
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unexpected error occurred removing external login for user with ID"));
        }
        #endregion

        #region TwoFactorAuthentication() method tests
        [Test]
        public async Task Get_TwoFactorAuthentication_ReturnsViewAndModel_WhenUserExists()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.GetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult("key"));
            userManager.CountRecoveryCodesAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(1));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.TwoFactorAuthentication();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<TwoFactorAuthenticationViewModel>());
            Assert.That(((result as ViewResult).Model as TwoFactorAuthenticationViewModel).HasAuthenticator, Is.True);
            Assert.That(((result as ViewResult).Model as TwoFactorAuthenticationViewModel).Is2faEnabled, Is.False);
            Assert.That(((result as ViewResult).Model as TwoFactorAuthenticationViewModel).RecoveryCodesLeft, Is.EqualTo(1));
        }

        public void Get_TwoFactorAuthentication_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.TwoFactorAuthentication();
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }
        #endregion

        #region Disable2faWarning() method tests
        [Test]
        public async Task Get_Disable2faWarning_ReturnsView_WhenUserExists()
        {
            // Arrange
            var user = CreateGoodApplicationUser();
            user.TwoFactorEnabled = true;
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(user));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.Disable2faWarning();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).ViewName, Is.EqualTo(nameof(ManageController.Disable2fa)));
        }

        [Test]
        public void Get_Disable2faWarning_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.Disable2faWarning();
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public void Get_Disable2faWarning_ThrowsAnApplicationException_WhenUserDoesNotHave2FactorAuthEnabled()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.Disable2faWarning();
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unexpected error occured disabling 2FA for user with ID"));
        }
        #endregion

        #region Disable2fa() method tests
        [Test]
        public async Task Post_Disable2fa_RedirectsToTwoFactorAuthentication_WhenUseExists()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.SetTwoFactorEnabledAsync(Arg.Any<ApplicationUser>(), Arg.Any<bool>())
                .Returns(Task.FromResult(IdentityResult.Success));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.Disable2fa();

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.TwoFactorAuthentication)));
        }

        [Test]
        public void Post_Disable2fa_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.Disable2fa();
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public void Post_Disable2fa_ThrowsAnApplicationException_WhenFailingToDisable2FactorAuth()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.SetTwoFactorEnabledAsync(Arg.Any<ApplicationUser>(), Arg.Any<bool>())
                .Returns(Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "code", Description = "description" })));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.Disable2fa();
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unexpected error occured disabling 2FA for user with ID"));
        }
        #endregion

        #region EnableAuthenticator() method tests
        [Test]
        public async Task Get_EnableAuthenticator_ReturnsViewAndModel_WhenUserExists()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.GetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult("key"));
            userManager.ResetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(IdentityResult.Success));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.EnableAuthenticator();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<EnableAuthenticatorViewModel>());
        }

        [Test]
        public void Get_EnableAuthenticator_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.EnableAuthenticator();
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public async Task Post_EnableAuthenticator_RedirectsToShowRecoveryCodes_WhenUserExistsAndTwoFactorTokenIsValid()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.VerifyTwoFactorTokenAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(true));
            userManager.SetTwoFactorEnabledAsync(Arg.Any<ApplicationUser>(), Arg.Any<bool>())
                .Returns(Task.FromResult(IdentityResult.Success));
            userManager.GenerateNewTwoFactorRecoveryCodesAsync(Arg.Any<ApplicationUser>(), Arg.Any<int>())
                .Returns(Task.FromResult<IEnumerable<string>>(new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" }));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(ManageController.EnableAuthenticator));

            var model = new EnableAuthenticatorViewModel
            {
                AuthenticatorUri = "uri",
                Code = "code",
                SharedKey = "key"
            };

            // Act
            var result = await controller.EnableAuthenticator(model);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.ShowRecoveryCodes)));
        }

        [Test]
        public async Task Post_EnableAuthenticator_ReturnsViewAndModel_WhenModelStateIsNotValid()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.GetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult("key"));
            userManager.ResetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(IdentityResult.Success));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);
            controller.ModelState.AddModelError("", "");

            var model = new EnableAuthenticatorViewModel();

            // Act
            var result = await controller.EnableAuthenticator(model);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<EnableAuthenticatorViewModel>());
        }

        [Test]
        public async Task Post_EnableAuthenticator_ReturnsViewAndModel_WhenTwoFactorTokenIsNotValid()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.VerifyTwoFactorTokenAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(false));
            userManager.GetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult("key"));
            userManager.ResetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(IdentityResult.Success));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            var model = new EnableAuthenticatorViewModel
            {
                AuthenticatorUri = "uri",
                Code = "code",
                SharedKey = "key"
            };

            // Act
            var result = await controller.EnableAuthenticator(model);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<EnableAuthenticatorViewModel>());
            Assert.That(controller.ModelState["Code"].Errors[0].ErrorMessage, Is.EqualTo("Verification code is invalid."));
        }

        [Test]
        public void Post_EnableAuthenticator_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            var model = new EnableAuthenticatorViewModel();

            // Act
            async Task Act()
            {
                var result = await controller.EnableAuthenticator(model);
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }
        #endregion

        #region ShowRecoveryCodes() method tests
        [Test]
        public void Get_ShowRecoveryCodes_ReturnsViewAndModel_WhenRecoveryCodesExists()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(ManageController.ShowRecoveryCodes));
            controller.TempData["RecoveryCodesKey"] = new string[] { };

            // Act
            var result = controller.ShowRecoveryCodes();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).Model, Is.InstanceOf<ShowRecoveryCodesViewModel>());
        }

        [Test]
        public void Get_ShowRecoveryCodes_RedirectsToTwoFactorAuthentication_WhenNoRecoveryCodesExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);
            InjectControllerContextStub(controller, nameof(ManageController.ShowRecoveryCodes));
            controller.TempData["RecoveryCodesKey"] = null;

            // Act
            var result = controller.ShowRecoveryCodes();

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.TwoFactorAuthentication)));
        }
        #endregion

        #region ResetAuthenticatorWarning() method tests
        [Test]
        public void Get_ResetAuthenticatorWarning_ReturnsViewResetAuthenticator_WhenCalled()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = controller.ResetAuthenticatorWarning();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).ViewName, Is.EqualTo(nameof(ManageController.ResetAuthenticator)));
        }
        #endregion

        #region ResetAuthenticator() method tests
        [Test]
        public async Task Post_ResetAuthenticator_RedirectsToEnableAuthenticator_WhenAuthenticationIsReset()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            userManager.SetTwoFactorEnabledAsync(Arg.Any<ApplicationUser>(), Arg.Any<bool>())
                .Returns(Task.FromResult(IdentityResult.Success));
            userManager.ResetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>())
                .Returns(Task.FromResult(IdentityResult.Success));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.ResetAuthenticator();

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            Assert.That((result as RedirectToActionResult).ActionName, Is.EqualTo(nameof(ManageController.EnableAuthenticator)));
        }

        [Test]
        public void Post_ResetAuthenticator_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.ResetAuthenticator();
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }
        #endregion

        #region GenerateRecoveryCodesWarning() method tests
        [Test]
        public async Task Get_GenerateRecoveryCodesWarning_ReturnsViewGenerateRecoveryCodes_WhenUserHasTwoFactorEnabled()
        {
            // Arrange
            var user = CreateGoodApplicationUser();
            user.TwoFactorEnabled = true;
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(user));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.GenerateRecoveryCodesWarning();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).ViewName, Is.EqualTo(nameof(ManageController.GenerateRecoveryCodes)));
        }

        [Test]
        public void Get_GenerateRecoveryCodesWarning_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.GenerateRecoveryCodesWarning();
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public void Get_GenerateRecoveryCodesWarning_ThrowsAnApplicationException_WhenUserDoesNotHaveTwoFactorEnabled()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.GenerateRecoveryCodesWarning();
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Cannot generate recovery codes for user with ID"));
        }
        #endregion

        #region GenerateRecoveryCodes() method tests
        [Test]
        public async Task Post_GenerateRecoveryCodes_ReturnsViewShowRecoveryCodesAndModel_WhenUserHasTwoFactorEnabled()
        {
            // Arrange
            var user = CreateGoodApplicationUser();
            user.TwoFactorEnabled = true;
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(user));
            userManager.GenerateNewTwoFactorRecoveryCodesAsync(Arg.Any<ApplicationUser>(), Arg.Any<int>())
                .Returns(Task.FromResult<IEnumerable<string>>(new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" }));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            var result = await controller.GenerateRecoveryCodes();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            Assert.That((result as ViewResult).ViewName, Is.EqualTo(nameof(ManageController.ShowRecoveryCodes)));
            Assert.That((result as ViewResult).Model, Is.InstanceOf<ShowRecoveryCodesViewModel>());
        }

        [Test]
        public void Post_GenerateRecoveryCodes_ThrowsAnApplicationException_WhenUserDoesNotExist()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult((ApplicationUser)null));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.GenerateRecoveryCodes();
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Unable to load user with ID"));
        }

        [Test]
        public void Post_GenerateRecoveryCodes_ThrowsAnApplicationException_WhenUserDoesNotHaveTwoFactorEnabled()
        {
            // Arrange
            var userManager = CreateUserManagerStub();
            userManager.GetUserAsync(Arg.Any<ClaimsPrincipal>())
                .Returns(Task.FromResult(CreateGoodApplicationUser()));
            var signInManager = CreateSignInManagerStub(userManager);

            var controller = CreateControllerInstance(signInManager);

            // Act
            async Task Act()
            {
                var result = await controller.GenerateRecoveryCodes();
            }

            // Assert
            var ex = Assert.ThrowsAsync<ApplicationException>(Act);
            Assert.That(ex.Message, Does.StartWith("Cannot generate recovery codes for user with ID"));
        }
        #endregion
    }
}
