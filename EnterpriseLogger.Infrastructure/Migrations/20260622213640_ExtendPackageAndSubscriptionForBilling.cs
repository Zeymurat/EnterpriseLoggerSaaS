using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EnterpriseLogger.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendPackageAndSubscriptionForBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantSubscriptions_TenantId",
                table: "TenantSubscriptions");

            migrationBuilder.AddColumn<bool>(
                name: "AutoRenew",
                table: "TenantSubscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BillingCycle",
                table: "TenantSubscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TenantSubscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "TenantSubscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Packages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "AllowedLogLevels",
                table: "Packages",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Packages",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Packages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "Packages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Packages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxLogsPerMinute",
                table: "Packages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceAnnual",
                table: "Packages",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceMonthly",
                table: "Packages",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceQuarterly",
                table: "Packages",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceSemiAnnual",
                table: "Packages",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Packages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StorageRetentionDays",
                table: "Packages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "Packages",
                columns: new[] { "Id", "AllowedLogLevels", "Code", "Description", "IsAvailable", "IsDefault", "IsMailEnabled", "IsSmsEnabled", "MaxLogsPerMinute", "MonthlyRequestLimit", "Name", "PriceAnnual", "PriceMonthly", "PriceQuarterly", "PriceSemiAnnual", "SortOrder", "StorageRetentionDays" },
                values: new object[,]
                {
                    { 1, "INFO,WARNING,ERROR", "free", "Başlangıç paketi — sınırlı log kotası", true, true, false, false, 100, 10000, "Free", 0m, 0m, 0m, 0m, 1, 30 },
                    { 2, "INFO,WARNING,ERROR", "basic", "Küçük ekipler için aylık paket", true, false, true, false, 500, 100000, "Basic", 4999m, 499m, 1399m, 2699m, 2, 90 },
                    { 3, "INFO,WARNING,ERROR", "pro", "Yüksek hacimli üretim ortamları", true, false, true, true, 2000, 1000000, "Pro", 19999m, 1999m, 5499m, 10499m, 3, 365 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_TenantId_StartDate",
                table: "TenantSubscriptions",
                columns: new[] { "TenantId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Packages_Code",
                table: "Packages",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantSubscriptions_TenantId_StartDate",
                table: "TenantSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Packages_Code",
                table: "Packages");

            migrationBuilder.DeleteData(
                table: "Packages",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Packages",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Packages",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "AutoRenew",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "BillingCycle",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "MaxLogsPerMinute",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "PriceAnnual",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "PriceMonthly",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "PriceQuarterly",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "PriceSemiAnnual",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "StorageRetentionDays",
                table: "Packages");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Packages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "AllowedLogLevels",
                table: "Packages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_TenantId",
                table: "TenantSubscriptions",
                column: "TenantId");
        }
    }
}
