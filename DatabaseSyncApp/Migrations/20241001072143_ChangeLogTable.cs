using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatabaseSyncApp.Migrations
{
    /// <inheritdoc />
    public partial class ChangeLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NewValue",
                table: "Logs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousValue",
                table: "Logs",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewValue",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "PreviousValue",
                table: "Logs");
        }
    }
}
