using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueCommerceUserAssociation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_CommerceId",
                table: "Users",
                column: "CommerceId",
                unique: true,
                filter: "[CommerceId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Commerces_CommerceId",
                table: "Users",
                column: "CommerceId",
                principalTable: "Commerces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Commerces_CommerceId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_CommerceId",
                table: "Users");
        }
    }
}
