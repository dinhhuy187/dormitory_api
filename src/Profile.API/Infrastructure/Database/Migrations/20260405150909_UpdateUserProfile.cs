using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Profile.API.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_Id",
                table: "UserProfile",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfile_Id",
                table: "UserProfile");
        }
    }
}
