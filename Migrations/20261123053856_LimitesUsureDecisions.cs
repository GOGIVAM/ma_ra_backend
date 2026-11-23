using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaRa.Api.Migrations
{
    /// <inheritdoc />
    public partial class LimitesUsureDecisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "hors_limites",
                table: "visites_etapes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "limite_max",
                table: "visites_etapes",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "limite_min",
                table: "visites_etapes",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "niveau_securite",
                table: "visites_etapes",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "unite",
                table: "visites_etapes",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "valeur",
                table: "visites_etapes",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gamme_instantane",
                table: "visites_essieu",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "version_conforme",
                table: "visites_essieu",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "limite_max",
                table: "etapes",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "limite_min",
                table: "etapes",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "unite",
                table: "etapes",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "decisions_essieu",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_essieu = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    visite_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    motif = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    decideur_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    decideur = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    decide_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_decisions_essieu", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_decisions_essieu_numero_essieu_decide_at",
                table: "decisions_essieu",
                columns: new[] { "numero_essieu", "decide_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "decisions_essieu");

            migrationBuilder.DropColumn(
                name: "hors_limites",
                table: "visites_etapes");

            migrationBuilder.DropColumn(
                name: "limite_max",
                table: "visites_etapes");

            migrationBuilder.DropColumn(
                name: "limite_min",
                table: "visites_etapes");

            migrationBuilder.DropColumn(
                name: "niveau_securite",
                table: "visites_etapes");

            migrationBuilder.DropColumn(
                name: "unite",
                table: "visites_etapes");

            migrationBuilder.DropColumn(
                name: "valeur",
                table: "visites_etapes");

            migrationBuilder.DropColumn(
                name: "gamme_instantane",
                table: "visites_essieu");

            migrationBuilder.DropColumn(
                name: "version_conforme",
                table: "visites_essieu");

            migrationBuilder.DropColumn(
                name: "limite_max",
                table: "etapes");

            migrationBuilder.DropColumn(
                name: "limite_min",
                table: "etapes");

            migrationBuilder.DropColumn(
                name: "unite",
                table: "etapes");
        }
    }
}
