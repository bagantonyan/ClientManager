using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    INN = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ClientType = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                    table.UniqueConstraint("AK_Clients_Id_ClientType", x => new { x.Id, x.ClientType });
                    table.CheckConstraint("CK_Client_INN_LengthByType", "([ClientType] = 1 AND LEN([INN]) = 10) OR ([ClientType] = 2 AND LEN([INN]) = 12)");
                });

            migrationBuilder.CreateTable(
                name: "Founders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    INN = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Founders", x => x.Id);
                    table.CheckConstraint("CK_Founder_INN_Length", "LEN([INN]) = 12");
                });

            migrationBuilder.CreateTable(
                name: "ClientFounders",
                columns: table => new
                {
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FounderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientType = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientFounders", x => new { x.ClientId, x.FounderId });
                    table.CheckConstraint("CK_ClientFounder_LegalEntityOnly", "[ClientType] = 1");
                    table.ForeignKey(
                        name: "FK_ClientFounders_Clients_ClientId_ClientType",
                        columns: x => new { x.ClientId, x.ClientType },
                        principalTable: "Clients",
                        principalColumns: new[] { "Id", "ClientType" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientFounders_Founders_FounderId",
                        column: x => x.FounderId,
                        principalTable: "Founders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientFounders_ClientId_ClientType",
                table: "ClientFounders",
                columns: new[] { "ClientId", "ClientType" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientFounders_FounderId",
                table: "ClientFounders",
                column: "FounderId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_INN",
                table: "Clients",
                column: "INN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Founders_INN",
                table: "Founders",
                column: "INN",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientFounders");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "Founders");
        }
    }
}
