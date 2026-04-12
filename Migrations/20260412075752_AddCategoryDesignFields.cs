using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace farm2homeWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryDesignFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorCode",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE Categories SET ImageUrl = '/images/category/traicay.webp', Description = N'Giữ trọn hương vị tự nhiên với công nghệ sấy hiện đại.', ColorCode = 'bg-orange-50' WHERE Name LIKE N'%Trái cây sấy%';
                UPDATE Categories SET ImageUrl = '/images/category/nongsan.webp', Description = N'Sản phẩm tươi sạch, an toàn trực tiếp từ trang trại.', ColorCode = 'bg-green-50' WHERE Name LIKE N'%Nông sản%';
                UPDATE Categories SET ImageUrl = '/images/category/nongsan.webp', Description = N'Nguồn dinh dưỡng quý giá cho sức khỏe mỗi ngày.', ColorCode = 'bg-stone-50' WHERE Name LIKE N'%Hạt đặc sản%';
                UPDATE Categories SET ImageUrl = '/images/category/banh.webp', Description = N'Ngọt ngào hương vị quê hương tinh tế.', ColorCode = 'bg-pink-50' WHERE Name LIKE N'%Bánh kẹo%';
                UPDATE Categories SET ImageUrl = '/images/category/tra.webp', Description = N'Thưởng thức tinh hoa trà Việt đậm đà.', ColorCode = 'bg-emerald-50' WHERE Name LIKE N'%Trà%';
                UPDATE Categories SET ImageUrl = '/images/category/nongsan.webp', Description = N'Khám phá những sản phẩm nông sản tinh túy.', ColorCode = 'bg-gray-50' WHERE ImageUrl IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorCode",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Categories");
        }
    }
}
