using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using nikstra.CoreShopping.Service.Models;
using nikstra.CoreShopping.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// http://danderson.io/posts/using-your-own-database-schema-and-classes-with-asp-net-core-identity-and-entity-framework-core/
// http://www.elemarjr.com/en/2017/05/writing-an-asp-net-core-identity-storage-provider-from-scratch-with-ravendb/

namespace nikstra.CoreShopping.Service.Data
{
    public class UserRepository : IUserStore<ShopUser>, IUserClaimStore<ShopUser>, IUserLoginStore<ShopUser>,
        IUserRoleStore<ShopUser>, IUserPasswordStore<ShopUser>, IUserSecurityStampStore<ShopUser>,
        IUserTwoFactorStore<ShopUser>, IUserPhoneNumberStore<ShopUser>, IUserEmailStore<ShopUser>,
        IUserLockoutStore<ShopUser>, IQueryableUserStore<ShopUser>
    {
        private UserDbContext _context;

        public UserRepository(UserDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        public IQueryable<ShopUser> Users => _context.Users;

        public Task AddClaimsAsync(ShopUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _context.UserClaims.AddRange(claims.Select(c =>
                new Models.ShopUserClaim { UserId = user.Id, ClaimType = c.Type, ClaimValue = c.Value }));

            return _context.SaveChangesAsync(cancellationToken);
        }

        public async Task AddLoginAsync(ShopUser user, UserLoginInfo login, CancellationToken cancellationToken)
        {
            await _context.UserLogins.AddAsync(new ShopUserLogin
            {
                LoginProvider = login.LoginProvider,
                ProviderDisplayName = login.ProviderDisplayName,
                ProviderKey = login.ProviderKey,
                UserId = user.Id
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task AddToRoleAsync(ShopUser user, string roleName, CancellationToken cancellationToken)
        {
            // TODO: Maybe an example to follow?
            // https://github.com/kriasoft/AspNet.Identity/blob/master/src/KriaSoft.AspNet.Identity.EntityFramework/UserStore.cs

            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentException($"{roleName} cannot be null or whitespace.");

            var role = await _context.Roles.SingleOrDefaultAsync(r => r.Name == roleName) ??
                throw new InvalidOperationException($"No role named {roleName} found.");

            var userRole = new ShopUserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            };

            user.Roles.Add(userRole);
        }

        //
        public async Task<IdentityResult> CreateAsync(ShopUser user, CancellationToken cancellationToken)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }

        public Task<IdentityResult> DeleteAsync(ShopUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _context?.Dispose();
            _context = null;
        }

        public Task<ShopUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            IncludeUsersNavigationProperties(_context.Users).FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

        public Task<ShopUser> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            IncludeUsersNavigationProperties(_context.Users).FirstOrDefaultAsync(u => u.Id == userId);

        public Task<ShopUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ShopUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
            IncludeUsersNavigationProperties(_context.Users)
                .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);
        //return _context.Users
        //    .Include(u => u.Roles).Include(u => u.Claims).Include(u => u.Logins).Include(u => u.Tokens)
        //    .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName);

        // TODO: Shouldn't GetAccessFailedCountAsync() actually querey the db?
        public Task<int> GetAccessFailedCountAsync(ShopUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user?.AccessFailedCount ?? throw new ArgumentNullException(nameof(user)));

        public Task<IList<Claim>> GetClaimsAsync(ShopUser user, CancellationToken cancellationToken) =>
            Task.FromResult<IList<Claim>>(user?.Claims.Select(c => new Claim(c.ClaimType, c.ClaimValue)).ToList() ??
                throw new ArgumentNullException(nameof(user)));

        public Task<string> GetEmailAsync(ShopUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> GetEmailConfirmedAsync(ShopUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> GetLockoutEnabledAsync(ShopUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<DateTimeOffset?> GetLockoutEndDateAsync(ShopUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<IList<UserLoginInfo>> GetLoginsAsync(ShopUser user, CancellationToken cancellationToken)
        {
            return await _context.UserLogins
                .Where(ul => ul.UserId == user.Id)
                .Select(ul => new UserLoginInfo(ul.LoginProvider, ul.ProviderKey, ul.ProviderDisplayName))
                .ToListAsync();
        }

        public Task<string> GetNormalizedEmailAsync(ShopUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetNormalizedUserNameAsync(ShopUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetPasswordHashAsync(ShopUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetPhoneNumberAsync(ShopUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(ShopUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<string>> GetRolesAsync(ShopUser user, CancellationToken cancellationToken)
        {
            var ro = user.Roles.Select(x => x.UserId);
            var t = user.Roles.Select(x => x.ShopRole).ToList();
            IList<string> roles = user.Roles.Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name).ToList(); // .Where(r => r.UserId == user.Id).Select(u => u.Name).ToList();
            //IList<string> roles = user.Roles.Where(r => r.UserId == user.Id).Select(u => u.Name).ToList();
            return Task.FromResult(roles); // Task.FromResult(user.Roles.Select(u => u.Name).ToList());
            //var u = await _context.Users.SingleAsync(u => u.Id == user.Id);
            //var roles = u.Roles;
            //return roles.ToListAsync();
            //throw new NotImplementedException();
        }

        public Task<string> GetSecurityStampAsync(ShopUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> GetTwoFactorEnabledAsync(ShopUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetUserIdAsync(ShopUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetUserNameAsync(ShopUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<ShopUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<ShopUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> HasPasswordAsync(ShopUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<int> IncrementAccessFailedCountAsync(ShopUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsInRoleAsync(ShopUser user, string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RemoveClaimsAsync(ShopUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RemoveFromRoleAsync(ShopUser user, string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RemoveLoginAsync(ShopUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task ReplaceClaimAsync(ShopUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task ResetAccessFailedCountAsync(ShopUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetEmailAsync(ShopUser user, string email, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetEmailConfirmedAsync(ShopUser user, bool confirmed, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetLockoutEnabledAsync(ShopUser user, bool enabled, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetLockoutEndDateAsync(ShopUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetNormalizedEmailAsync(ShopUser user, string normalizedEmail, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetNormalizedUserNameAsync(ShopUser user, string normalizedName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetPasswordHashAsync(ShopUser user, string passwordHash, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetPhoneNumberAsync(ShopUser user, string phoneNumber, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetPhoneNumberConfirmedAsync(ShopUser user, bool confirmed, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetSecurityStampAsync(ShopUser user, string stamp, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetTwoFactorEnabledAsync(ShopUser user, bool enabled, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetUserNameAsync(ShopUser user, string userName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> UpdateAsync(ShopUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected IQueryable<ShopUser> IncludeUsersNavigationProperties(DbSet<ShopUser> user) =>
            user.Include(u => u.Roles).Include(u => u.Claims).Include(u => u.Logins).Include(u => u.Tokens);
    }
}
