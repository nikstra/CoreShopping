using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using nikstra.CoreShopping.Service.Models;
using nikstra.CoreShopping.Web.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace nikstra.CoreShopping.Service.Data
{
    public class UserDbContext : DbContext
    {
        public UserDbContext()
        {
        }

        public UserDbContext(DbContextOptions<UserDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //base.OnConfiguring(optionsBuilder);

            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=EFProviders.InMemory;Trusted_Connection=True;ConnectRetryCount=0");
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Configuration "stolen" from the IdentityUserContext classes at https://github.com/aspnet/Identity

            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
            builder.Entity<ShopUser>(b =>
            {
                b.HasKey(u => u.Id);
                b.HasIndex(u => u.NormalizedUserName).HasName("UserNameIndex").IsUnique();
                b.HasIndex(u => u.NormalizedEmail).HasName("EmailIndex");
                b.ToTable("ShopUsers", "CoreShopping");
                b.Property(u => u.ConcurrencyStamp).IsConcurrencyToken();

                b.Property(u => u.UserName).HasMaxLength(256);
                b.Property(u => u.NormalizedUserName).HasMaxLength(256);
                b.Property(u => u.Email).HasMaxLength(256);
                b.Property(u => u.NormalizedEmail).HasMaxLength(256);

                b.HasMany<ShopUserClaim>().WithOne().HasForeignKey(uc => uc.UserId).IsRequired();
                b.HasMany<ShopUserLogin>().WithOne().HasForeignKey(ul => ul.UserId).IsRequired();
                b.HasMany<ShopUserToken>().WithOne().HasForeignKey(ut => ut.UserId).IsRequired();

                //b.HasMany<ShopRole>().WithOne().HasForeignKey(ur => ur.Id).IsRequired();
                b.HasMany<ShopUserRole>(r => r.Roles).WithOne().HasForeignKey(ur => ur.UserId).IsRequired();

                // What I came up with.
                //b.HasMany<ShopUserRole>().WithOne().HasForeignKey(ur => ur.UserId).IsRequired();
                //b.HasMany<ShopUserLogin>().WithOne().HasForeignKey(ul => ul.UserId).IsRequired();

            });

            builder.Entity<ShopUserClaim>(b =>
            {
                b.HasKey(uc => uc.Id);
                b.ToTable("ShopUserClaims", "CoreShopping");
            });

            builder.Entity<ShopUserLogin>(b =>
            {
                b.HasKey(k => new { k.LoginProvider, k.ProviderKey });
                b.ToTable("ShopUserLogins", "CoreShopping");
                //b.HasMany<ShopUser>().WithOne().HasForeignKey(su => su.Id).IsRequired();
            });

            builder.Entity<ShopUserToken>(b =>
            {
                b.HasKey(k => new { k.LoginProvider, k.Name, k.UserId });
                b.ToTable("ShopUserTokens", "CoreShopping");
            });

            builder.Entity<ShopRole>(b =>
            {
                b.HasKey(r => r.Id);
                b.HasIndex(r => r.NormalizedName).HasName("RoleNameIndex").IsUnique();
                b.ToTable("ShopRoles", "CoreShopping");
                b.Property(r => r.ConcurrencyStamp).IsConcurrencyToken();

                b.Property(u => u.Name).HasMaxLength(256);
                b.Property(u => u.NormalizedName).HasMaxLength(256);

                b.HasMany<ShopUserRole>(r => r.Users).WithOne().HasForeignKey(ur => ur.RoleId).IsRequired();
                b.HasMany<ShopRoleClaim>().WithOne().HasForeignKey(rc => rc.RoleId).IsRequired();

                // What I came up with.
                //b.HasKey(r => r.Id);
                //b.HasIndex(r => r.NormailzedName).HasName("RoleNameIndex").IsUnique();
                //b.Property(r => r.ConcurrencyStamp).IsConcurrencyToken();
                //b.Property(r => r.Name).HasMaxLength(256);
                //b.Property(r => r.NormailzedName).HasMaxLength(256);
                //b.HasMany<ShopUserRole>().WithOne().HasForeignKey(ur => ur.RoleId).IsRequired();
                //b.HasMany<ShopRoleClaim>().WithOne().HasForeignKey(rc => rc.RoleId).IsRequired();
            });

            builder.Entity<ShopRoleClaim>(b =>
            {
                b.HasKey(rc => rc.Id);
                b.ToTable("ShopRoleClaims", "CoreShopping");
            });

            builder.Entity<ShopUserRole>(b =>
            {
                b.HasKey(r => new { r.UserId, r.RoleId });
                b.ToTable("ShopUserRoles", "CoreShopping");
            });
        }

        // https://github.com/aspnet/Identity/blob/release/2.1/src/EF/IdentityDbContext.cs
        // https://github.com/aspnet/Identity/tree/release/2.1/src/Stores
        public DbSet<ShopUser> Users { get; set; }
        public DbSet<ShopUserClaim> UserClaims { get; set; }
        public DbSet<ShopRole> Roles { get; set; }
        public DbSet<ShopRoleClaim> RoleClaims { get; set; }
        public DbSet<ShopUserLogin> UserLogins { get; set; }
        public DbSet<ShopUserToken> UserTokens { get; set; }
        //public DbSet<ShopUserRole> UserRoles { get; set; }
    }
}
