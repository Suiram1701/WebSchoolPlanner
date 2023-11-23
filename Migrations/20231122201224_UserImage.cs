using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebSchoolPlanner.Migrations
{
    /// <inheritdoc />
    public partial class UserImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountImage",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "UserImages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Image = table.Column<byte[]>(type: "varbinary(max)", maxLength: 5000000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserImages_AspNetUsers_Id",
                        column: x => x.Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserImages");

            migrationBuilder.AddColumn<byte[]>(
                name: "AccountImage",
                table: "AspNetUsers",
                type: "varbinary(max)",
                maxLength: 5000000,
                nullable: true);
        }
    }
}
