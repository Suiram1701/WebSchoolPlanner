using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebSchoolPlanner.Migrations
{
    /// <inheritdoc />
    public partial class TMP01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "AccountImage",
                table: "AspNetUsers",
                type: "varbinary(max)",
                maxLength: 5000000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountImage",
                table: "AspNetUsers");
        }
    }
}
