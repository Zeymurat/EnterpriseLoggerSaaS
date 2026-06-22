using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseLogger.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLogTenantCorrelationIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Logs_TenantId_CorrelationId",
                table: "Logs",
                columns: new[] { "TenantId", "CorrelationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Logs_TenantId_CorrelationId",
                table: "Logs");
        }
    }
}
