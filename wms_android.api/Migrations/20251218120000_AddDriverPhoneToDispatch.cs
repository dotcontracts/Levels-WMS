using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wms_android.api.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverPhoneToDispatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if column exists before adding it
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Dispatches' 
                        AND column_name = 'DriverPhone'
                    ) THEN
                        ALTER TABLE ""Dispatches"" ADD COLUMN ""DriverPhone"" text;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Check if column exists before dropping it
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Dispatches' 
                        AND column_name = 'DriverPhone'
                    ) THEN
                        ALTER TABLE ""Dispatches"" DROP COLUMN ""DriverPhone"";
                    END IF;
                END $$;
            ");
        }
    }
}