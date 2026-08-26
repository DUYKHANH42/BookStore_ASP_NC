using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModelSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsFlashSale",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SaleEndDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SaleSoldCount",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SaleStock",
                table: "Products");

            migrationBuilder.AddColumn<int>(
                name: "FlashSaleId",
                table: "OrderDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FlashSales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SaleStock = table.Column<int>(type: "int", nullable: false),
                    SoldCount = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlashSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlashSales_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_FlashSaleId",
                table: "OrderDetails",
                column: "FlashSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_FlashSales_ProductId",
                table: "FlashSales",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_FlashSales_FlashSaleId",
                table: "OrderDetails",
                column: "FlashSaleId",
                principalTable: "FlashSales",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_FlashSales_FlashSaleId",
                table: "OrderDetails");

            migrationBuilder.DropTable(
                name: "FlashSales");

            migrationBuilder.DropIndex(
                name: "IX_OrderDetails_FlashSaleId",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "FlashSaleId",
                table: "OrderDetails");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPrice",
                table: "Products",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFlashSale",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SaleEndDate",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SaleSoldCount",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SaleStock",
                table: "Products",
                type: "int",
                nullable: true);
        }
    }
}
