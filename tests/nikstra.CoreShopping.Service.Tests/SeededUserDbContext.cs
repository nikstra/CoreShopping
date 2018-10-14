using Microsoft.EntityFrameworkCore;
using nikstra.CoreShopping.Service.Data;
using nikstra.CoreShopping.Service.Models;

namespace nikstra.CoreShopping.Service.Tests
{
    public class SeededUserDbContext : UserDbContext
    {
        public SeededUserDbContext(DbContextOptions<UserDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ShopUser>().HasData(
                new ShopUser { UserName = "user1@domain.tld", Email = "user1@domain.tld" },
                new ShopUser { UserName = "user2@domain.tld", Email = "user2@domain.tld" }
            );

            builder.Entity<ShopRole>().HasData(
                new ShopRole { Name = "admin" },
                new ShopRole { Name = "user" }
            );
        }
    }
}
