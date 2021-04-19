using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TerritoryTools.Web.Data.Migrations
{
    public partial class AddUrlHost : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HostId",
                table: "ShortUrls",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "ShortUrls",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ShortUrlHosts",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: false),
                    AllowNewUrls = table.Column<bool>(nullable: false),
                    Created = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortUrlHosts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_HostId",
                table: "ShortUrls",
                column: "HostId");

            migrationBuilder.Sql(
                $"INSERT INTO ShortUrlHosts (Id, Name, AllowNewUrls, Created) " +
                $"VALUES (0, 'localhost', 1, '{DateTime.Now}')");

            migrationBuilder.Sql(
                $"UPDATE ShortUrls SET HostId = (SELECT MAX(Id) FROM ShortUrlHosts)");

            migrationBuilder.AddForeignKey(
                name: "ForeignKey_ShortUrl_Host",
                table: "ShortUrls",
                column: "HostId",
                principalTable: "ShortUrlHosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "ForeignKey_ShortUrl_Host",
                table: "ShortUrls");

            migrationBuilder.DropTable(
                name: "ShortUrlHosts");

            migrationBuilder.DropIndex(
                name: "IX_ShortUrls_HostId",
                table: "ShortUrls");

            migrationBuilder.DropColumn(
                name: "HostId",
                table: "ShortUrls");

            migrationBuilder.DropColumn(
                name: "Path",
                table: "ShortUrls");
        }
    }
}
