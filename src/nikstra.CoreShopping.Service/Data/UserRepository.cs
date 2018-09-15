using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using nikstra.CoreShopping.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

// TODO: Maybe an example to follow?
// https://github.com/kriasoft/AspNet.Identity/blob/master/src/KriaSoft.AspNet.Identity.EntityFramework/UserStore.cs

// http://danderson.io/posts/using-your-own-database-schema-and-classes-with-asp-net-core-identity-and-entity-framework-core/
// http://www.elemarjr.com/en/2017/05/writing-an-asp-net-core-identity-storage-provider-from-scratch-with-ravendb/

// TODO: Implement IUserAuthenticationTokenStore<ShopUser> and/or IUserAuthenticatorKeyStore<ShopUser>.
// If two-factor auth should be used or remove two-factor links in razor pages.

namespace nikstra.CoreShopping.Service.Data
{
    public class UserRepository :
        IUserStore<ShopUser>,
        IUserClaimStore<ShopUser>,
        IUserLoginStore<ShopUser>,
        IUserRoleStore<ShopUser>,
        IUserPasswordStore<ShopUser>,
        IUserSecurityStampStore<ShopUser>,
        IUserTwoFactorStore<ShopUser>,
        IUserPhoneNumberStore<ShopUser>,
        IUserEmailStore<ShopUser>,
        IUserLockoutStore<ShopUser>,
        IQueryableUserStore<ShopUser>
    {
        private UserDbContext _context;

        public UserRepository(UserDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IQueryable<ShopUser> Users => _context.Users;

        public Task AddClaimsAsync(ShopUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (claims == null) throw new ArgumentNullException(nameof(claims));
            cancellationToken.ThrowIfCancellationRequested();

            _context.UserClaims.AddRange(claims.Select(c =>
                new ShopUserClaim { UserId = user.Id, ClaimType = c.Type, ClaimValue = c.Value }));

            return Task.CompletedTask;
        }

        public async Task AddLoginAsync(ShopUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (login == null) throw new ArgumentNullException(nameof(login));
            cancellationToken.ThrowIfCancellationRequested();

            await _context.UserLogins.AddAsync(new ShopUserLogin
            {
                LoginProvider = login.LoginProvider,
                ProviderDisplayName = login.ProviderDisplayName,
                ProviderKey = login.ProviderKey,
                UserId = user.Id
            }, cancellationToken);
        }

        public async Task AddToRoleAsync(ShopUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentException($"{nameof(roleName)} cannot be null or empty.");
            cancellationToken.ThrowIfCancellationRequested();

            var role = await _context.Roles.SingleOrDefaultAsync(r => r.Name == roleName) ??
                throw new InvalidOperationException($"No role named {roleName} found.");

            var userRole = new ShopUserRole
            {
                ShopUser = user,
                ShopRole = role
            };

            user.Roles.Add(userRole);
        }

        public async Task<IdentityResult> CreateAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            _context.Users.Remove(user);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch(DbUpdateConcurrencyException)
            {
                // TODO: Should probably not create a new instance of IdentityErrorDescriber here!?
                return IdentityResult.Failed(new IdentityErrorDescriber().ConcurrencyFailure());
            }

            return IdentityResult.Success;
        }

        public void Dispose()
        {
            _context?.Dispose();
            _context = null;
        }

        public Task<ShopUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(normalizedEmail)) throw new ArgumentException($"{nameof(normalizedEmail)} cannot be null or empty.");
            cancellationToken.ThrowIfCancellationRequested();

