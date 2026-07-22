using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nucleo.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class IndiceTicketEstadoFecha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_Estado",
                table: "Tickets");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Estado_FechaCreacion",
                table: "Tickets",
                columns: new[] { "Estado", "FechaCreacion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_Estado_FechaCreacion",
                table: "Tickets");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Estado",
                table: "Tickets",
                column: "Estado");
        }
    }
}
