using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaRa.Api.Migrations
{
    /// <inheritdoc />
    public partial class VisitesEtValidationGammes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "expire_at",
                table: "gammes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_document",
                table: "gammes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "statut",
                table: "gammes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "valide_at",
                table: "gammes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "valide_par",
                table: "gammes",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "visites_essieu",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_essieu = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    gamme_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gamme_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    gamme_version = table.Column<int>(type: "integer", nullable: false),
                    gamme_statut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    operateur_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    signataire = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    debut_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cloture_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    recue_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    nombre_ecarts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visites_essieu", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "visites_etapes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    visite_id = table.Column<Guid>(type: "uuid", nullable: false),
                    etape_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordre = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    resultat = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    mesure = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    photo_ref = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    realisee_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visites_etapes", x => x.id);
                    table.ForeignKey(
                        name: "fk_visites_etapes_visites_essieu_visite_id",
                        column: x => x.visite_id,
                        principalTable: "visites_essieu",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_visites_essieu_cloture_at",
                table: "visites_essieu",
                column: "cloture_at");

            migrationBuilder.CreateIndex(
                name: "ix_visites_essieu_numero_essieu",
                table: "visites_essieu",
                column: "numero_essieu");

            migrationBuilder.CreateIndex(
                name: "ix_visites_etapes_visite_id_etape_id",
                table: "visites_etapes",
                columns: new[] { "visite_id", "etape_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "visites_etapes");

            migrationBuilder.DropTable(
                name: "visites_essieu");

            migrationBuilder.DropColumn(
                name: "expire_at",
                table: "gammes");

            migrationBuilder.DropColumn(
                name: "source_document",
                table: "gammes");

            migrationBuilder.DropColumn(
                name: "statut",
                table: "gammes");

            migrationBuilder.DropColumn(
                name: "valide_at",
                table: "gammes");

            migrationBuilder.DropColumn(
                name: "valide_par",
                table: "gammes");
        }
    }
}
