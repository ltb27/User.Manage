using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManage.API.Migrations
{
    public partial class RolesSeeding : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "369949e2-4533-43eb-8f66-2a20c1e31f75", "3", "HR", "Human Resource" });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "412a985a-a314-4fdd-818d-ffb9f71b210d", "1", "Admin", "Admin" });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "68f5296e-e6f9-4ca8-852c-b768c3f9b7b8", "2", "User", "User" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "369949e2-4533-43eb-8f66-2a20c1e31f75");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "412a985a-a314-4fdd-818d-ffb9f71b210d");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "68f5296e-e6f9-4ca8-852c-b768c3f9b7b8");
        }
    }
}
