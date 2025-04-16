using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GearShop.Migrations
{
    /// <inheritdoc />
    public partial class _2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Kiểm tra xem cột có tồn tại trước khi xóa
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                           WHERE TABLE_NAME = 'brands' AND COLUMN_NAME = 'ProductTypeId')
                BEGIN
                    ALTER TABLE brands DROP COLUMN ProductTypeId;
                END
            ");

            // Tạo bảng trung gian cho quan hệ nhiều-nhiều
            migrationBuilder.CreateTable(
                name: "BrandProductType",
                columns: table => new
                {
                    BrandsId = table.Column<int>(type: "int", nullable: false),
                    ProductTypesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandProductType", x => new { x.BrandsId, x.ProductTypesId });
                    table.ForeignKey(
                        name: "FK_BrandProductType_brands_BrandsId",
                        column: x => x.BrandsId,
                        principalTable: "brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BrandProductType_productTypes_ProductTypesId",
                        column: x => x.ProductTypesId,
                        principalTable: "productTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrandProductType_ProductTypesId",
                table: "BrandProductType",
                column: "ProductTypesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrandProductType");

            // Kiểm tra trước khi thêm lại cột
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                               WHERE TABLE_NAME = 'brands' AND COLUMN_NAME = 'ProductTypeId')
                BEGIN
                    ALTER TABLE brands ADD ProductTypeId INT NOT NULL DEFAULT 0;
                END
            ");

            migrationBuilder.CreateIndex(
                name: "IX_brands_ProductTypeId",
                table: "brands",
                column: "ProductTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_brands_productTypes_ProductTypeId",
                table: "brands",
                column: "ProductTypeId",
                principalTable: "productTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
