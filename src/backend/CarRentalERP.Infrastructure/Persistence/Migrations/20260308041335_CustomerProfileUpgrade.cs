using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerProfileUpgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlternatePhone",
                table: "Customers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Customers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "Customers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName",
                table: "Customers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                table: "Customers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdentityDocumentNumber",
                table: "Customers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdentityDocumentType",
                table: "Customers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Customers",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "Customers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Customers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Customers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RiskNotes",
                table: "Customers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Customers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE Customers
                SET
                    City = CASE WHEN City = '' THEN 'Unknown City' ELSE City END,
                    State = CASE WHEN State = '' THEN 'Unknown State' ELSE State END,
                    PostalCode = CASE WHEN PostalCode = '' THEN '000000' ELSE PostalCode END,
                    Nationality = CASE WHEN Nationality = '' THEN 'Unknown' ELSE Nationality END,
                    IdentityDocumentType = CASE WHEN IdentityDocumentType = '' THEN 'Legacy ID' ELSE IdentityDocumentType END,
                    IdentityDocumentNumber = CASE WHEN IdentityDocumentNumber = '' THEN 'LEGACY-' || CustomerCode ELSE IdentityDocumentNumber END,
                    EmergencyContactName = CASE WHEN EmergencyContactName = '' THEN 'Not Provided' ELSE EmergencyContactName END,
                    EmergencyContactPhone = CASE WHEN EmergencyContactPhone = '' THEN Phone ELSE EmergencyContactPhone END,
                    IsActive = 1
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_IdentityDocumentNumber",
                table: "Customers",
                column: "IdentityDocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_IsActive",
                table: "Customers",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_IdentityDocumentNumber",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_IsActive",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "AlternatePhone",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IdentityDocumentNumber",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IdentityDocumentType",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "RiskNotes",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Customers");
        }
    }
}
