using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandAdminActivityLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActorRole",
                table: "AdminActivityLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityId",
                table: "AdminActivityLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "AdminActivityLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "AdminActivityLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Module",
                table: "AdminActivityLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActorRole",
                table: "AdminActivityLogs");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "AdminActivityLogs");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "AdminActivityLogs");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "AdminActivityLogs");

            migrationBuilder.DropColumn(
                name: "Module",
                table: "AdminActivityLogs");
        }
    }
}
