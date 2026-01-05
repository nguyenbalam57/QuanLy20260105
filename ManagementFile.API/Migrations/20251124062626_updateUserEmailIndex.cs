using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManagementFile.API.Migrations
{
    /// <inheritdoc />
    public partial class updateUserEmailIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa index Email
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Tạo lại index nếu rollback
            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }
    }
}
