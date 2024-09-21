using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicalOffice.Data.MOMigrations
{
    /// <inheritdoc />
    public partial class AddedMedicalTrial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MedicalTrialID",
                table: "Patients",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MedicalTrials",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrialName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalTrials", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_DOB_LastName_FirstName",
                table: "Patients",
                columns: new[] { "DOB", "LastName", "FirstName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_MedicalTrialID",
                table: "Patients",
                column: "MedicalTrialID");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_MedicalTrials_MedicalTrialID",
                table: "Patients",
                column: "MedicalTrialID",
                principalTable: "MedicalTrials",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_MedicalTrials_MedicalTrialID",
                table: "Patients");

            migrationBuilder.DropTable(
                name: "MedicalTrials");

            migrationBuilder.DropIndex(
                name: "IX_Patients_DOB_LastName_FirstName",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_MedicalTrialID",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "MedicalTrialID",
                table: "Patients");
        }
    }
}
