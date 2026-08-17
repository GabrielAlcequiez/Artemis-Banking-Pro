using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardCreationOperationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreationOperationId",
                table: "CreditCards",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [CreditCards] SET [CreationOperationId] = NEWID() WHERE [CreationOperationId] IS NULL");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreationOperationId",
                table: "CreditCards",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CreditCards_CreationOperationId_NonEmpty",
                table: "CreditCards",
                sql: "[CreationOperationId] <> '00000000-0000-0000-0000-000000000000'");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCards_CreationOperationId",
                table: "CreditCards",
                column: "CreationOperationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CreditCards_CreationOperationId",
                table: "CreditCards");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CreditCards_CreationOperationId_NonEmpty",
                table: "CreditCards");

            migrationBuilder.DropColumn(
                name: "CreationOperationId",
                table: "CreditCards");
        }
    }
}
