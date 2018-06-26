using System;
using System.Collections.Generic;

namespace nikstra.CoreShopping.Service.Models
{
    public class ShopRole
    {
        public ShopRole()
        {
            Id = Guid.NewGuid().ToString();
        }

        public ShopRole(string roleName)
            : this()
        {
            Name = roleName;
        }

        public virtual string Id { get; set; }
        public virtual string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
        public virtual string Name { get; set; }
        public virtual string NormalizedName { get; set; }

        public virtual ICollection<ShopUserRole> Users { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
}