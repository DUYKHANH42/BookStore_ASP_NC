using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashSaleCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Gỡ bỏ tham chiếu từ OrderDetails trước để tránh lỗi REFERENCE constraint
            migrationBuilder.Sql("UPDATE [OrderDetails] SET [FlashSaleId] = NULL;");
            
            // Xóa toàn bộ dữ liệu cũ của bảng FlashSales do kiến trúc mới yêu cầu tham chiếu đến CampaignId
            // Việc xóa này để tránh lỗi FOREIGN KEY constraint khi thêm cột CampaignId không cho phép NULL
            migrationBuilder.Sql("DELETE FROM [FlashSales];");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "FlashSales");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "FlashSales");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "FlashSales");

            migrationBuilder.AddColumn<int>(
                name: "FlashSaleCampaignId",
                table: "FlashSales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "FlashSaleCampaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlashSaleCampaigns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlashSales_FlashSaleCampaignId",
                table: "FlashSales",
                column: "FlashSaleCampaignId");

            migrationBuilder.AddForeignKey(
                name: "FK_FlashSales_FlashSaleCampaigns_FlashSaleCampaignId",
                table: "FlashSales",
                column: "FlashSaleCampaignId",
                principalTable: "FlashSaleCampaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlashSales_FlashSaleCampaigns_FlashSaleCampaignId",
                table: "FlashSales");

            migrationBuilder.DropTable(
                name: "FlashSaleCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_FlashSales_FlashSaleCampaignId",
                table: "FlashSales");

            migrationBuilder.DropColumn(
                name: "FlashSaleCampaignId",
                table: "FlashSales");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "FlashSales",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "FlashSales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartTime",
                table: "FlashSales",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
