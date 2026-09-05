using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBookPdfPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PdfPath",
                table: "Books",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfPath",
                table: "Books");
        }
    }
}