            return IncludeUsersNavigationProperties(_context.Users).FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        }

        public Task<ShopUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException($"{nameof(userId)} cannot be null or empty.");
            cancellationToken.ThrowIfCancellationRequested();

            return IncludeUsersNavigationProperties(_context.Users).FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<ShopUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(loginProvider)) throw new ArgumentException($"{nameof(loginProvider)} cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(providerKey)) throw new ArgumentException($"{nameof(providerKey)} cannot be null or empty.");
            cancellationToken.ThrowIfCancellationRequested();

            var userLogin = await _context.UserLogins
                .SingleOrDefaultAsync(ul => ul.LoginProvider == loginProvider && ul.ProviderKey == providerKey, cancellationToken);

            if (userLogin != null)
            {
                return await _context.Users.FindAsync(userLogin.UserId, cancellationToken);
            }

            return null;
        }

        public Task<ShopUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(normalizedUserName)) throw new ArgumentException($"{nameof(normalizedUserName)} cannot be null or empty.");
            cancellationToken.ThrowIfCancellationRequested();

            return IncludeUsersNavigationProperties(_context.Users)
                .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);
        }

        public Task<int> GetAccessFailedCountAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(user.AccessFailedCount);
        }

        public Task<IList<Claim>> GetClaimsAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<IList<Claim>>(user.Claims.Select(c => new Claim(c.ClaimType, c.ClaimValue)).ToList());
        }

        public Task<string> GetEmailAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(user.EmailConfirmed);
        }

        public Task<bool> GetLockoutEnabledAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(user.LockoutEnabled);
        }

        public Task<DateTimeOffset?> GetLockoutEndDateAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(user.LockoutEnd);
        }

        public async Task<IList<UserLoginInfo>> GetLoginsAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            return await _context.UserLogins
                .Where(ul => ul.UserId == user.Id)
                .Select(ul => new UserLoginInfo(ul.LoginProvider, ul.ProviderKey, ul.ProviderDisplayName))
                .ToListAsync();
        }

        public Task<string> GetNormalizedEmailAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(user.NormalizedEmail);
        }

        public Task<string> GetNormalizedUserNameAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(user.NormalizedUserName);
        }

        public Task<string> GetPasswordHashAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(user.PasswordHash);
        }

        public Task<string> GetPhoneNumberAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(user.PhoneNumber);
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(user.PhoneNumberConfirmed);
        }

        public Task<IList<string>> GetRolesAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<IList<string>>(user.Roles.Select(x => x.ShopRole.Name).ToList());
        }

        public Task<string> GetSecurityStampAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(user.SecurityStamp);
        }

        public Task<bool> GetTwoFactorEnabledAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(user.TwoFactorEnabled);
        }

        public Task<string> GetUserIdAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(user.Id);
        }

        public Task<string> GetUserNameAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(user.UserName);
        }

        public async Task<IList<ShopUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (claim == null) throw new ArgumentNullException(nameof(claim));
            cancellationToken.ThrowIfCancellationRequested();

            var query = _context.Users.Join(
                _context.UserClaims.Where(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value),
                u => u.Id,
                c => c.UserId,
                (u, c) => u);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IList<ShopUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentException($"{nameof(roleName)} cannot be null or empty.");
            cancellationToken.ThrowIfCancellationRequested();

            var role = await _context.Roles.SingleOrDefaultAsync(r => r.Name == roleName, cancellationToken);
            if(role != null)
            {
                var query = _context.Users.Join(
                    _context.UserRoles.Where(ur => ur.RoleId == role.Id),
                    u => u.Id,
                    ur => ur.UserId,
                    (u, ur) => u);

                return await query.ToListAsync(cancellationToken);
            }

            return new List<ShopUser>();
        }

        public Task<bool> HasPasswordAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(string.IsNullOrEmpty(user.PasswordHash) == false);
        }

        public Task<int> IncrementAccessFailedCountAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            user.AccessFailedCount++;

            return Task.FromResult(user.AccessFailedCount);
        }

        public async Task<bool> IsInRoleAsync(ShopUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentException($"{nameof(roleName)} cannot be null or empty.");
            cancellationToken.ThrowIfCancellationRequested();

            var role = await _context.Roles.SingleOrDefaultAsync(r => r.Name == roleName);
            if(role != null)
            {
                //return await _context.UserRoles.AnyAsync(ur => ur.RoleId == role.Id && ur.UserId == user.Id, cancellationToken);
                return await _context.UserRoles.FindAsync(new[] { user.Id, role.Id }, cancellationToken) != null;
            }

            return false;
        }

        public async Task RemoveClaimsAsync(ShopUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (claims == null) throw new ArgumentNullException(nameof(claims));
            cancellationToken.ThrowIfCancellationRequested();

            foreach(var claim in claims)
            {
                var foundClaims = await _context.UserClaims.Where(c =>
                        c.UserId == user.Id &&
                        c.ClaimType == claim.Type &&
                        c.ClaimValue == claim.Value)
                    .ToListAsync(cancellationToken);

                _context.UserClaims.RemoveRange(foundClaims);
            }
        }

        public async Task RemoveFromRoleAsync(ShopUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentException($"{nameof(roleName)} cannot be null or empty.");
            cancellationToken.ThrowIfCancellationRequested();

            var role = await _context.Roles.SingleOrDefaultAsync(r => r.Name == roleName, cancellationToken);
            if(role != null)
            {
                var userRole = await _context.UserRoles.FindAsync(new[] { user.Id, role.Id }, cancellationToken);
                if(userRole != null)
                {
                    _context.UserRoles.Remove(userRole);
                }
            }
        }

        public async Task RemoveLoginAsync(ShopUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(loginProvider)) throw new ArgumentException($"{nameof(loginProvider)} cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(providerKey)) throw new ArgumentException($"{nameof(providerKey)} cannot be null or empty.");
            cancellationToken.ThrowIfCancellationRequested();

            var login = await _context.UserLogins.SingleOrDefaultAsync(l =>
                l.LoginProvider == loginProvider &&
                l.ProviderKey == providerKey,
                cancellationToken);

            if(login != null)
            {
                _context.UserLogins.Remove(login);
            }
        }

        public async Task ReplaceClaimAsync(ShopUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (claim == null) throw new ArgumentNullException(nameof(claim));
            if (newClaim == null) throw new ArgumentNullException(nameof(newClaim));
            cancellationToken.ThrowIfCancellationRequested();

            await AddClaimsAsync(user, new[] { newClaim }, cancellationToken);
            await RemoveClaimsAsync(user, new[] { claim }, cancellationToken);
        }

        public Task ResetAccessFailedCountAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            user.AccessFailedCount = 0;
            return Task.CompletedTask;
        }

        public Task SetEmailAsync(ShopUser user, string email, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException($"{nameof(email)} cannot be null or empty.");
            cancellationToken.ThrowIfCancellationRequested();

            user.Email = email;
            return Task.CompletedTask;
        }

        public Task SetEmailConfirmedAsync(ShopUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            user.EmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public Task SetLockoutEnabledAsync(ShopUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            user.LockoutEnabled = enabled;
            return Task.CompletedTask;
        }

        public Task SetLockoutEndDateAsync(ShopUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            user.LockoutEnd = lockoutEnd;
            return Task.CompletedTask;
        }

        public Task SetNormalizedEmailAsync(ShopUser user, string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(normalizedEmail)) throw new ArgumentException($"{nameof(normalizedEmail)} cannot be null or empty.");
            cancellationToken.ThrowIfCancellationRequested();

            user.NormalizedEmail = normalizedEmail;
            return Task.CompletedTask;
        }

        public Task SetNormalizedUserNameAsync(ShopUser user, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(normalizedName)) throw new ArgumentException($"{nameof(normalizedName)} cannot be null or empty.");
            cancellationToken.ThrowIfCancellationRequested();

            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task SetPasswordHashAsync(ShopUser user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException($"{nameof(passwordHash)} cannot be null or empty.");
            cancellationToken.ThrowIfCancellationRequested();

            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task SetPhoneNumberAsync(ShopUser user, string phoneNumber, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(phoneNumber)) throw new ArgumentException($"{nameof(phoneNumber)} cannot be null or empty.");
            cancellationToken.ThrowIfCancellationRequested();

            user.PhoneNumber = phoneNumber;
            return Task.CompletedTask;
        }

        public Task SetPhoneNumberConfirmedAsync(ShopUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            user.PhoneNumberConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public Task SetSecurityStampAsync(ShopUser user, string stamp, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(stamp)) throw new ArgumentException($"{nameof(stamp)} cannot be null or empty.");
            cancellationToken.ThrowIfCancellationRequested();

            user.SecurityStamp = stamp;
            return Task.CompletedTask;
        }

        public Task SetTwoFactorEnabledAsync(ShopUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            user.TwoFactorEnabled = enabled;
            return Task.CompletedTask;
        }

        public Task SetUserNameAsync(ShopUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException($"{nameof(userName)} cannot be null or empty.");
            cancellationToken.ThrowIfCancellationRequested();

            user.UserName = userName;
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> UpdateAsync(ShopUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            cancellationToken.ThrowIfCancellationRequested();

            _context.Attach(user);
            user.ConcurrencyStamp = Guid.NewGuid().ToString(); // TODO: Figure out what ConcurrencyStamp is used for?
            _context.Update(user);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch(DbUpdateConcurrencyException)
            {
                // TODO: Should probably not create a new instance of IdentityErrorDescriber here!?
                return IdentityResult.Failed(new IdentityErrorDescriber().ConcurrencyFailure());
            }

            return IdentityResult.Success;
        }

        protected IQueryable<ShopUser> IncludeUsersNavigationProperties(DbSet<ShopUser> user) =>
            user.Include(u => u.Roles).Include(u => u.Claims).Include(u => u.Logins).Include(u => u.Tokens);
    }
}
