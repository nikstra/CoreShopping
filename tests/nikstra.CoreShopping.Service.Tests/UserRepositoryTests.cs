using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;
using nikstra.CoreShopping.Service.Data;
using nikstra.CoreShopping.Service.Models;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

// https://docs.microsoft.com/en-us/ef/core/miscellaneous/testing/in-memory

namespace nikstra.CoreShopping.Service.Tests
{
    [TestFixture, SetCulture("en-US")]
    public class UserRepositoryTests
    {
        private Dictionary<Type, Func<DbContextOptions<UserDbContext>, DbContext>> _typeMap =
            new Dictionary<Type, Func<DbContextOptions<UserDbContext>, DbContext>>
        {
            { typeof(UserDbContext), options => new UserDbContext(options) },
            { typeof(SeededUserDbContext), options => new SeededUserDbContext(options) }
        };

        private T CreateContext<T>(string databaseName, bool dropDb = false)
            where T : DbContext
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;

            var context = _typeMap.ContainsKey(typeof(T))
                ? _typeMap[typeof(T)](options) as T
                : throw new ApplicationException($"Cannot create instance of {typeof(T).Name}");

            if(dropDb)
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }

            return context;
        }

        private class UserRepositoryAccessor : UserRepository
        {
            private UserRepositoryAccessor()
                : base(new UserDbContext())
            {
            }
            public static string AuthenticatorKeyName => _authenticatorKeyTokenName;
            public static string InternalLoginProvider => _internalLoginProvider;
            public static string RecoveryCodeTokenName => _recoveryCodeTokenName;
        }
        string AuthenticatorKeyName = UserRepositoryAccessor.AuthenticatorKeyName;
        string InternalLoginProvider = UserRepositoryAccessor.InternalLoginProvider;
        string RecoveryCodeTokenName = UserRepositoryAccessor.RecoveryCodeTokenName;
        ILookupNormalizer _normalizer = new UpperInvariantLookupNormalizer();

        #region AddClaimsAsync tests
        [Test]
        public async Task AddClaimsAsync_AddsClaims_WhenCalled()
        {
            // Arrange
            var dbName = nameof(AddClaimsAsync_AddsClaims_WhenCalled);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            var claims = new List<Claim>
            {
                new Claim("type", "value"),
                new Claim("type", "value")
            };

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.AddClaimsAsync(user, claims);
                await repository.UpdateAsync(user);
            }

            // Assert
            using (var resultContext = CreateContext<SeededUserDbContext>(dbName))
            {
                var result = await resultContext.UserClaims.ToListAsync();
                Assert.That(result.Count, Is.EqualTo(2));
                Assert.That(result.Any(c => c.ClaimType == "type"), Is.True);
                Assert.That(result.Any(c => c.ClaimValue == "value"), Is.True);
                Assert.That(result.Any(c => c.UserId == user.Id), Is.True);
            }
        }

        [Test]
        public void AddClaimsAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var claims = Substitute.For<IEnumerable<Claim>>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.AddClaimsAsync(null, claims);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void AddClaimsAsync_ThrowsArgumentNullException_WhenClaimsIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.AddClaimsAsync(user, null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("claims"));
        }

        [Test]
        public void AddClaimsAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.UserClaims = Substitute.For<DbSet<ShopUserClaim>>();
            var user = new ShopUser();
            var claims = Substitute.For<IEnumerable<Claim>>();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.AddClaimsAsync(user, claims, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
            context.UserClaims.DidNotReceiveWithAnyArgs().AddRange(Arg.Any<IEnumerable<ShopUserClaim>>());
        }
        #endregion

        #region AddLoginAsync tests
        [Test]
        public async Task AddLoginAsync_AddsLogin_WhenCalled()
        {
            // Arrange
            var dbName = nameof(AddLoginAsync_AddsLogin_WhenCalled);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            var loginInfo = new UserLoginInfo("provider", "key", "name");

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.AddLoginAsync(user, loginInfo);
                await repository.UpdateAsync(user);
            }

            // Assert
            using (var resultContext = CreateContext<SeededUserDbContext>(dbName))
            {
                var result = await resultContext.UserLogins.SingleOrDefaultAsync();
                Assert.That(result?.LoginProvider, Is.EqualTo(loginInfo.LoginProvider));
                Assert.That(result?.ProviderKey, Is.EqualTo(loginInfo.ProviderKey));
                Assert.That(result?.ProviderDisplayName, Is.EqualTo(loginInfo.ProviderDisplayName));
                Assert.That(result?.UserId, Is.EqualTo(user.Id));
            }
        }

        [Test]
        public void AddLoginAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var loginInfo = new UserLoginInfo("provider", "key", "name");

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.AddLoginAsync(null, loginInfo);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void AddLoginAsync_ThrowsArgumentNullException_WhenLoginIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.AddLoginAsync(user, null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("login"));
        }

        [Test]
        public void AddLoginAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.UserLogins = Substitute.For<DbSet<ShopUserLogin>>();
            var user = new ShopUser();
            var loginInfo = new UserLoginInfo("provider", "key", "name");
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.AddLoginAsync(user, loginInfo, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
            context.UserLogins.DidNotReceiveWithAnyArgs().Add(Arg.Any<ShopUserLogin>());
        }
        #endregion

        #region AddToRoleAsync tests
        [Test]
        public async Task AddToRoleAsync_AddsUserToRole_WhenCalled()
        {
            // Arrange
            var dbName = nameof(AddToRoleAsync_AddsUserToRole_WhenCalled);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            var role = context.Roles.First();

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.AddToRoleAsync(user, role.Name);
                await repository.UpdateAsync(user);
            }

            // Assert
            using (var resultContext = CreateContext<SeededUserDbContext>(dbName))
            {
                var resultUser = resultContext.Users.Find(user.Id);
                var resultRole = resultContext.UserRoles
                    .Include(ur => ur.ShopRole)
                    .Where(ur => ur.UserId == resultUser.Id)
                    .Select(ur => ur.ShopRole)
                    .FirstOrDefault();

                Assert.That(resultRole?.Name, Is.EqualTo(role.Name));
            }
        }

        [Test]
        public void AddToRoleAsync_ThrowsInvalidOperationException_WhenRoleDoesNotExist()
        {
            // Arrange
            var dbName = nameof(AddToRoleAsync_ThrowsInvalidOperationException_WhenRoleDoesNotExist);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.AddToRoleAsync(user, "roleName");
                }
            }

            // Assert
            Assert.ThrowsAsync<InvalidOperationException>(Act);
        }

        [Test]
        public void AddToRoleAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.AddToRoleAsync(null, "roleName");
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void AddToRoleAsync_ThrowsArgumentException_WhenRoleIsNullOrWhitespace(string role)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.AddToRoleAsync(user, role);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("roleName"));
        }

        [Test]
        public void AddToRoleAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.Roles = Substitute.For<DbSet<ShopRole>>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.AddToRoleAsync(user, "roleName", cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
            Assert.That(user.Roles.Count, Is.Zero);
        }
        #endregion

        #region CountCodesAsync tests
        [TestCase("", 0)]
        [TestCase("one", 1)]
        [TestCase("one;two", 2)]
        [TestCase("one;two;three", 3)]
        public async Task CountCodesAsync_ReturnsNumberOfAvailableCodes_WhenCalled(string codes, int expectedCount)
        {
            // Arrange
            var dbName = nameof(CountCodesAsync_ReturnsNumberOfAvailableCodes_WhenCalled);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            var userToken = new ShopUserToken
            {
                UserId = user.Id,
                LoginProvider = InternalLoginProvider,
                Name = RecoveryCodeTokenName,
                Value = codes
            };
            context.UserTokens.Add(userToken);
            context.SaveChanges();

            // Act
            int result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.CountCodesAsync(user);
            }

            // Assert
            Assert.That(result, Is.EqualTo(expectedCount));
        }

        [Test]
        public void CountCodesAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.CountCodesAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void CountCodesAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.CountCodesAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
        }
        #endregion

        #region CreateAsync tests
        [Test]
        public async Task CreateAsync_AddsUser_WhenCalled()
        {
            // Arrange
            var dbName = nameof(CountCodesAsync_ReturnsNumberOfAvailableCodes_WhenCalled);
            var context = CreateContext<UserDbContext>(dbName, true);
            var user = new ShopUser { UserName = "user@domain.tld" };

            // Act
            IdentityResult result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.CreateAsync(user);
            }

            // Assert
            using (var resultContext = CreateContext<UserDbContext>(dbName))
            {
                Assert.That(result, Is.EqualTo(IdentityResult.Success));
                var resultUser = resultContext.Users.Find(user.Id);
                Assert.That(resultUser.UserName, Is.EqualTo(user.UserName));
            }
        }

        [Test]
        public void CreateAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.CreateAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void CreateAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.Users = Substitute.For<DbSet<ShopUser>>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.CreateAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
            context.Users.DidNotReceiveWithAnyArgs().Add(Arg.Any<ShopUser>());
        }
        #endregion

        #region DeleteAsync tests
        [Test]
        public async Task DeleteAsync_RemovesUser_WhenCalled()
        {
            // Arrange
            var dbName = nameof(DeleteAsync_RemovesUser_WhenCalled);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();

            // Act
            IdentityResult result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.DeleteAsync(user);
            }

            // Assert
            using (var resultContext = CreateContext<SeededUserDbContext>(dbName))
            {

                Assert.That(result, Is.EqualTo(IdentityResult.Success));
                var resultUser = resultContext.Users.Find(user.Id);
                Assert.That(resultUser, Is.Null);
            }
        }

        [Test]
        public async Task DeleteAsync_ReturnsIdentityResultFailed_WhenSaveChangesThrowsDbUpdateConcurrencyException()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.Users = Substitute.For<DbSet<ShopUser>>();
            var entityEntries = new List<IUpdateEntry> { Substitute.For<IUpdateEntry>() };
            var exception = new DbUpdateConcurrencyException("Update concurrency exception", entityEntries);
            context.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromException<int>(exception));

            // Act
            IdentityResult result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.DeleteAsync(new ShopUser());
            }

            // Assert
            Assert.That(result, Is.Not.EqualTo(IdentityResult.Success));
        }

        [Test]
        public void DeleteAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.DeleteAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void DeleteAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.Users = Substitute.For<DbSet<ShopUser>>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.DeleteAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
            context.Users.DidNotReceiveWithAnyArgs().Remove(Arg.Any<ShopUser>());
        }
        #endregion

        #region FindByEmailAsync tests
        [Test]
        public async Task FindByEmailAsync_ReturnsAShopUser_WhenAUserIsFound()
        {
            // Arrange
            var dbName = nameof(FindByEmailAsync_ReturnsAShopUser_WhenAUserIsFound);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            var normalizedEmail = _normalizer.Normalize(user.Email);
            user.NormalizedEmail = normalizedEmail;

            // Act
            ShopUser result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.FindByEmailAsync(normalizedEmail);
            }

            // Assert
            Assert.That(result?.UserName, Is.EqualTo(user.UserName));
        }

        [Test]
        public async Task FindByEmailAsync_ReturnsNull_WhenAUserIsNotFound()
        {
            // Arrange
            var dbName = nameof(FindByEmailAsync_ReturnsNull_WhenAUserIsNotFound);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            var normalizedEmail = _normalizer.Normalize(user.Email);
            user.NormalizedEmail = normalizedEmail;
            context.SaveChanges();

            // Act
            ShopUser result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.FindByEmailAsync("nonexisting@domain.tld");
            }

            // Assert
            Assert.That(result, Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void FindByEmailAsync_ThrowsArgumentException_WhenNormalizedEmailIsNullOrWhitespace(string normalizedEmail)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.FindByEmailAsync(normalizedEmail);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("normalizedEmail"));
        }

        [Test]
        public void FindByEmailAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.FindByEmailAsync("user@domain.tld", cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region FindByIdAsync tests
        [Test]
        public async Task FindByIdAsync_ReturnsAShopUser_WhenAUserIsFound()
        {
            // Arrange
            var dbName = nameof(FindByIdAsync_ReturnsAShopUser_WhenAUserIsFound);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();

            // Act
            ShopUser result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.FindByIdAsync(user.Id);
            }

            // Assert
            Assert.That(result?.UserName, Is.EqualTo(user.UserName));
        }

        [Test]
        public async Task FindByIdAsync_ReturnsNull_WhenAUserIsNotFound()
        {
            // Arrange
            var dbName = nameof(FindByIdAsync_ReturnsNull_WhenAUserIsNotFound);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();

            // Act
            ShopUser result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.FindByIdAsync("nonexistingUserId");
            }

            // Assert
            Assert.That(result, Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void FindByIdAsync_ThrowsArgumentException_WhenUserIdIsNullOrWhitespace(string id)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.FindByIdAsync(id);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("userId"));
        }

        [Test]
        public void FindByIdAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.FindByIdAsync("userId", cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region FindByLoginAsync tests
        [Test]
        public async Task FindByLoginAsync_ReturnsAShopUser_WhenAUserIsFound()
        {
            // Arrange
            var dbName = nameof(FindByLoginAsync_ReturnsAShopUser_WhenAUserIsFound);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            context.UserLogins.Add(new ShopUserLogin
            {
                UserId = user.Id,
                LoginProvider = "provider",
                ProviderDisplayName = "name",
                ProviderKey = "key"
            });
            context.SaveChanges();

            // Act
            ShopUser result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.FindByLoginAsync("provider", "key");
            }

            // Assert
            Assert.That(result?.UserName, Is.EqualTo(user.UserName));
        }

        [Test]
        public async Task FindByLoginAsync_ReturnsNull_WhenAUserIsNotFound()
        {
            // Arrange
            var dbName = nameof(FindByLoginAsync_ReturnsNull_WhenAUserIsNotFound);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();

            // Act
            ShopUser result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.FindByLoginAsync("provider", "key");
            }

            // Assert
            Assert.That(result, Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void FindByLoginAsync_ThrowsArgumentException_WhenLoginProviderIsNullOrWhitespace(string provider)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.FindByLoginAsync(provider, "key");
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("loginProvider"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void FindByLoginAsync_ThrowsArgumentException_WhenProviderKeyIsNullOrWhitespace(string key)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.FindByLoginAsync("provider", key);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("providerKey"));
        }

        [Test]
        public void FindByLoginAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.FindByLoginAsync("provider", "key", cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
        }
        #endregion

        #region FindByNameAsync tests
        [Test]
        public async Task FindByNameAsync_ReturnsAShopUser_WhenAUserIsFound()
        {
            // Arrange
            var dbName = nameof(FindByNameAsync_ReturnsAShopUser_WhenAUserIsFound);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            var normalizedUserName = _normalizer.Normalize(user.UserName);
            user.NormalizedUserName = normalizedUserName;
            context.SaveChanges();

            // Act
            ShopUser result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.FindByNameAsync(normalizedUserName);
            }

            // Assert
            Assert.That(result?.UserName, Is.EqualTo(user.UserName));
        }

        [Test]
        public async Task FindByNameAsync_ReturnsNull_WhenAUserIsNotFound()
        {
            // Arrange
            var dbName = nameof(FindByNameAsync_ReturnsNull_WhenAUserIsNotFound);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            var normalizedUserName = _normalizer.Normalize(user.UserName);
            user.NormalizedUserName = normalizedUserName;
            context.SaveChanges();

            // Act
            ShopUser result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.FindByNameAsync("nonexisting@domain.tld");
            }

            // Assert
            Assert.That(result, Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void FindByNameAsync_ThrowsArgumentException_WhenNormalizedUserNameIsNullOrWhitespace(string normalizedUserName)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.FindByNameAsync(normalizedUserName);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("normalizedUserName"));
        }

        [Test]
        public void FindByNameAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.FindByNameAsync("nonexisting@domain.tld", cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetAccessFailedCountAsync tests
        [Test]
        public async Task GetAccessFailedCountAsync_ReturnsFailedCount_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser { AccessFailedCount = 1 };

            // Act
            int result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetAccessFailedCountAsync(user);
            }

            // Assert
            Assert.That(result, Is.EqualTo(user.AccessFailedCount));
        }

        [Test]
        public void GetAccessFailedCountAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetAccessFailedCountAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void GetAccessFailedCountAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetAccessFailedCountAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetAuthenticatorKeyAsync tests
        [Test]
        public async Task GetAuthenticatorKeyAsync_ReturnsKey_WhenAKeyIsFound()
        {
            // Arrange
            var dbName = nameof(GetAuthenticatorKeyAsync_ReturnsKey_WhenAKeyIsFound);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            var userLogin = new ShopUserToken
            {
                UserId = user.Id,
                LoginProvider = InternalLoginProvider,
                Name = AuthenticatorKeyName,
                Value = "value"
            };
            context.UserTokens.Add(userLogin);
            context.SaveChanges();

            // Act
            string result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetAuthenticatorKeyAsync(user);
            }

            // Assert
            Assert.That(result, Is.EqualTo(userLogin.Value));
        }

        [Test]
        public async Task GetAuthenticatorKeyAsync_ReturnsNull_WhenAKeyIsNotFound()
        {
            // Arrange
            var dbName = nameof(GetAuthenticatorKeyAsync_ReturnsNull_WhenAKeyIsNotFound);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();

            // Act
            string result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetAuthenticatorKeyAsync(user);
            }

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetAuthenticatorKeyAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetAuthenticatorKeyAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void GetAuthenticatorKeyAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetAuthenticatorKeyAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetClaimsAsync tests
        [Test]
        public async Task GetClaimsAsync_ReturnsClaims_WhenClaimsAreFound()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser { UserName = "user@domain.tld" };
            var claims = new List<ShopUserClaim>
            {
                new ShopUserClaim { UserId = user.Id, ClaimType = "type", ClaimValue = "value" },
                new ShopUserClaim { UserId = user.Id, ClaimType = "type", ClaimValue = "value" }
            };
            user.Claims = claims;

            // Act
            IList<Claim> result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetClaimsAsync(user);
            }

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Any(c => c.Type == "type"), Is.True);
            Assert.That(result.Any(c => c.Value == "value"), Is.True);
        }

        [Test]
        public void GetClaimsAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetClaimsAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void GetClaimsAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetClaimsAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetEmailAsync tests
        [Test]
        public async Task GetEmailAsync_ReturnsEmail_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser { Email = "user@domain.tld" };

            // Act
            string result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetEmailAsync(user);
            }

            // Assert
            Assert.That(result, Is.EqualTo(user.Email));
        }

        [Test]
        public void GetEmailAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetEmailAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void GetEmailAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetEmailAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetEmailConfirmedAsync tests
        [Test]
        public async Task GetEmailConfirmedAsync_ReturnsTrue_WhenEmailIsConfirmed()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser { EmailConfirmed = true };

            // Act
            bool result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetEmailConfirmedAsync(user);
            }

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void GetEmailConfirmedAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetEmailConfirmedAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void GetEmailConfirmedAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetEmailConfirmedAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetLockoutEnabledAsync tests
        [Test]
        public async Task GetLockoutEnabledAsync_ReturnsTrue_WhenEmailIsConfirmed()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser { LockoutEnabled = true };

            // Act
            bool result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetLockoutEnabledAsync(user);
            }

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void GetLockoutEnabledAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetLockoutEnabledAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void GetLockoutEnabledAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetLockoutEnabledAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetLockoutEndDateAsync tests
        [Test]
        public async Task GetLockoutEndDateAsync_ReturnsEndDate_WhenLockoutEndIsSet()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var dateTimeOffset = new DateTimeOffset(
                    new DateTime(2018, 10, 12, 13, 15, 7),
                    new TimeSpan(1, 0, 0));
            var user = new ShopUser { LockoutEnd = dateTimeOffset };

            // Act
            DateTimeOffset? result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetLockoutEndDateAsync(user);
            }

            // Assert
            Assert.That(result, Is.EqualTo(dateTimeOffset));
        }

        [Test]
        public void GetLockoutEndDateAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetLockoutEndDateAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void GetLockoutEndDateAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetLockoutEndDateAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetLoginsAsync tests
        [Test]
        public async Task GetLoginsAsync_ReturnsLogins_WhenLoginsAreFound()
        {
            // Arrange
            var dbName = nameof(GetLoginsAsync_ReturnsLogins_WhenLoginsAreFound);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            context.UserLogins.Add(new ShopUserLogin
            {
                UserId = user.Id,
                LoginProvider = "provider",
                ProviderDisplayName = "name",
                ProviderKey = "key"
            });
            context.SaveChanges();

            // Act
            IList<UserLoginInfo> result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetLoginsAsync(user);
            }

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Any(l => l.LoginProvider == "provider"), Is.True);
            Assert.That(result.Any(l => l.ProviderDisplayName == "name"), Is.True);
            Assert.That(result.Any(l => l.ProviderKey == "key"), Is.True);
        }

        [Test]
        public void GetLoginsAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetLoginsAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void GetLoginsAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetLoginsAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
        }
        #endregion

        #region GetNormalizedEmailAsync tests
        [Test]
        public async Task GetNormalizedEmailAsync_ReturnsNormalizedEmail_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser { NormalizedEmail = _normalizer.Normalize("user@domain.tld") };

            // Act
            string result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetNormalizedEmailAsync(user);
            }

            // Assert
            Assert.That(result, Is.EqualTo(user.NormalizedEmail));
        }

        [Test]
        public void GetNormalizedEmailAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetNormalizedEmailAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void GetNormalizedEmailAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetNormalizedEmailAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetNormalizedUserNameAsync tests
        [Test]
        public async Task GetNormalizedUserNameAsync_ReturnsNormalizedUserName_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser { NormalizedUserName = _normalizer.Normalize("user@domain.tld") };

            // Act
            string result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetNormalizedUserNameAsync(user);
            }

            // Assert
            Assert.That(result, Is.EqualTo(user.NormalizedUserName));
        }

        [Test]
        public void GetNormalizedUserNameAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetNormalizedUserNameAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void GetNormalizedUserNameAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetNormalizedUserNameAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetPasswordHashAsync tests
        [Test]
        public async Task GetPasswordHashAsync_ReturnsPasswordHash_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser { PasswordHash = "passwordHash" };

            // Act
            string result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetPasswordHashAsync(user);
            }

            // Assert
            Assert.That(result, Is.EqualTo(user.PasswordHash));
        }

        [Test]
        public void GetPasswordHashAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetPasswordHashAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void GetPasswordHashAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetPasswordHashAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetPhoneNumberAsync tests
        [Test]
        public async Task GetPhoneNumberAsync_ReturnsPhoneNumber_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser { PhoneNumber = "070-123456" };

            // Act
            string result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetPhoneNumberAsync(user);
            }

            // Assert
            Assert.That(result, Is.EqualTo(user.PhoneNumber));
        }

        [Test]
        public void GetPhoneNumberAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetPhoneNumberAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void GetPhoneNumberAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetPhoneNumberAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetPhoneNumberConfirmedAsync tests
        [Test]
        public async Task GetPhoneNumberConfirmedAsync_ReturnsTrue_WhenPhoneNumberIsConfirmed()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser { PhoneNumberConfirmed = true };

            // Act
            bool result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetPhoneNumberConfirmedAsync(user);
            }

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void GetPhoneNumberConfirmedAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetPhoneNumberConfirmedAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void GetPhoneNumberConfirmedAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetPhoneNumberConfirmedAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetRolesAsync tests
        [Test]
        public async Task GetRolesAsync_ReturnsRoles_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var userRoles = new List<ShopUserRole>
            {
                new ShopUserRole { ShopUser = user, ShopRole = new ShopRole { Name = "admin" } },
                new ShopUserRole { ShopUser = user, ShopRole = new ShopRole { Name = "user" } }
            };
            user.Roles = userRoles;

            // Act
            IList<string> result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetRolesAsync(user);
            }

            // Assert
            Assert.That(result, Does.Contain("admin"));
            Assert.That(result, Does.Contain("user"));
        }

        [Test]
        public void GetRolesAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetRolesAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void GetRolesAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetRolesAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetSecurityStampAsync tests
        [Test]
        public async Task GetSecurityStampAsync_ReturnsSecurityStamp_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser { SecurityStamp = "securityStamp" };

            // Act
            string result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetSecurityStampAsync(user);
            }

            // Assert
            Assert.That(result, Is.EqualTo(user.SecurityStamp));
        }

        [Test]
        public void GetSecurityStampAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetSecurityStampAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void GetSecurityStampAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetSecurityStampAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetTokenAsync tests
        [Test]
        public async Task GetTokenAsync_ReturnsAToken_WhenCalled()
        {
            // Arrange
            var dbName = nameof(GetTokenAsync_ReturnsAToken_WhenCalled);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            context.UserTokens.Add(new ShopUserToken
            {
                UserId = user.Id,
                LoginProvider = "provider",
                Name = "name",
                Value = "value"
            });
            context.SaveChanges();

            // Act
            string result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetTokenAsync(user, "provider", "name");
            }

            // Assert
            Assert.That(result, Is.EqualTo("value"));
        }

        [Test]
        public async Task GetTokenAsync_ReturnsNull_WhenATokenIsNotFound()
        {
            // Arrange
            var dbName = nameof(GetTokenAsync_ReturnsAToken_WhenCalled);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();

            // Act
            string result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetTokenAsync(user, "provider", "name");
            }

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetTokenAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetTokenAsync(null, "provider", "name");
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void GetTokenAsync_ThrowsArgumentException_WhenLoginProviderIsNullOrWhitespace(string provider)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetTokenAsync(user, provider, "name");
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("loginProvider"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void GetTokenAsync_ThrowsArgumentException_WhenNameIsNullOrWhitespace(string name)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetTokenAsync(user, "provider", name);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void GetTokenAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetTokenAsync(user, "provider", "name", cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
        }
        #endregion

        #region GetTwoFactorEnabledAsync tests
        [Test]
        public async Task GetTwoFactorEnabledAsync_ReturnsTrue_WhenTwoFactorIsEnabled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser { TwoFactorEnabled = true };

            // Act
            bool result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetTwoFactorEnabledAsync(user);
            }

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void GetTwoFactorEnabledAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetTwoFactorEnabledAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void GetTwoFactorEnabledAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetTwoFactorEnabledAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetUserIdAsync tests
        [Test]
        public async Task GetUserIdAsync_ReturnsUserId_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser { TwoFactorEnabled = true };

            // Act
            string result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetUserIdAsync(user);
            }

            // Assert
            Assert.That(result, Is.EqualTo(user.Id));
        }

        [Test]
        public void GetUserIdAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetUserIdAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void GetUserIdAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetUserIdAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetUserNameAsync tests
        [Test]
        public async Task GetUserNameAsync_ReturnsUserName_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser { UserName = "user@domain.tld" };

            // Act
            string result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetUserNameAsync(user);
            }

            // Assert
            Assert.That(result, Is.EqualTo(user.UserName));
        }

        [Test]
        public void GetUserNameAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetUserNameAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void GetUserNameAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetUserNameAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetUsersForClaimAsync tests
        [Test]
        public async Task GetUsersForClaimAsync_ReturnsUsers_WhenCalled()
        {
            // Arrange
            var dbName = nameof(GetUsersForClaimAsync_ReturnsUsers_WhenCalled);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            var user2 = context.Users.Skip(1).First();
            context.UserClaims.AddRange(
                new ShopUserClaim
                {
                    UserId = user.Id,
                    ClaimType = "type",
                    ClaimValue = "value"
                },
                new ShopUserClaim
                {
                    UserId = user2.Id,
                    ClaimType = "type",
                    ClaimValue = "value"
                }
            );
            context.SaveChanges();

            // Act
            IList<ShopUser> result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetUsersForClaimAsync(new Claim("type", "value"));
            }

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Any(u => u.UserName == user.UserName), Is.True);
            Assert.That(result.Any(u => u.UserName == user2.UserName), Is.True);
        }

        [Test]
        public void GetUsersForClaimAsync_ThrowsArgumentNullException_WhenClaimIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetUsersForClaimAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("claim"));
        }

        [Test]
        public void GetUsersForClaimAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new Claim("type", "value");
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetUsersForClaimAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
        }
        #endregion

        #region GetUsersInRoleAsync tests
        [Test]
        public async Task GetUsersInRoleAsync_ReturnsUsers_WhenCalled()
        {
            // Arrange
            var dbName = nameof(GetUsersInRoleAsync_ReturnsUsers_WhenCalled);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            var user2 = context.Users.Skip(1).First();
            var role = context.Roles.First();
            context.UserRoles.AddRange(
                new ShopUserRole { UserId = user.Id, RoleId = role.Id },
                new ShopUserRole { UserId = user2.Id, RoleId = role.Id }
                );
            context.SaveChanges();

            // Act
            IList<ShopUser> result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.GetUsersInRoleAsync(role.Name);
            }

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Any(u => u.UserName == user.UserName), Is.True);
            Assert.That(result.Any(u => u.UserName == user2.UserName), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void GetUsersInRoleAsync_ThrowsArgumentException_WhenRoleNameIsNullOrWhitespace(string roleName)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetUsersInRoleAsync(roleName);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("roleName"));
        }

        [Test]
        public void GetUsersInRoleAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.GetUsersInRoleAsync("role", cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
        }
        #endregion

        #region HasPasswordAsync tests
        [Test]
        public async Task HasPasswordAsync_ReturnsTrue_WhenUserHasAPassword()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser { PasswordHash = "hash" };

            // Act
            bool result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.HasPasswordAsync(user);
            }

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void HasPasswordAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.HasPasswordAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void HasPasswordAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.HasPasswordAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region IncrementAccessFailedCountAsync tests
        [Test]
        public async Task IncrementAccessFailedCountAsync_ReturnsFailedCount_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser { AccessFailedCount = 1 };

            // Act
            int result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.IncrementAccessFailedCountAsync(user);
            }

            // Assert
            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        public void IncrementAccessFailedCountAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.IncrementAccessFailedCountAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void IncrementAccessFailedCountAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.IncrementAccessFailedCountAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region IsInRoleAsync tests
        [Test]
        public async Task IsInRoleAsync_ReturnsTrue_WhenUserIsInRole()
        {
            // Arrange
            var dbName = nameof(IsInRoleAsync_ReturnsTrue_WhenUserIsInRole);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            var role = context.Roles.First();
            context.UserRoles.Add(new ShopUserRole { UserId = user.Id, RoleId = role.Id });
            context.SaveChanges();

            // Act
            bool result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.IsInRoleAsync(user, role.Name);
            }

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsInRoleAsync_ReturnsFalse_WhenUserIsNotInRole()
        {
            // Arrange
            var dbName = nameof(IsInRoleAsync_ReturnsFalse_WhenUserIsNotInRole);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();

            // Act
            bool result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.IsInRoleAsync(user, "roleName");
            }

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsInRoleAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.IsInRoleAsync(null, "roleName");
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void IsInRoleAsync_ThrowsArgumentException_WhenRoleIsNullOrWhitespace(string roleName)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.IsInRoleAsync(user, roleName);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("roleName"));
        }

        [Test]
        public void IsInRoleAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.Roles = Substitute.For<DbSet<ShopRole>>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.IsInRoleAsync(user, "roleName", cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
        }
        #endregion

        #region RedeemCodeAsync tests
        [Test]
        public async Task RedeemCodeAsync_ReturnsTrue_WhenCodeIsSpent()
        {
            // Arrange
            var dbName = nameof(RedeemCodeAsync_ReturnsTrue_WhenCodeIsSpent);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            context.UserTokens.Add(new ShopUserToken
            {
                UserId = user.Id,
                LoginProvider = InternalLoginProvider,
                Name = RecoveryCodeTokenName,
                Value = "one;two;three"
            });
            context.SaveChanges();

            // Act
            bool result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.RedeemCodeAsync(user, "one");
                await repository.UpdateAsync(user);
            }

            // Assert
            Assert.That(result, Is.True);
            using (var resultContext = CreateContext<SeededUserDbContext>(dbName))
            {
                Assert.That(resultContext.UserTokens.FirstOrDefault()?.Value, Does.Not.Contain("one"));
            }
        }

        [Test]
        public async Task RedeemCodeAsync_ReturnsFalse_WhenCodeDoesNotExist()
        {
            // Arrange
            var dbName = nameof(RedeemCodeAsync_ReturnsFalse_WhenCodeDoesNotExist);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            context.UserTokens.Add(new ShopUserToken
            {
                UserId = user.Id,
                LoginProvider = InternalLoginProvider,
                Name = RecoveryCodeTokenName,
                Value = "one;two;three"
            });
            context.SaveChanges();

            // Act
            bool result;
            using (var repository = new UserRepository(context))
            {
                result = await repository.RedeemCodeAsync(user, "four");
            }

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void RedeemCodeAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.RedeemCodeAsync(null, "code");
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void RedeemCodeAsync_ThrowsArgumentException_WhenCodeIsNullOrWhitespace(string code)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.RedeemCodeAsync(user, code);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("code"));
        }

        [Test]
        public void RedeemCodeAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.Roles = Substitute.For<DbSet<ShopRole>>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.RedeemCodeAsync(user, "code", cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
        }
        #endregion

        #region RemoveClaimsAsync tests
        [Test]
        public async Task RemoveClaimsAsync_RemovesClaims_WhenCalled()
        {
            // Arrange
            var dbName = nameof(RedeemCodeAsync_ReturnsTrue_WhenCodeIsSpent);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            var claims = new List<ShopUserClaim>
            {
                new ShopUserClaim { UserId = user.Id, ClaimType = "type", ClaimValue = "value" },
                new ShopUserClaim { UserId = user.Id, ClaimType = "type", ClaimValue = "value2" }
            };
            context.UserClaims.AddRange(claims);
            context.SaveChanges();

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.RemoveClaimsAsync(user, new[] { new Claim("type", "value") });
                await repository.UpdateAsync(user);
            }

            // Assert
            using (var resultContext = CreateContext<SeededUserDbContext>(dbName))
            {
                Assert.That(resultContext.UserClaims.Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public void RemoveClaimsAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var claims = new List<Claim>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.RemoveClaimsAsync(null, claims);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void RemoveClaimsAsync_ThrowsArgumentNullException_WhenClaimsIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.RemoveClaimsAsync(user, null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("claims"));
        }

        [Test]
        public void RemoveClaimsAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.Roles = Substitute.For<DbSet<ShopRole>>();
            var user = new ShopUser();
            var claims = new List<Claim>();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.RemoveClaimsAsync(user, claims, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
        }
        #endregion

        #region RemoveFromRoleAsync tests
        [Test]
        public async Task RemoveFromRoleAsync_RemovesUserFromARole_WhenCalled()
        {
            // Arrange
            var dbName = nameof(RemoveFromRoleAsync_RemovesUserFromARole_WhenCalled);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            var role = context.Roles.First();
            context.UserRoles.Add(new ShopUserRole { UserId = user.Id, RoleId = role.Id });
            context.SaveChanges();

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.RemoveFromRoleAsync(user, role.Name);
                await repository.UpdateAsync(user);
            }

            // Assert
            using (var resultContext = CreateContext<SeededUserDbContext>(dbName))
            {
                var resultUserRole = resultContext.UserRoles.Find(new[] { user.Id, role.Id });
                Assert.That(resultUserRole, Is.Null);
            }
        }

        [Test]
        public void RemoveFromRoleAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.RemoveFromRoleAsync(null, "roleName");
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void RemoveFromRoleAsync_ThrowsArgumentException_WhenRoleNameIsNullOrWhitespace(string roleName)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.RemoveFromRoleAsync(user, roleName);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("roleName"));
        }

        [Test]
        public void RemoveFromRoleAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.Roles = Substitute.For<DbSet<ShopRole>>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.RemoveFromRoleAsync(user, "roleName", cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
            Assert.That(user.Roles.Count, Is.Zero);
        }
        #endregion

        #region RemoveLoginAsync tests
        [Test]
        public async Task RemoveLoginAsync_RemovesLogin_WhenCalled()
        {
            // Arrange
            var dbName = nameof(RemoveLoginAsync_RemovesLogin_WhenCalled);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            var loginInfo = new ShopUserLogin
            {
                UserId = user.Id,
                LoginProvider = "provider",
                ProviderDisplayName = "name",
                ProviderKey = "key"
            };
            context.UserLogins.Add(loginInfo);
            context.SaveChanges();

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.RemoveLoginAsync(user, "provider", "key");
                await repository.UpdateAsync(user);
            }

            // Assert
            using (var resultContext = CreateContext<SeededUserDbContext>(dbName))
            {
                var result = resultContext.UserLogins.Find(new[] { loginInfo.LoginProvider, loginInfo.ProviderKey });
                Assert.That(result, Is.Null);
            }
        }

        [Test]
        public void RemoveLoginAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.RemoveLoginAsync(null, "provider", "key");
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void RemoveLoginAsync_ThrowsArgumentException_WhenLoginProviderIsNullOrWhitespace(string provider)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.RemoveLoginAsync(user, provider, "key");
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("loginProvider"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void RemoveLoginAsync_ThrowsArgumentException_WhenProviderKeyIsNullOrWhitespace(string key)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.RemoveLoginAsync(user, "provider", key);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("providerKey"));
        }

        [Test]
        public void RemoveLoginAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.UserLogins = Substitute.For<DbSet<ShopUserLogin>>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.RemoveLoginAsync(user, "provider", "key", cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
            context.UserLogins.DidNotReceiveWithAnyArgs().Remove(Arg.Any<ShopUserLogin>());
        }
        #endregion

        #region RemoveTokenAsync tests
        [Test]
        public async Task RemoveTokenAsync_RemovesAToken_WhenCalled()
        {
            // Arrange
            var dbName = nameof(RemoveTokenAsync_RemovesAToken_WhenCalled);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            context.UserTokens.Add(new ShopUserToken
            {
                UserId = user.Id,
                LoginProvider = "provider",
                Name = "name",
                Value = "value"
            });
            context.SaveChanges();

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.RemoveTokenAsync(user, "provider", "name");
                await repository.UpdateAsync(user);
            }

            // Assert
            using (var resultContext = CreateContext<SeededUserDbContext>(dbName))
            {
                var result = resultContext.UserTokens.Find(new[] { "provider", "name", user.Id });
                Assert.That(result, Is.Null);
            }
        }

        [Test]
        public void RemoveTokenAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.RemoveTokenAsync(null, "provider", "name");
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void RemoveTokenAsync_ThrowsArgumentException_WhenLoginProviderIsNullOrWhitespace(string provider)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.RemoveTokenAsync(user, provider, "name");
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("loginProvider"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void RemoveTokenAsync_ThrowsArgumentException_WhenNameIsNullOrWhitespce(string name)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.RemoveTokenAsync(user, "provider", name);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void RemoveTokenAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.UserTokens = Substitute.For<DbSet<ShopUserToken>>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.RemoveTokenAsync(user, "provider", "name", cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
            context.UserTokens.DidNotReceiveWithAnyArgs().Remove(Arg.Any<ShopUserToken>());
        }
        #endregion

        #region ReplaceClaimAsync tests
        [Test]
        public async Task ReplaceClaimAsync_ReplacesAClaim_WhenCalled()
        {
            // Arrange
            var dbName = nameof(ReplaceClaimAsync_ReplacesAClaim_WhenCalled);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            var claim = new Claim("type", "value");
            var newClaim = new Claim("type", "newValue");
            context.UserClaims.Add(new ShopUserClaim
            {
                UserId = user.Id,
                ClaimType = claim.Type,
                ClaimValue = claim.Value
            });
            context.SaveChanges();

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.ReplaceClaimAsync(user, claim, newClaim);
                await repository.UpdateAsync(user);
            }

            // Assert
            using (var resultContext = CreateContext<SeededUserDbContext>(dbName))
            {
                var resultOldClaim = resultContext.UserClaims
                    .Where(uc =>
                        uc.UserId == user.Id &&
                        uc.ClaimType == claim.Type &&
                        uc.ClaimValue == claim.Value)
                    .ToList();
                var resultNewClaim = resultContext.UserClaims
                    .Where(uc =>
                        uc.UserId == user.Id &&
                        uc.ClaimType == newClaim.Type &&
                        uc.ClaimValue == newClaim.Value)
                    .ToList();
                Assert.That(resultOldClaim?.Count, Is.Zero);
                Assert.That(resultNewClaim?.FirstOrDefault()?.ClaimValue, Is.EqualTo("newValue"));
            }
        }

        [Test]
        public void ReplaceClaimAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var claim = new Claim("type", "value");

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.ReplaceClaimAsync(null, claim, claim);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void ReplaceClaimAsync_ThrowsArgumentNullException_WhenClaimIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var claim = new Claim("type", "value");

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.ReplaceClaimAsync(user, null, claim);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("claim"));
        }

        [Test]
        public void ReplaceClaimAsync_ThrowsArgumentNullException_WhenNewClaimIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var claim = new Claim("type", "value");

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.ReplaceClaimAsync(user, claim, null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("newClaim"));
        }

        [Test]
        public void ReplaceClaimAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.UserClaims = Substitute.For<DbSet<ShopUserClaim>>();
            var user = new ShopUser();
            var claim = new Claim("type", "value");
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.ReplaceClaimAsync(user, claim, claim, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
        }
        #endregion

        #region ReplaceCodesAsync tests
        [Test]
        public async Task ReplaceCodesAsync_ReplacesCodes_WhenCalled()
        {
            // Arrange
            var dbName = nameof(ReplaceCodesAsync_ReplacesCodes_WhenCalled);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            context.UserTokens.Add(new ShopUserToken
            {
                UserId = user.Id,
                LoginProvider = InternalLoginProvider,
                Name = RecoveryCodeTokenName,
                Value = "one;two;three"
            });
            context.SaveChanges();
            var newCodes = new string[] { "four", "five", "six" };

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.ReplaceCodesAsync(user, newCodes);
                await repository.UpdateAsync(user);
            }

            // Assert
            using (var resultContext = CreateContext<SeededUserDbContext>(dbName))
            {
                var result = resultContext.UserTokens.Find(new[] { InternalLoginProvider, RecoveryCodeTokenName, user.Id });
                Assert.That(result?.Value, Is.EqualTo(string.Join(";", newCodes)));
            }
        }
        #endregion

        #region ResetAccessFailedCountAsync tests
        [Test]
        public async Task ResetAccessFailedCountAsync_ResetsFailedCount_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser { AccessFailedCount = 1 };

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.ResetAccessFailedCountAsync(user);
            }

            // Assert
            Assert.That(user.AccessFailedCount, Is.Zero);
        }

        [Test]
        public void ResetAccessFailedCountAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.ResetAccessFailedCountAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void ResetAccessFailedCountAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.ResetAccessFailedCountAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region SetAuthenticatorKeyAsync tests
        [Test]
        public async Task SetAuthenticatorKeyAsync_SetsKey_WhenCalled()
        {
            // Arrange
            var dbName = nameof(SetAuthenticatorKeyAsync_SetsKey_WhenCalled);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.SetAuthenticatorKeyAsync(user, "key");
                await repository.UpdateAsync(user);
            }

            // Assert
            using (var resultContext = CreateContext<SeededUserDbContext>(dbName))
            {
                var result = resultContext.UserTokens.Find(new[] { InternalLoginProvider, AuthenticatorKeyName, user.Id });
                Assert.That(result?.Value, Is.EqualTo("key"));
            }
        }
        #endregion

        #region SetEmailAsync tests
        [Test]
        public async Task SetEmailAsync_SetsEmail_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var email = "user@domain.tld";

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.SetEmailAsync(user, email);
            }

            // Assert
            Assert.That(user.Email, Is.EqualTo(email));
        }

        [Test]
        public void SetEmailAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var email = "user@domain.tld";

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetEmailAsync(null, email);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SetEmailAsync_ThrowsArgumentException_WhenEmailIsNullOrWhitespace(string email)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetEmailAsync(user, email);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("email"));
        }

        [Test]
        public void SetEmailAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var email = "user@domain.tld";
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetEmailAsync(user, email, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region SetEmailConfirmedAsync tests
        [Test]
        public async Task SetEmailConfirmedAsync_SetsEmailConfirmed_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.SetEmailConfirmedAsync(user, true);
            }

            // Assert
            Assert.That(user.EmailConfirmed, Is.True);
        }

        [Test]
        public void SetEmailConfirmedAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetEmailConfirmedAsync(null, true);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void SetEmailConfirmedAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetEmailConfirmedAsync(user, true, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region SetLockoutEnabledAsync tests
        [Test]
        public async Task SetLockoutEnabledAsync_SetsEmail_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.SetLockoutEnabledAsync(user, true);
            }

            // Assert
            Assert.That(user.LockoutEnabled, Is.True);
        }

        [Test]
        public void SetLockoutEnabledAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetLockoutEnabledAsync(null, true);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void SetLockoutEnabledAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetLockoutEnabledAsync(user, true, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region SetLockoutEndDateAsync tests
        [Test]
        public async Task SetLockoutEndDateAsync_SetsEmail_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var end = new DateTimeOffset(
                new DateTime(2018, 10, 13, 19, 19, 31),
                new TimeSpan(1, 0, 0));

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.SetLockoutEndDateAsync(user, end);
            }

            // Assert
            Assert.That(user.LockoutEnd, Is.EqualTo(end));
        }

        [Test]
        public void SetLockoutEndDateAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var end = new DateTimeOffset();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetLockoutEndDateAsync(null, end);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void SetLockoutEndDateAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var end = new DateTimeOffset();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetLockoutEndDateAsync(user, end, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region SetNormalizedEmailAsync tests
        [Test]
        public async Task SetNormalizedEmailAsync_SetsNormalizedEmail_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var normalizedEmail = _normalizer.Normalize("user@domain.tld");

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.SetNormalizedEmailAsync(user, normalizedEmail);
            }

            // Assert
            Assert.That(user.NormalizedEmail, Is.EqualTo(normalizedEmail));
        }

        [Test]
        public void SetNormalizedEmailAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var normalizedEmail = _normalizer.Normalize("user@domain.tld");

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetNormalizedEmailAsync(null, normalizedEmail);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SetNormalizedEmailAsync_ThrowsArgumentException_WhenNormalizedEmailIsNullOrWhitespace(string normalizedEmail)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetNormalizedEmailAsync(user, normalizedEmail);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("normalizedEmail"));
        }

        [Test]
        public void SetNormalizedEmailAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var normalizedEmail = _normalizer.Normalize("user@domain.tld");
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetNormalizedEmailAsync(user, normalizedEmail, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region SetNormalizedUserNameAsync tests
        [Test]
        public async Task SetNormalizedUserNameAsync_SetsNormalizedUserName_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var normalizedUserName = _normalizer.Normalize("user@domain.tld");

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.SetNormalizedUserNameAsync(user, normalizedUserName);
            }

            // Assert
            Assert.That(user.NormalizedUserName, Is.EqualTo(normalizedUserName));
        }

        [Test]
        public void SetNormalizedUserNameAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var normalizedUserName = _normalizer.Normalize("user@domain.tld");

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetNormalizedUserNameAsync(null, normalizedUserName);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SetNormalizedUserNameAsync_ThrowsArgumentException_WhenNormalizedUserNameIsNullOrWhitespace(string normalizedName)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetNormalizedUserNameAsync(user, normalizedName);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("normalizedName"));
        }

        [Test]
        public void SetNormalizedUserNameAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var normalizedUserName = _normalizer.Normalize("user@domain.tld");
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetNormalizedUserNameAsync(user, normalizedUserName, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region SetPasswordHashAsync tests
        [Test]
        public async Task SetPasswordHashAsync_SetsPasswordHash_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var hash = "hash";

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.SetPasswordHashAsync(user, hash);
            }

            // Assert
            Assert.That(user.PasswordHash, Is.EqualTo(hash));
        }

        [Test]
        public void SetPasswordHashAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var hash = "hash";

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetPasswordHashAsync(null, hash);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SetPasswordHashAsync_ThrowsArgumentException_WhenPasswordHashIsNullOrWhitespace(string hash)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetPasswordHashAsync(user, hash);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("passwordHash"));
        }

        [Test]
        public void SetPasswordHashAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var hash = "hash";
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetNormalizedUserNameAsync(user, hash, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region SetPhoneNumberAsync tests
        [Test]
        public async Task SetPhoneNumberAsync_SetsPhoneNumber_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var phoneNumber = "070-123456";

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.SetPhoneNumberAsync(user, phoneNumber);
            }

            // Assert
            Assert.That(user.PhoneNumber, Is.EqualTo(phoneNumber));
        }

        [Test]
        public void SetPhoneNumberAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var phoneNumber = "070-123456";

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetPhoneNumberAsync(null, phoneNumber);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SetPhoneNumberAsync_ThrowsArgumentException_WhenPhoneNumberIsNullOrWhitespace(string phoneNumber)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetPhoneNumberAsync(user, phoneNumber);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("phoneNumber"));
        }

        [Test]
        public void SetPhoneNumberAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var phoneNumber = "070-123456";
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetPhoneNumberAsync(user, phoneNumber, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region SetPhoneNumberConfirmedAsync tests
        [Test]
        public async Task SetPhoneNumberConfirmedAsync_SetsPhoneNumberConfirmed_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.SetPhoneNumberConfirmedAsync(user, true);
            }

            // Assert
            Assert.That(user.PhoneNumberConfirmed, Is.True);
        }

        [Test]
        public void SetPhoneNumberConfirmedAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetPhoneNumberConfirmedAsync(null, true);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void SetPhoneNumberConfirmedAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetPhoneNumberConfirmedAsync(user, true, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region SetSecurityStampAsync tests
        [Test]
        public async Task SetSecurityStampAsync_SetsPhoneNumber_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var stamp = "stamp";

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.SetSecurityStampAsync(user, stamp);
            }

            // Assert
            Assert.That(user.SecurityStamp, Is.EqualTo(stamp));
        }

        [Test]
        public void SetSecurityStampAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var stamp = "stamp";

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetPhoneNumberAsync(null, stamp);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SetSecurityStampAsync_ThrowsArgumentException_WhenSecurityStampIsNullOrWhitespace(string stamp)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetSecurityStampAsync(user, stamp);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("stamp"));
        }

        [Test]
        public void SetSecurityStampAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var stamp = "stamp";
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetSecurityStampAsync(user, stamp, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region SetTokenAsync tests
        [Test]
        public async Task SetTokenAsync_SetsToken_WhenCalled()
        {
            // Arrange
            var dbName = nameof(SetTokenAsync_SetsToken_WhenCalled);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            var provider = "provider";
            var name = "name";
            var value = "value";

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.SetTokenAsync(user, provider, name, value);
                await repository.UpdateAsync(user);
            }

            // Assert
            using (var resultContext = CreateContext<SeededUserDbContext>(dbName))
            {
                var result = resultContext.UserTokens.Find(new[] { provider, name, user.Id });
                Assert.That(result.Value, Is.EqualTo(value));
            }
        }

        [Test]
        public void SetTokenAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var provider = "provider";
            var name = "name";
            var value = "value";

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetTokenAsync(null, provider, name, value);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SetTokenAsync_ThrowsArgumentException_WhenLoginProviderIsNullOrWhitespace(string provider)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var name = "name";
            var value = "value";

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetTokenAsync(user, provider, name, value);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("loginProvider"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SetTokenAsync_ThrowsArgumentException_WhenNameIsNullOrWhitespace(string name)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var provider = "provider";
            var value = "value";

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetTokenAsync(user, provider, name, value);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void SetTokenAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var provider = "provider";
            var name = "name";
            var value = "value";
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetTokenAsync(user, provider, name, value, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
        }
        #endregion

        #region SetTwoFactorEnabledAsync tests
        [Test]
        public async Task SetTwoFactorEnabledAsync_SetsTwoFactorEnabled_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.SetTwoFactorEnabledAsync(user, true);
            }

            // Assert
            Assert.That(user.TwoFactorEnabled, Is.True);
        }

        [Test]
        public void SetTwoFactorEnabledAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetTwoFactorEnabledAsync(null, true);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void SetTwoFactorEnabledAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetTwoFactorEnabledAsync(user, true, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region SetUserNameAsync tests
        [Test]
        public async Task SetUserNameAsync_SetsUserName_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var userName = "user@domain.tld";

            // Act
            using (var repository = new UserRepository(context))
            {
                await repository.SetUserNameAsync(user, userName);
            }

            // Assert
            Assert.That(user.UserName, Is.EqualTo(userName));
        }

        [Test]
        public void SetUserNameAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var userName = "user@domain.tld";

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetUserNameAsync(null, userName);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SetUserNameAsync_ThrowsArgumentException_WhenNormalizedUserNameIsNullOrWhitespace(string name)
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetUserNameAsync(user, name);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("userName"));
        }

        [Test]
        public void SetUserNameAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var userName = "user@domain.tld";
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.SetNormalizedUserNameAsync(user, userName, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region UpdateAsync tests
        [Test]
        public async Task UpdateAsync_ComittsModifiedEntities_WhenCalled()
        {
            // Arrange
            var dbName = nameof(AddClaimsAsync_AddsClaims_WhenCalled);
            var context = CreateContext<SeededUserDbContext>(dbName, true);
            var user = context.Users.First();
            var userName = "changed@domain.tld";

            // Act
            IdentityResult identityResult;
            using (var repository = new UserRepository(context))
            {
                await repository.SetUserNameAsync(user, userName);
                identityResult = await repository.UpdateAsync(user);
            }

            // Assert
            Assert.That(identityResult, Is.EqualTo(IdentityResult.Success));
            using (var resultContext = CreateContext<SeededUserDbContext>(dbName))
            {
                var result = resultContext.Users.Find(user.Id);
                Assert.That(result.UserName, Is.EqualTo(userName));
            }
        }

        [Test]
        public async Task UpdateAsync_ReturnsIdentityResultFailed_WhenSaveChangesThrowsDbUpdateConcurrencyException()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.Users = Substitute.For<DbSet<ShopUser>>();
            var entityEntries = new List<IUpdateEntry> { Substitute.For<IUpdateEntry>() };
            var exception = new DbUpdateConcurrencyException("Update concurrency exception", entityEntries);
            context.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromException<int>(exception));

            // Act
            IdentityResult identityResult;
            using (var repository = new UserRepository(context))
            {
                identityResult = await repository.UpdateAsync(new ShopUser());
            }

            // Assert
            Assert.That(identityResult, Is.Not.EqualTo(IdentityResult.Success));
        }

        [Test]
        public void UpdateAsync_ThrowsArgumentNullException_WhenUserIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.UpdateAsync(null);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Is.EqualTo("user"));
        }

        [Test]
        public void UpdateAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var user = new ShopUser();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new UserRepository(context))
                {
                    return repository.UpdateAsync(user, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
            context.DidNotReceiveWithAnyArgs().SaveChangesAsync(Arg.Any<CancellationToken>());
        }
        #endregion
    }
}
