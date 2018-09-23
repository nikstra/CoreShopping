using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using nikstra.CoreShopping.Service.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace nikstra.CoreShopping.Service.Data
{
    public class RoleRepository : IRoleStore<ShopRole>
    {
        private UserDbContext _context;

        public RoleRepository(UserDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IdentityResult> CreateAsync(ShopRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            cancellationToken.ThrowIfCancellationRequested();

            _context.Roles.Add(role);
            await _context.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(ShopRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            cancellationToken.ThrowIfCancellationRequested();

            _context.Roles.Remove(role);

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

        public Task<ShopRole> FindByIdAsync(string roleId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(roleId)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(roleId));
            cancellationToken.ThrowIfCancellationRequested();

            return _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        }

        public Task<ShopRole> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(normalizedRoleName)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(normalizedRoleName));
            cancellationToken.ThrowIfCancellationRequested();

            return _context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken);
        }

        public Task<string> GetNormalizedRoleNameAsync(ShopRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(role.NormalizedName);
        }

        public Task<string> GetRoleIdAsync(ShopRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(role.Id);
        }

        public Task<string> GetRoleNameAsync(ShopRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(role.Name);
        }

        public Task SetNormalizedRoleNameAsync(ShopRole role, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            if (string.IsNullOrWhiteSpace(normalizedName)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(normalizedName));
            cancellationToken.ThrowIfCancellationRequested();

            role.NormalizedName = normalizedName;
            return Task.CompletedTask;
        }

        public Task SetRoleNameAsync(ShopRole role, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(roleName));
            cancellationToken.ThrowIfCancellationRequested();

            role.Name = roleName;
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> UpdateAsync(ShopRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            cancellationToken.ThrowIfCancellationRequested();

            _context.Attach(role);
            role.ConcurrencyStamp = Guid.NewGuid().ToString(); // TODO: Figure out what ConcurrencyStamp is used for?
            _context.Update(role);

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
    }
}
