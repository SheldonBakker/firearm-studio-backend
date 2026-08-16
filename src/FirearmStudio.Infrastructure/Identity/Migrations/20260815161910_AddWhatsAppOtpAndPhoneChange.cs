using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsAppOtpAndPhoneChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:public.otp_purpose", "email_confirmation,invite,password_reset,phone_change,two_factor")
                .OldAnnotation("Npgsql:Enum:public.otp_purpose", "email_confirmation,invite,password_reset");

            migrationBuilder.AddColumn<string>(
                name: "pending_phone_number",
                schema: "identity",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pending_phone_number",
                schema: "identity",
                table: "users");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:public.otp_purpose", "email_confirmation,invite,password_reset")
                .OldAnnotation("Npgsql:Enum:public.otp_purpose", "email_confirmation,invite,password_reset,phone_change,two_factor");
        }
    }
}
