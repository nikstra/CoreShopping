using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace nikstra.CoreShopping.Service.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "CoreShopping");

            migrationBuilder.CreateTable(
                name: "ShopRoles",
                schema: "CoreShopping",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    Name = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShopUsers",
                schema: "CoreShopping",
                columns: table => new
                {
                    AccessFailedCount = table.Column<int>(nullable: false),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    Email = table.Column<string>(maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(nullable: false),
                    Id = table.Column<string>(nullable: false),
                    LockoutEnabled = table.Column<bool>(nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(nullable: true),
                    NormalizedEmail = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(maxLength: 256, nullable: true),
                    PasswordHash = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(nullable: false),
                    SecurityStamp = table.Column<string>(nullable: true),
                    TwoFactorEnabled = table.Column<bool>(nullable: false),
                    UserName = table.Column<string>(maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShopRoleClaims",
                schema: "CoreShopping",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true),
                    RoleId = table.Column<string>(nullable: false),
                    ShopRoleId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopRoleClaims_ShopRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "CoreShopping",
                        principalTable: "ShopRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopRoleClaims_ShopRoles_ShopRoleId",
                        column: x => x.ShopRoleId,
                        principalSchema: "CoreShopping",
                        principalTable: "ShopRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShopUserClaims",
                schema: "CoreShopping",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: false),
                    ShopUserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopUserClaims_ShopUsers_ShopUserId",
                        column: x => x.ShopUserId,
                        principalSchema: "CoreShopping",
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopUserClaims_ShopUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "CoreShopping",
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopUserLogins",
                schema: "CoreShopping",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(nullable: false),
                    ProviderKey = table.Column<string>(nullable: false),
                    ProviderDisplayName = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: false),
                    ShopUserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_ShopUserLogins_ShopUsers_ShopUserId",
                        column: x => x.ShopUserId,
                        principalSchema: "CoreShopping",
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopUserLogins_ShopUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "CoreShopping",
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopUserRoles",
                schema: "CoreShopping",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    ShopUserId1 = table.Column<string>(nullable: true),
                    RoleId = table.Column<string>(nullable: false),
                    ShopRoleId1 = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_ShopUserRoles_ShopRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "CoreShopping",
                        principalTable: "ShopRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopUserRoles_ShopRoles_ShopRoleId1",
                        column: x => x.ShopRoleId1,
                        principalSchema: "CoreShopping",
                        principalTable: "ShopRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopUserRoles_ShopUsers_ShopUserId1",
                        column: x => x.ShopUserId1,
                        principalSchema: "CoreShopping",
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopUserRoles_ShopUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "CoreShopping",
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopUserTokens",
                schema: "CoreShopping",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    LoginProvider = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true),
                    ShopUserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopUserTokens", x => new { x.LoginProvider, x.Name, x.UserId });
                    table.ForeignKey(
                        name: "FK_ShopUserTokens_ShopUsers_ShopUserId",
                        column: x => x.ShopUserId,
                        principalSchema: "CoreShopping",
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopUserTokens_ShopUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "CoreShopping",
                        principalTable: "ShopUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopRoleClaims_RoleId",
                schema: "CoreShopping",
                table: "ShopRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopRoleClaims_ShopRoleId",
                schema: "CoreShopping",
                table: "ShopRoleClaims",
                column: "ShopRoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "CoreShopping",
                table: "ShopRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ShopUserClaims_ShopUserId",
                schema: "CoreShopping",
                table: "ShopUserClaims",
                column: "ShopUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopUserClaims_UserId",
                schema: "CoreShopping",
                table: "ShopUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopUserLogins_ShopUserId",
                schema: "CoreShopping",
                table: "ShopUserLogins",
                column: "ShopUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopUserLogins_UserId",
                schema: "CoreShopping",
                table: "ShopUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopUserRoles_RoleId",
                schema: "CoreShopping",
                table: "ShopUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopUserRoles_ShopRoleId1",
                schema: "CoreShopping",
                table: "ShopUserRoles",
                column: "ShopRoleId1");

            migrationBuilder.CreateIndex(
                name: "IX_ShopUserRoles_ShopUserId1",
                schema: "CoreShopping",
                table: "ShopUserRoles",
                column: "ShopUserId1");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "CoreShopping",
                table: "ShopUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "CoreShopping",
                table: "ShopUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ShopUserTokens_ShopUserId",
                schema: "CoreShopping",
                table: "ShopUserTokens",
                column: "ShopUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopUserTokens_UserId",
                schema: "CoreShopping",
                table: "ShopUserTokens",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopRoleClaims",
                schema: "CoreShopping");

            migrationBuilder.DropTable(
                name: "ShopUserClaims",
                schema: "CoreShopping");

            migrationBuilder.DropTable(
                name: "ShopUserLogins",
                schema: "CoreShopping");

            migrationBuilder.DropTable(
                name: "ShopUserRoles",
                schema: "CoreShopping");

            migrationBuilder.DropTable(
                name: "ShopUserTokens",
                schema: "CoreShopping");

            migrationBuilder.DropTable(
                name: "ShopRoles",
                schema: "CoreShopping");

            migrationBuilder.DropTable(
                name: "ShopUsers",
                schema: "CoreShopping");
        }
    }
}
