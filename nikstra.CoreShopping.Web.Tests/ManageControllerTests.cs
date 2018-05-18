using Castle.Core.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
