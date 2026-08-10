using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixLoanClientRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Loans_Users_ClientId1",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_ClientId1",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "ClientId1",
                table: "Loans");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientId1",
                table: "Loans",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_ClientId1",
                table: "Loans",
                column: "ClientId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Loans_Users_ClientId1",
                table: "Loans",
                column: "ClientId1",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
