using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuctionOx.Migrations
{
    /// <inheritdoc />
    public partial class AddConcurrencyToAuctionItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AuctionItems",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AuctionItems");
        }
    }
}
