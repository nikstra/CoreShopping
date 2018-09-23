using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Update;
using nikstra.CoreShopping.Service.Data;
using nikstra.CoreShopping.Service.Models;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace nikstra.CoreShopping.Service.Tests
{
    [TestFixture, SetCulture("en-US")]
    public class RoleRepositoryTests
    {
        ILookupNormalizer _normalizer = new UpperInvariantLookupNormalizer();

        #region CreateAsync tests
        [Test]
        public async Task CreateAsync_CreatesANewRole_WhenCalled()
        {
            // Arrange
            var dbName = nameof(CreateAsync_CreatesANewRole_WhenCalled);
            var context = GetInMemoryContext(dbName);
            using (var repository = new RoleRepository(context))
            {
                var newRole = new ShopRole { Name = "RoleName" };

                // Act
                var result = await repository.CreateAsync(newRole);

                // Assert
                Assert.That(result, Is.EqualTo(IdentityResult.Success));
                Assert.That((await context.Roles.SingleAsync()).Name, Is.EqualTo(newRole.Name));
            }
        }

        [Test]
        public void CreateAsync_ThrowsArgumentNullException_WhenRoleIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            ShopRole newRole = null;

            // Act
            async Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    await repository.CreateAsync(newRole);
                }
            }

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(Act);
        }

        [Test]
        public void CreateAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.Roles = Substitute.For<DbSet<ShopRole>>();
            context.Roles.Add(Arg.Any<ShopRole>());
            context.SaveChangesAsync(Arg.Any<CancellationToken>());
            var role = new ShopRole();
            var cancellationToken = new CancellationToken(true);
            IdentityResult result = null;

            // Act
            async Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    result = await repository.CreateAsync(role, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
            context.Roles.DidNotReceiveWithAnyArgs().Add(Arg.Any<ShopRole>());
            context.DidNotReceiveWithAnyArgs().SaveChangesAsync(Arg.Any<CancellationToken>());
            Assert.That(result, Is.Null);
        }
        #endregion

        #region DeleteAsync tests
        [Test]
        public async Task DeleteAsync_DeletesARole_WhenCalled()
        {
            // Arrange
            var dbName = nameof(DeleteAsync_DeletesARole_WhenCalled);
            var context = GetInMemoryContextWithRoles(dbName);
            var role = await context.Roles.FirstAsync();
            using (var repository = new RoleRepository(context))
            {
                // Act
                var result = await repository.DeleteAsync(role);

                // Assert
                Assert.That(result, Is.EqualTo(IdentityResult.Success));
                Assert.That(
                    await context.Roles.FirstOrDefaultAsync(r => r.Name == role.Name),
                    Is.Null);
            }
        }

        [Test]
        public async Task DeleteAsync_ReturnsIdentityResultFailed_WhenSaveChangesThrowsDbUpdateConcurrencyException()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.Roles = Substitute.For<DbSet<ShopRole>>();
            context.Roles.Remove(Arg.Any<ShopRole>())
                .Returns(null as EntityEntry<ShopRole>);
            var entityEntries = new List<IUpdateEntry> { Substitute.For<IUpdateEntry>() };
            var exception = new DbUpdateConcurrencyException("Update concurrency exception", entityEntries);
            context.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromException<int>(exception));
            IdentityResult result;

            // Act
            using (var repository = new RoleRepository(context))
            {
                result = await repository.DeleteAsync(new ShopRole());
            }

            // Assert
            Assert.That(result, Is.Not.EqualTo(IdentityResult.Success));
        }

        [Test]
        public void DeleteAsync_ThrowsArgumentNullException_WhenRoleIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            ShopRole newRole = null;

            // Act
            async Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    await repository.DeleteAsync(newRole);
                }
            }

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(Act);
        }

        [Test]
        public void DeleteAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.Roles = Substitute.For<DbSet<ShopRole>>();
            context.Roles.Remove(Arg.Any<ShopRole>());
            context.SaveChangesAsync(Arg.Any<CancellationToken>());
            var role = new ShopRole();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.DeleteAsync(role, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
            context.Roles.DidNotReceiveWithAnyArgs().Remove(Arg.Any<ShopRole>());
            context.DidNotReceiveWithAnyArgs().SaveChangesAsync(Arg.Any<CancellationToken>());
        }
        #endregion

        #region FindByIdAsync tests
        [Test]
        public async Task FindByIdAsync_FindsARole_WhenCalledWithAnExistingRoleId()
        {
            // Arrange
            var dbName = nameof(FindByIdAsync_FindsARole_WhenCalledWithAnExistingRoleId);
            var context = GetInMemoryContext(dbName);
            var newRole = new ShopRole
            {
                Name = "RoleName"
            };
            context.Roles.Add(newRole);
            context.SaveChanges();
            ShopRole result;

            // Act
            using (var repository = new RoleRepository(context))
            {
                result = await repository.FindByIdAsync(newRole.Id);
            }

            // Assert
            Assert.That(result?.Name, Is.EqualTo(newRole.Name));
        }

        [Test]
        public void FindByIdAsync_ThrowsArgumentException_WhenRoleIdIsNullOrEmpty()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            string roleId = null;

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.FindByIdAsync(roleId);
                }
            }

            // Assert
            Assert.ThrowsAsync<ArgumentException>(Act);
        }

        [Test]
        public void FindByIdAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var roleId = "roleId";
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.FindByIdAsync(roleId, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region FindByNameAsync tests
        [Test]
        public async Task FindByNameAsync_FindsARole_WhenCalledWithAnExistingRoleName()
        {
            // Arrange
            var dbName = nameof(FindByNameAsync_FindsARole_WhenCalledWithAnExistingRoleName);
            var context = GetInMemoryContext(dbName);
            var newRole = new ShopRole
            {
                Name = "RoleName",
                NormalizedName = _normalizer.Normalize("RoleName")
            };
            context.Roles.Add(newRole);
            context.SaveChanges();
            ShopRole result;

            // Act
            using (var repository = new RoleRepository(context))
            {
                result = await repository.FindByNameAsync(newRole.NormalizedName);
            }

            // Assert
            Assert.That(result?.Name, Is.EqualTo(newRole.Name));
        }

        [Test]
        public void FindByNameAsync_ThrowsArgumentException_WhenRoleNameIsNullOrEmpty()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            string roleName = null;

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.FindByNameAsync(roleName);
                }
            }

            // Assert
            Assert.ThrowsAsync<ArgumentException>(Act);
        }

        [Test]
        public void FindByNameAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var roleName = "roleName";
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.FindByNameAsync(roleName, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetNormalizedRoleNameAsync tests
        [Test]
        public async Task GetNormalizedRoleNameAsync_ReturnsNormalizedRoleName_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var role = new ShopRole
            {
                Name = "RoleName",
                NormalizedName = _normalizer.Normalize("RoleName")
            };
            string result = string.Empty;

            // Act
            using (var repository = new RoleRepository(context))
            {
                result = await repository.GetNormalizedRoleNameAsync(role);
            }

            // Assert
            Assert.That(result, Is.EqualTo(role.NormalizedName));
        }

        [Test]
        public void GetNormalizedRoleNameAsync_ThrowsArgumentNullException_WhenRoleIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            ShopRole role = null;

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.GetNormalizedRoleNameAsync(role);
                }
            }

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(Act);
        }

        [Test]
        public void GetNormalizedRoleNameAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var role = new ShopRole();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.GetNormalizedRoleNameAsync(role, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetRoleIdAsync tests
        [Test]
        public async Task GetRoleIdAsync_ReturnsRoleId_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var role = new ShopRole();
            string result = string.Empty;

            // Act
            using (var repository = new RoleRepository(context))
            {
                result = await repository.GetRoleIdAsync(role);
            }

            // Assert
            Assert.That(result, Is.EqualTo(role.Id));
        }

        [Test]
        public void GetRoleIdAsync_ThrowsArgumentNullException_WhenRoleIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            ShopRole role = null;

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.GetRoleIdAsync(role);
                }
            }

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(Act);
        }

        [Test]
        public void GetRoleIdAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var role = new ShopRole();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.GetRoleIdAsync(role, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region GetRoleNameAsync testsa
        [Test]
        public async Task GetRoleNameAsync_ReturnsRoleName_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var role = new ShopRole
            {
                Name = "RoleName"
            };
            string result = string.Empty;

            // Act
            using (var repository = new RoleRepository(context))
            {
                result = await repository.GetRoleNameAsync(role);
            }

            // Assert
            Assert.That(result, Is.EqualTo(role.Name));
        }

        [Test]
        public void GetRoleNameAsync_ThrowsArgumentNullException_WhenRoleIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            ShopRole role = null;

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.GetRoleNameAsync(role);
                }
            }

            // Assert
            Assert.ThrowsAsync<ArgumentNullException>(Act);
        }

        [Test]
        public void GetRoleNameAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var role = new ShopRole();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.GetRoleNameAsync(role, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
        }
        #endregion

        #region SetNormalizedRoleNameAsync tests
        [Test]
        public async Task SetNormalizedRoleNameAsync_SetsNormalizedRoleName_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var normalizedRoleName = _normalizer.Normalize("RoleName");
            var role = new ShopRole { NormalizedName = null };

            // Act
            using (var repository = new RoleRepository(context))
            {
                await repository.SetNormalizedRoleNameAsync(role, normalizedRoleName);
            }

            // Assert
            Assert.That(role.NormalizedName, Is.EqualTo(normalizedRoleName));
        }

        [Test]
        public void SetNormalizedRoleNameAsync_ThrowsArgumentNullException_WhenRoleIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            ShopRole role = null;
            var normalizedRoleName = "ROLENAME";

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.SetNormalizedRoleNameAsync(role, normalizedRoleName);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Does.StartWith("role"));
        }

        [Test]
        public void SetNormalizedRoleNameAsync_ThrowsArgumentException_WhenNormalizedRoleNameIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            ShopRole role = new ShopRole();
            string normalizedRoleName = null;

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.SetNormalizedRoleNameAsync(role, normalizedRoleName);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.Message, Does.StartWith("normalizedName"));
        }

        [Test]
        public void SetNormalizedRoleNameAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var role = new ShopRole { NormalizedName = null };
            var normalizedRoleName = "ROLENAME";
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.SetNormalizedRoleNameAsync(role, normalizedRoleName, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
            Assert.That(role.NormalizedName, Is.Null);
        }
        #endregion

        #region SetRoleNameAsync tests
        [Test]
        public async Task SetRoleNameAsync_SetsRoleName_WhenCalled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var roleName = "RoleName";
            var role = new ShopRole { Name = null };

            // Act
            using (var repository = new RoleRepository(context))
            {
                await repository.SetRoleNameAsync(role, roleName);
            }

            // Assert
            Assert.That(role.Name, Is.EqualTo(roleName));
        }

        [Test]
        public void SetRoleNameAsync_ThrowsArgumentNullException_WhenRoleIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            ShopRole role = null;
            var roleName = "RoleName";

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.SetRoleNameAsync(role, roleName);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Does.StartWith("role"));
        }

        [Test]
        public void SetRoleNameAsync_ThrowsArgumentException_WhenRoleNameIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            ShopRole role = new ShopRole();
            string roleName = null;

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.SetRoleNameAsync(role, roleName);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(Act);
            Assert.That(ex.Message, Does.StartWith("roleName"));
        }

        [Test]
        public void SetRoleNameAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            var role = new ShopRole { Name = null };
            var roleName = "rolename";
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.SetRoleNameAsync(role, roleName, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<OperationCanceledException>(Act);
            Assert.That(role.Name, Is.Null);
        }
        #endregion

        #region UpdateAsync tests
        [Test]
        public async Task UpdateAsync_UpdatesRole_WhenCalled()
        {
            // Arrange
            var dbName = nameof(UpdateAsync_UpdatesRole_WhenCalled);
            var context = GetInMemoryContextWithRoles(dbName);
            using (var repository = new RoleRepository(context))
            {
                var role = await context.Roles.FirstAsync();
                role.Name = "UpdatedRoleName";

                // Act
                var result = await repository.UpdateAsync(role);

                // Assert
                Assert.That(result, Is.EqualTo(IdentityResult.Success));
                Assert.That(
                    (await context.Roles.SingleOrDefaultAsync(r => r.Name == role.Name)).Name,
                    Is.EqualTo(role.Name));
            }
        }

        [Test]
        public async Task UpdateAsync_ReturnsIdentityResultFailed_WhenSaveChangesThrowsDbUpdateConcurrencyException()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.Attach(Arg.Any<ShopRole>())
                .Returns(null as EntityEntry<ShopRole>);
            context.Update(Arg.Any<ShopRole>())
                .Returns(null as EntityEntry<ShopRole>);
            var entityEntries = new List<IUpdateEntry> { Substitute.For<IUpdateEntry>() };
            var exception = new DbUpdateConcurrencyException("Update concurrency exception", entityEntries);
            context.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromException<int>(exception));
            IdentityResult result;

            // Act
            using (var repository = new RoleRepository(context))
            {
                result = await repository.UpdateAsync(new ShopRole());
            }

            // Assert
            Assert.That(result, Is.Not.EqualTo(IdentityResult.Success));
        }

        [Test]
        public void UpdateAsync_ThrowsArgumentNullException_WhenRoleIsNull()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            ShopRole role = null;

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.UpdateAsync(role);
                }
            }

            // Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(Act);
            Assert.That(ex.ParamName, Does.StartWith("role"));
        }

        [Test]
        public void UpdateAsync_DoesNotExecute_WhenCanceled()
        {
            // Arrange
            var context = Substitute.For<UserDbContext>();
            context.Attach(Arg.Any<ShopRole>())
                .Returns(null as EntityEntry<ShopRole>);
            context.Update(Arg.Any<ShopRole>())
                .Returns(null as EntityEntry<ShopRole>);
            context.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(1));
            var role = new ShopRole();
            var cancellationToken = new CancellationToken(true);

            // Act
            Task Act()
            {
                using (var repository = new RoleRepository(context))
                {
                    return repository.UpdateAsync(role, cancellationToken);
                }
            }

            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(Act);
            context.DidNotReceiveWithAnyArgs().Attach(Arg.Any<ShopRole>());
            context.DidNotReceiveWithAnyArgs().Update(Arg.Any<ShopRole>());
            context.DidNotReceiveWithAnyArgs().SaveChangesAsync();
        }
        #endregion

        private UserDbContext GetInMemoryContext(string databaseName)
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;

            return new UserDbContext(options);
        }

        private UserDbContext GetInMemoryContextWithRoles(string databaseName)
        {
            var context = GetInMemoryContext(databaseName);
            if(context.Roles.AnyAsync().GetAwaiter().GetResult())
            {
                return context;
            }

            context.AddRange(
                new ShopRole
                {
                    Name = "Administrator",
                    NormalizedName = _normalizer.Normalize("Administrator")
                },
                new ShopRole
                {
                    Name = "User",
                    NormalizedName = _normalizer.Normalize("User")
                }
            );
            context.SaveChanges();
            return context;
        }
    }
}
