using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SilverbackShop.Catalog.Infrastructure.Migrations
{
    public partial class MessagingTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Messaging_InboundMessages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(nullable: false),
                    EndpointName = table.Column<string>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    Consumed = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messaging_InboundMessages", x => new { x.MessageId, x.EndpointName });
                    table.UniqueConstraint("AK_Messaging_InboundMessages_EndpointName_MessageId", x => new { x.EndpointName, x.MessageId });
                });

            migrationBuilder.CreateTable(
                name: "Messaging_OutboundMessages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    EndpointName = table.Column<string>(nullable: true),
                    Endpoint = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: false),
                    Produced = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messaging_OutboundMessages", x => x.MessageId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Messaging_InboundMessages");

            migrationBuilder.DropTable(
                name: "Messaging_OutboundMessages");
        }
    }
}
