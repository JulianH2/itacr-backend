using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Itacr.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPinLockoutAndPinResetSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SubjectEmployeeId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEndAt",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_SubjectEmployeeId",
                table: "Notifications",
                column: "SubjectEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Employees_SubjectEmployeeId",
                table: "Notifications",
                column: "SubjectEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Employees_SubjectEmployeeId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_SubjectEmployeeId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "SubjectEmployeeId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "LockoutEndAt",
                table: "Employees");
        }
    }
}
