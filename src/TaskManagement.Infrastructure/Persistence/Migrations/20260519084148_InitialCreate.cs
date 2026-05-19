using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaskManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    AssignedTo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Tasks",
                columns: new[] { "Id", "AssignedTo", "CreatedDate", "Description", "IdempotencyKey", "IsDeleted", "ModifiedDate", "Priority", "Status", "Title" },
                values: new object[,]
                {
                    { 1L, "bob", new DateTime(2026, 1, 1, 0, 1, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 1", null, false, new DateTime(2026, 1, 1, 0, 1, 0, 0, DateTimeKind.Utc), 1, 1, "Seed task #1" },
                    { 2L, "carol", new DateTime(2026, 1, 1, 0, 2, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 2", null, false, new DateTime(2026, 1, 1, 0, 2, 0, 0, DateTimeKind.Utc), 2, 2, "Seed task #2" },
                    { 3L, "dave", new DateTime(2026, 1, 1, 0, 3, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 3", null, false, new DateTime(2026, 1, 1, 0, 3, 0, 0, DateTimeKind.Utc), 3, 0, "Seed task #3" },
                    { 4L, "erin", new DateTime(2026, 1, 1, 0, 4, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 4", null, false, new DateTime(2026, 1, 1, 0, 4, 0, 0, DateTimeKind.Utc), 0, 1, "Seed task #4" },
                    { 5L, "alice", new DateTime(2026, 1, 1, 0, 5, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 5", null, false, new DateTime(2026, 1, 1, 0, 5, 0, 0, DateTimeKind.Utc), 1, 2, "Seed task #5" },
                    { 6L, "bob", new DateTime(2026, 1, 1, 0, 6, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 6", null, false, new DateTime(2026, 1, 1, 0, 6, 0, 0, DateTimeKind.Utc), 2, 0, "Seed task #6" },
                    { 7L, "carol", new DateTime(2026, 1, 1, 0, 7, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 7", null, false, new DateTime(2026, 1, 1, 0, 7, 0, 0, DateTimeKind.Utc), 3, 1, "Seed task #7" },
                    { 8L, "dave", new DateTime(2026, 1, 1, 0, 8, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 8", null, false, new DateTime(2026, 1, 1, 0, 8, 0, 0, DateTimeKind.Utc), 0, 2, "Seed task #8" },
                    { 9L, "erin", new DateTime(2026, 1, 1, 0, 9, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 9", null, false, new DateTime(2026, 1, 1, 0, 9, 0, 0, DateTimeKind.Utc), 1, 0, "Seed task #9" },
                    { 10L, "alice", new DateTime(2026, 1, 1, 0, 10, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 10", null, false, new DateTime(2026, 1, 1, 0, 10, 0, 0, DateTimeKind.Utc), 2, 1, "Seed task #10" },
                    { 11L, "bob", new DateTime(2026, 1, 1, 0, 11, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 11", null, false, new DateTime(2026, 1, 1, 0, 11, 0, 0, DateTimeKind.Utc), 3, 2, "Seed task #11" },
                    { 12L, "carol", new DateTime(2026, 1, 1, 0, 12, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 12", null, false, new DateTime(2026, 1, 1, 0, 12, 0, 0, DateTimeKind.Utc), 0, 0, "Seed task #12" },
                    { 13L, "dave", new DateTime(2026, 1, 1, 0, 13, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 13", null, false, new DateTime(2026, 1, 1, 0, 13, 0, 0, DateTimeKind.Utc), 1, 1, "Seed task #13" },
                    { 14L, "erin", new DateTime(2026, 1, 1, 0, 14, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 14", null, false, new DateTime(2026, 1, 1, 0, 14, 0, 0, DateTimeKind.Utc), 2, 2, "Seed task #14" },
                    { 15L, "alice", new DateTime(2026, 1, 1, 0, 15, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 15", null, false, new DateTime(2026, 1, 1, 0, 15, 0, 0, DateTimeKind.Utc), 3, 0, "Seed task #15" },
                    { 16L, "bob", new DateTime(2026, 1, 1, 0, 16, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 16", null, false, new DateTime(2026, 1, 1, 0, 16, 0, 0, DateTimeKind.Utc), 0, 1, "Seed task #16" },
                    { 17L, "carol", new DateTime(2026, 1, 1, 0, 17, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 17", null, false, new DateTime(2026, 1, 1, 0, 17, 0, 0, DateTimeKind.Utc), 1, 2, "Seed task #17" },
                    { 18L, "dave", new DateTime(2026, 1, 1, 0, 18, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 18", null, false, new DateTime(2026, 1, 1, 0, 18, 0, 0, DateTimeKind.Utc), 2, 0, "Seed task #18" },
                    { 19L, "erin", new DateTime(2026, 1, 1, 0, 19, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 19", null, false, new DateTime(2026, 1, 1, 0, 19, 0, 0, DateTimeKind.Utc), 3, 1, "Seed task #19" },
                    { 20L, "alice", new DateTime(2026, 1, 1, 0, 20, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 20", null, false, new DateTime(2026, 1, 1, 0, 20, 0, 0, DateTimeKind.Utc), 0, 2, "Seed task #20" },
                    { 21L, "bob", new DateTime(2026, 1, 1, 0, 21, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 21", null, false, new DateTime(2026, 1, 1, 0, 21, 0, 0, DateTimeKind.Utc), 1, 0, "Seed task #21" },
                    { 22L, "carol", new DateTime(2026, 1, 1, 0, 22, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 22", null, false, new DateTime(2026, 1, 1, 0, 22, 0, 0, DateTimeKind.Utc), 2, 1, "Seed task #22" },
                    { 23L, "dave", new DateTime(2026, 1, 1, 0, 23, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 23", null, false, new DateTime(2026, 1, 1, 0, 23, 0, 0, DateTimeKind.Utc), 3, 2, "Seed task #23" },
                    { 24L, "erin", new DateTime(2026, 1, 1, 0, 24, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 24", null, false, new DateTime(2026, 1, 1, 0, 24, 0, 0, DateTimeKind.Utc), 0, 0, "Seed task #24" },
                    { 25L, "alice", new DateTime(2026, 1, 1, 0, 25, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 25", null, false, new DateTime(2026, 1, 1, 0, 25, 0, 0, DateTimeKind.Utc), 1, 1, "Seed task #25" },
                    { 26L, "bob", new DateTime(2026, 1, 1, 0, 26, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 26", null, false, new DateTime(2026, 1, 1, 0, 26, 0, 0, DateTimeKind.Utc), 2, 2, "Seed task #26" },
                    { 27L, "carol", new DateTime(2026, 1, 1, 0, 27, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 27", null, false, new DateTime(2026, 1, 1, 0, 27, 0, 0, DateTimeKind.Utc), 3, 0, "Seed task #27" },
                    { 28L, "dave", new DateTime(2026, 1, 1, 0, 28, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 28", null, false, new DateTime(2026, 1, 1, 0, 28, 0, 0, DateTimeKind.Utc), 0, 1, "Seed task #28" },
                    { 29L, "erin", new DateTime(2026, 1, 1, 0, 29, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 29", null, false, new DateTime(2026, 1, 1, 0, 29, 0, 0, DateTimeKind.Utc), 1, 2, "Seed task #29" },
                    { 30L, "alice", new DateTime(2026, 1, 1, 0, 30, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 30", null, false, new DateTime(2026, 1, 1, 0, 30, 0, 0, DateTimeKind.Utc), 2, 0, "Seed task #30" },
                    { 31L, "bob", new DateTime(2026, 1, 1, 0, 31, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 31", null, false, new DateTime(2026, 1, 1, 0, 31, 0, 0, DateTimeKind.Utc), 3, 1, "Seed task #31" },
                    { 32L, "carol", new DateTime(2026, 1, 1, 0, 32, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 32", null, false, new DateTime(2026, 1, 1, 0, 32, 0, 0, DateTimeKind.Utc), 0, 2, "Seed task #32" },
                    { 33L, "dave", new DateTime(2026, 1, 1, 0, 33, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 33", null, false, new DateTime(2026, 1, 1, 0, 33, 0, 0, DateTimeKind.Utc), 1, 0, "Seed task #33" },
                    { 34L, "erin", new DateTime(2026, 1, 1, 0, 34, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 34", null, false, new DateTime(2026, 1, 1, 0, 34, 0, 0, DateTimeKind.Utc), 2, 1, "Seed task #34" },
                    { 35L, "alice", new DateTime(2026, 1, 1, 0, 35, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 35", null, false, new DateTime(2026, 1, 1, 0, 35, 0, 0, DateTimeKind.Utc), 3, 2, "Seed task #35" },
                    { 36L, "bob", new DateTime(2026, 1, 1, 0, 36, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 36", null, false, new DateTime(2026, 1, 1, 0, 36, 0, 0, DateTimeKind.Utc), 0, 0, "Seed task #36" },
                    { 37L, "carol", new DateTime(2026, 1, 1, 0, 37, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 37", null, false, new DateTime(2026, 1, 1, 0, 37, 0, 0, DateTimeKind.Utc), 1, 1, "Seed task #37" },
                    { 38L, "dave", new DateTime(2026, 1, 1, 0, 38, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 38", null, false, new DateTime(2026, 1, 1, 0, 38, 0, 0, DateTimeKind.Utc), 2, 2, "Seed task #38" },
                    { 39L, "erin", new DateTime(2026, 1, 1, 0, 39, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 39", null, false, new DateTime(2026, 1, 1, 0, 39, 0, 0, DateTimeKind.Utc), 3, 0, "Seed task #39" },
                    { 40L, "alice", new DateTime(2026, 1, 1, 0, 40, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 40", null, false, new DateTime(2026, 1, 1, 0, 40, 0, 0, DateTimeKind.Utc), 0, 1, "Seed task #40" },
                    { 41L, "bob", new DateTime(2026, 1, 1, 0, 41, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 41", null, false, new DateTime(2026, 1, 1, 0, 41, 0, 0, DateTimeKind.Utc), 1, 2, "Seed task #41" },
                    { 42L, "carol", new DateTime(2026, 1, 1, 0, 42, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 42", null, false, new DateTime(2026, 1, 1, 0, 42, 0, 0, DateTimeKind.Utc), 2, 0, "Seed task #42" },
                    { 43L, "dave", new DateTime(2026, 1, 1, 0, 43, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 43", null, false, new DateTime(2026, 1, 1, 0, 43, 0, 0, DateTimeKind.Utc), 3, 1, "Seed task #43" },
                    { 44L, "erin", new DateTime(2026, 1, 1, 0, 44, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 44", null, false, new DateTime(2026, 1, 1, 0, 44, 0, 0, DateTimeKind.Utc), 0, 2, "Seed task #44" },
                    { 45L, "alice", new DateTime(2026, 1, 1, 0, 45, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 45", null, false, new DateTime(2026, 1, 1, 0, 45, 0, 0, DateTimeKind.Utc), 1, 0, "Seed task #45" },
                    { 46L, "bob", new DateTime(2026, 1, 1, 0, 46, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 46", null, false, new DateTime(2026, 1, 1, 0, 46, 0, 0, DateTimeKind.Utc), 2, 1, "Seed task #46" },
                    { 47L, "carol", new DateTime(2026, 1, 1, 0, 47, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 47", null, false, new DateTime(2026, 1, 1, 0, 47, 0, 0, DateTimeKind.Utc), 3, 2, "Seed task #47" },
                    { 48L, "dave", new DateTime(2026, 1, 1, 0, 48, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 48", null, false, new DateTime(2026, 1, 1, 0, 48, 0, 0, DateTimeKind.Utc), 0, 0, "Seed task #48" },
                    { 49L, "erin", new DateTime(2026, 1, 1, 0, 49, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 49", null, false, new DateTime(2026, 1, 1, 0, 49, 0, 0, DateTimeKind.Utc), 1, 1, "Seed task #49" },
                    { 50L, "alice", new DateTime(2026, 1, 1, 0, 50, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 50", null, false, new DateTime(2026, 1, 1, 0, 50, 0, 0, DateTimeKind.Utc), 2, 2, "Seed task #50" },
                    { 51L, "bob", new DateTime(2026, 1, 1, 0, 51, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 51", null, false, new DateTime(2026, 1, 1, 0, 51, 0, 0, DateTimeKind.Utc), 3, 0, "Seed task #51" },
                    { 52L, "carol", new DateTime(2026, 1, 1, 0, 52, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 52", null, false, new DateTime(2026, 1, 1, 0, 52, 0, 0, DateTimeKind.Utc), 0, 1, "Seed task #52" },
                    { 53L, "dave", new DateTime(2026, 1, 1, 0, 53, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 53", null, false, new DateTime(2026, 1, 1, 0, 53, 0, 0, DateTimeKind.Utc), 1, 2, "Seed task #53" },
                    { 54L, "erin", new DateTime(2026, 1, 1, 0, 54, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 54", null, false, new DateTime(2026, 1, 1, 0, 54, 0, 0, DateTimeKind.Utc), 2, 0, "Seed task #54" },
                    { 55L, "alice", new DateTime(2026, 1, 1, 0, 55, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 55", null, false, new DateTime(2026, 1, 1, 0, 55, 0, 0, DateTimeKind.Utc), 3, 1, "Seed task #55" },
                    { 56L, "bob", new DateTime(2026, 1, 1, 0, 56, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 56", null, false, new DateTime(2026, 1, 1, 0, 56, 0, 0, DateTimeKind.Utc), 0, 2, "Seed task #56" },
                    { 57L, "carol", new DateTime(2026, 1, 1, 0, 57, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 57", null, false, new DateTime(2026, 1, 1, 0, 57, 0, 0, DateTimeKind.Utc), 1, 0, "Seed task #57" },
                    { 58L, "dave", new DateTime(2026, 1, 1, 0, 58, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 58", null, false, new DateTime(2026, 1, 1, 0, 58, 0, 0, DateTimeKind.Utc), 2, 1, "Seed task #58" },
                    { 59L, "erin", new DateTime(2026, 1, 1, 0, 59, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 59", null, false, new DateTime(2026, 1, 1, 0, 59, 0, 0, DateTimeKind.Utc), 3, 2, "Seed task #59" },
                    { 60L, "alice", new DateTime(2026, 1, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Auto-generated seed task number 60", null, false, new DateTime(2026, 1, 1, 1, 0, 0, 0, DateTimeKind.Utc), 0, 0, "Seed task #60" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_AssignedTo",
                table: "Tasks",
                column: "AssignedTo");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_IsDeleted",
                table: "Tasks",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Priority",
                table: "Tasks",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Status",
                table: "Tasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Status_Priority",
                table: "Tasks",
                columns: new[] { "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "UX_Tasks_IdempotencyKey",
                table: "Tasks",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
