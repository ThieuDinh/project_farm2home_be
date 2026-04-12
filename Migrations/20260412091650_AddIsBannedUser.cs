using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace farm2homeWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddIsBannedUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBanned",
                table: "AppUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBanned",
                table: "AppUsers");
        }
    }
}
