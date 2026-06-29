using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkAuditLogToAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // app_user_id historically stored the Supabase auth user id (= app_users.auth_user_id),
            // not app_users.id. Convert existing rows to the matching app_users.id before adding the FK.
            migrationBuilder.Sql(@"
                UPDATE audit_logs a
                SET app_user_id = u.id
                FROM app_users u
                WHERE a.app_user_id = u.auth_user_id;");

            // Null out any value that still doesn't reference a real app_users.id (orphaned auth ids).
            migrationBuilder.Sql(@"
                UPDATE audit_logs
                SET app_user_id = NULL
                WHERE app_user_id IS NOT NULL
                  AND app_user_id NOT IN (SELECT id FROM app_users);");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_app_user_id",
                table: "audit_logs",
                column: "app_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_audit_logs_app_users_app_user_id",
                table: "audit_logs",
                column: "app_user_id",
                principalTable: "app_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_audit_logs_app_users_app_user_id",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_app_user_id",
                table: "audit_logs");
        }
    }
}
