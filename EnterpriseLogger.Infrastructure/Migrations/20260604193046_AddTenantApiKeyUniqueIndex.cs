using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseLogger.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantApiKeyUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Tenants_ApiKey",
                table: "Tenants",
                column: "ApiKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_ApiKey",
                table: "Tenants");
        }
    }
}
