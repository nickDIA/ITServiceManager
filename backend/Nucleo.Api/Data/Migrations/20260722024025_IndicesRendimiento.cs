using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nucleo.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class IndicesRendimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Estado",
                table: "Tickets",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Prioridad",
                table: "Tickets",
                column: "Prioridad");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Nombre",
                table: "Clientes",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_Activos_Estado",
                table: "Activos",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Activos_Nombre",
                table: "Activos",
                column: "Nombre");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_Estado",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Prioridad",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_Nombre",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Activos_Estado",
                table: "Activos");

            migrationBuilder.DropIndex(
                name: "IX_Activos_Nombre",
                table: "Activos");
        }
    }
}
