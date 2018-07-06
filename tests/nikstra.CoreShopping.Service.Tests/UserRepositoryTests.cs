using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using nikstra.CoreShopping.Service.Data;
using nikstra.CoreShopping.Service.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

// https://docs.microsoft.com/en-us/ef/core/miscellaneous/testing/in-memory

namespace nikstra.CoreShopping.Service.Tests
{
    [TestFixture]
    public class UserRepositoryTests
    {
        [Test]
        public async Task CreateUserOnContext()
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: "Testing_in_memory_database")
                .Options;

            using (var context = new UserDbContext(options))
            {
                context.Users.Add(new Models.ShopUser
                {
                    UserName = "niklas@domain.tld"
                });
                context.SaveChanges();
            }

            using (var context = new UserDbContext(options))
            {
                var count = await context.Users.CountAsync();
                Assert.That(count, Is.EqualTo(1));
                var user = await context.Users.SingleAsync();
                Assert.That(user.UserName, Is.EqualTo("niklas@domain.tld"));
            }
        }

        [Test]
        public async Task CreateUserOnRepository()
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: "Testing_repo_in_memory_database")
                .Options;

            using (var repo = new UserRepository(new UserDbContext(options)))
            {
                await repo.CreateAsync(new Models.ShopUser
                {
                    UserName = "niklas@domain.tld",
                    NormalizedUserName = "NIKLAS@DOMAIN.TLD"
                }, CancellationToken.None);
            }

            using (var repo = new UserRepository(new UserDbContext(options)))
            {
                var user = await repo.FindByNameAsync("NIKLAS@DOMAIN.TLD", CancellationToken.None);
                Assert.That(user, Is.Not.Null);
                Assert.That(user.UserName, Is.EqualTo("niklas@domain.tld"));
            }
        }

        [Test]
        public async Task AddLoginOnRepository()
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: nameof(AddLoginOnRepository))
                .Options;

            var user = new Models.ShopUser
            {
                UserName = "niklas@domain.tld",
                NormalizedUserName = "NIKLAS@DOMAIN.TLD"
            };

            using (var repo = new UserRepository(new UserDbContext(options)))
            {
                await repo.CreateAsync(user, CancellationToken.None);

                await repo.AddLoginAsync(user, new UserLoginInfo("LoginProvider", "ProviderKey", "DisplayName"), CancellationToken.None);
                await repo.UpdateAsync(user);
            }

            using (var repo = new UserRepository(new UserDbContext(options)))
            {
                var logins = await repo.GetLoginsAsync(user, CancellationToken.None);
                Assert.That(logins.Count, Is.GreaterThan(0));
                Assert.That(logins[0].LoginProvider, Is.EqualTo("LoginProvider"));
            }
        }

        [Test]
        public async Task AddRoleOnRepository()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: nameof(AddRoleOnRepository))
                .Options;

            var user = new Models.ShopUser
            {
                UserName = "niklas@domain.tld",
                NormalizedUserName = "NIKLAS@DOMAIN.TLD"
            };

            using (var context = new UserDbContext(options))
            {
                context.Roles.Add(new Models.ShopRole
                {
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                });

                await context.SaveChangesAsync();
            }

            // Act
            using (var repo = new UserRepository(new UserDbContext(options)))
            {
                await repo.CreateAsync(user, CancellationToken.None);
                var newUser = await repo.FindByNameAsync("NIKLAS@DOMAIN.TLD", CancellationToken.None);
                await repo.AddToRoleAsync(newUser, "Admin", CancellationToken.None);
            }

            // Assert
            using (var repo = new UserRepository(new UserDbContext(options)))
            {
                var roles = await repo.GetRolesAsync(user, CancellationToken.None);
                Assert.That(roles.Count, Is.GreaterThan(0));
                Assert.That(roles[0], Is.EqualTo("Admin"));
            }
        }

        [Test]
        public async Task GetUsersForClaimAsync_Test()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: nameof(GetUsersForClaimAsync_Test))
                .Options;

            var user1 = new ShopUser("niklas@domain.tld");
            user1.NormalizedUserName = user1.UserName.Normalize().ToUpperInvariant();

            var user2 = new ShopUser("user@domain.tld");
            user2.NormalizedUserName = user2.UserName.Normalize().ToUpperInvariant();

            using (var repo = new UserRepository(new UserDbContext(options)))
            {
                await repo.CreateAsync(user1, CancellationToken.None);
                await repo.CreateAsync(user2, CancellationToken.None);

                await repo.AddClaimsAsync(user1, new[] { new Claim("type1", "value1") });
                await repo.AddClaimsAsync(user2, new[] { new Claim("type1", "value1") });

                await repo.UpdateAsync(user1);
                await repo.UpdateAsync(user2);
            }

            // Act
            IList<ShopUser> result;
            using (var repo = new UserRepository(new UserDbContext(options)))
            {
                result = await repo.GetUsersForClaimAsync(new Claim("type1", "value1"));
            }

            // Assert
            using (var repo = new UserRepository(new UserDbContext(options)))
            {
                Assert.That(result.Count, Is.EqualTo(2));
            }
        }
    }
}
