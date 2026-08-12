using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenCardOperationIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CardPayments_EffectiveAmount_Positive",
                table: "CardPayments");

            migrationBuilder.AddColumn<string>(
                name: "FailureCode",
                table: "CardPayments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureDescription",
                table: "CardPayments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RequestedAmount",
                table: "CardPayments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CardPayments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorUserId",
                table: "CardConsumptions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureCode",
                table: "CardConsumptions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureDescription",
                table: "CardConsumptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RequestedAmount",
                table: "CardConsumptions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TargetAccountId",
                table: "CardConsumptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [CardPayments] SET [RequestedAmount] = [EffectiveAmount], [Status] = 'Approved';");

            migrationBuilder.AlterColumn<decimal>(
                name: "RequestedAmount",
                table: "CardPayments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "CardPayments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CardPayments_EffectiveAmount_Valid",
                table: "CardPayments",
                sql: "([Status] = 'Approved' AND [EffectiveAmount] > 0) OR ([Status] = 'Rejected' AND [EffectiveAmount] >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CardPayments_RequestedAmount_Positive",
                table: "CardPayments",
                sql: "[RequestedAmount] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_CardConsumptions_ActorUserId",
                table: "CardConsumptions",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CardConsumptions_TargetAccountId",
                table: "CardConsumptions",
                column: "TargetAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_CardConsumptions_SavingsAccounts_TargetAccountId",
                table: "CardConsumptions",
                column: "TargetAccountId",
                principalTable: "SavingsAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CardConsumptions_Users_ActorUserId",
                table: "CardConsumptions",
                column: "ActorUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardConsumptions_SavingsAccounts_TargetAccountId",
                table: "CardConsumptions");

            migrationBuilder.DropForeignKey(
                name: "FK_CardConsumptions_Users_ActorUserId",
                table: "CardConsumptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CardPayments_EffectiveAmount_Valid",
                table: "CardPayments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CardPayments_RequestedAmount_Positive",
                table: "CardPayments");

            migrationBuilder.DropIndex(
                name: "IX_CardConsumptions_ActorUserId",
                table: "CardConsumptions");

            migrationBuilder.DropIndex(
                name: "IX_CardConsumptions_TargetAccountId",
                table: "CardConsumptions");

            migrationBuilder.DropColumn(
                name: "FailureCode",
                table: "CardPayments");

            migrationBuilder.DropColumn(
                name: "FailureDescription",
                table: "CardPayments");

            migrationBuilder.DropColumn(
                name: "RequestedAmount",
                table: "CardPayments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CardPayments");

            migrationBuilder.DropColumn(
                name: "ActorUserId",
                table: "CardConsumptions");

            migrationBuilder.DropColumn(
                name: "FailureCode",
                table: "CardConsumptions");

            migrationBuilder.DropColumn(
                name: "FailureDescription",
                table: "CardConsumptions");

            migrationBuilder.DropColumn(
                name: "RequestedAmount",
                table: "CardConsumptions");

            migrationBuilder.DropColumn(
                name: "TargetAccountId",
                table: "CardConsumptions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CardPayments_EffectiveAmount_Positive",
                table: "CardPayments",
                sql: "[EffectiveAmount] > 0");
        }
    }
}
