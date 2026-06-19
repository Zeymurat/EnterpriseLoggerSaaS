using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseLogger.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HashTenantApiKeysAndLogIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_ApiKey",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Logs_TenantId",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "Tenants");

            migrationBuilder.AddColumn<string>(
                name: "ApiKeyHash",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_ApiKeyHash",
                table: "Tenants",
                column: "ApiKeyHash",
                unique: true,
                filter: "\"ApiKeyHash\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Logs_TenantId_LogLevel_Timestamp",
                table: "Logs",
                columns: new[] { "TenantId", "LogLevel", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_ApiKeyHash",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Logs_TenantId_LogLevel_Timestamp",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "ApiKeyHash",
                table: "Tenants");

            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "Tenants",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_ApiKey",
                table: "Tenants",
                column: "ApiKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Logs_TenantId",
                table: "Logs",
                column: "TenantId");
        }
    }
}
