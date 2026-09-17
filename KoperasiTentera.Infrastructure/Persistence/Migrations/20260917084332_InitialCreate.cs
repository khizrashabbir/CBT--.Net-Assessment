using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KoperasiTentera.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Banners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IcNumber = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    MobileNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CustomerType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsBiometricEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PinHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PolicyAcceptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedPolicyVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrivacyPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacyPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OtpVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OtpType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtpVerifications_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Banners",
                columns: new[] { "Id", "Description", "ImageUrl", "IsActive", "SortOrder", "Title" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Earn cashback on every eligible transaction this month.", "https://example.com/banners/cashback.png", true, 1, "Oh My Cashback!" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "Discover our newest Shariah-compliant savings plan.", "https://example.com/banners/savings.png", true, 2, "New Shariah Savings" }
                });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "AcceptedPolicyVersion", "CreatedAtUtc", "CustomerType", "Email", "FullName", "IcNumber", "IsBiometricEnabled", "MobileNumber", "PinHash", "PolicyAcceptedAtUtc", "Status", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ExistingUser", "mariam.rashid@example.com", "Mariam Abdul Rashid", "880214566831", false, "+60123456675", null, null, "PendingVerification", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "1.0", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "NewCustomer", "ali.zulkifli@example.com", "Ali Zulkifli", "900101011111", true, "+60123450099", "$2a$11$YE0/Pi43pcuyWFulDWl5pOhTOlnhUgvC.qq.HxpO0FQsodl6hnhkq", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Active", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "PrivacyPolicies",
                columns: new[] { "Id", "Content", "CreatedAtUtc", "IsActive", "Version" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), "By continuing, you agree to Koperasi Tentera's Terms & Conditions and Privacy Policy, which describe how we collect, use, and protect your personal data.", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "1.0" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_IcNumber",
                table: "Customers",
                column: "IcNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OtpVerifications_CustomerId_OtpType_CreatedAtUtc",
                table: "OtpVerifications",
                columns: new[] { "CustomerId", "OtpType", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Banners");

            migrationBuilder.DropTable(
                name: "OtpVerifications");

            migrationBuilder.DropTable(
                name: "PrivacyPolicies");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
