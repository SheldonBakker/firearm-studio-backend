using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Identity.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWhatsAppOtpAndPhoneChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pending_phone_number",
                schema: "identity",
                table: "users");

            migrationBuilder.Sql("""
                DELETE FROM identity.otp_codes WHERE purpose = 'phone_change';
                ALTER TYPE public.otp_purpose RENAME TO otp_purpose_old;
                CREATE TYPE public.otp_purpose AS ENUM ('email_confirmation', 'invite', 'password_reset', 'two_factor');
                ALTER TABLE identity.otp_codes
                    ALTER COLUMN purpose TYPE public.otp_purpose
                    USING purpose::text::public.otp_purpose;
                DROP TYPE public.otp_purpose_old;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TYPE public.otp_purpose RENAME TO otp_purpose_old;
                CREATE TYPE public.otp_purpose AS ENUM ('email_confirmation', 'invite', 'password_reset', 'phone_change', 'two_factor');
                ALTER TABLE identity.otp_codes
                    ALTER COLUMN purpose TYPE public.otp_purpose
                    USING purpose::text::public.otp_purpose;
                DROP TYPE public.otp_purpose_old;
                """);

            migrationBuilder.AddColumn<string>(
                name: "pending_phone_number",
                schema: "identity",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
