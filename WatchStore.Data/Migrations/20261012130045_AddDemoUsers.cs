using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WatchStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "PasswordHash", "Role", "StoreId", "Username" },
                values: new object[,]
                {
                    { 1, "$2a$11$lfcQS78hRc1tJvdwcOEGpO2LKjJQOs0ubAPCe8a.FBLa0/L7LHtWi", 0, null, "admin" },
                    { 2, "$2a$11$z3wgO5Eb7LnRVWdxMOmFvONVpZCsfgw/OUiq2l0rcUEotW.q7wksC", 1, 1, "store1" },
                    { 3, "$2a$11$z3wgO5Eb7LnRVWdxMOmFvONVpZCsfgw/OUiq2l0rcUEotW.q7wksC", 1, 2, "store2" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
