using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace nikstra.CoreShopping.Service.Models
{
    public class ShopUser
    {
        public ShopUser()
        {
            Id = Guid.NewGuid().ToString();
        }

        public ShopUser(string userName)
            : this()
        {
            UserName = userName;
        }

        public virtual int AccessFailedCount { get; set; }
        public virtual string ConcurrencyStamp { get; set; }
        public virtual string Email { get; set; }
        public virtual bool EmailConfirmed { get; set; }
        public virtual string Id { get; set; }
        public virtual bool LockoutEnabled { get; set; }
        public virtual System.DateTimeOffset? LockoutEnd { get; set; }
        public virtual string NormalizedEmail { get; set; }
        public virtual string NormalizedUserName { get; set; }
        public virtual string PasswordHash { get; set; }
        public virtual string PhoneNumber { get; set; }
        public virtual bool PhoneNumberConfirmed { get; set; }
        public virtual string SecurityStamp { get; set; }
        public virtual bool TwoFactorEnabled { get; set; }
        public virtual string UserName { get; set; }

        public virtual ICollection<ShopUserToken> Tokens { get; set; } = new List<ShopUserToken>();
        public virtual ICollection<ShopUserRole> Roles { get; set; } = new List<ShopUserRole>();
        public virtual ICollection<ShopUserLogin> Logins { get; set; } = new List<ShopUserLogin>();
        public virtual ICollection<ShopUserClaim> Claims { get; set; } = new List<ShopUserClaim>();

        public override string ToString()
        {
            return UserName;
        }
    }
}
