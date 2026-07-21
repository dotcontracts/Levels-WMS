using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace wms_android.api.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveredAtAndTrackingNavProps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("2c0bcd1b-1f19-4e71-8e38-c99500e21ec6"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("6df25cda-ced8-4e10-8b48-980af0cd0565"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("7eeda2bb-6b9f-4e63-b772-bce08e8b883b"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("89712a85-3ce4-4044-95e8-c8df9662fc1e"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("95fc4e0a-f45e-4afd-a738-3f2506b493a0"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("9ba33fa5-1773-4cf1-9894-5d64c31d7910"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("b15ea148-6b3f-4061-b049-f9ef33523750"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("ca03f44b-b77f-49cf-9ce9-a7fa8aa37aee"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("d513c897-3a41-4782-8be9-aa50fa38259e"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("efce749a-5292-4596-8443-0fe363f4faa7"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("02f2ed27-c8bb-45fe-88d4-3d492eb8f402"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("055b1412-f96f-4357-ba74-e44423c3dc0d"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("9f2ab65c-2e4b-413f-ab41-f8eaa8a07dbd"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("a2c426be-4da7-4292-9d92-422e96e9b32f"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("c48cabd1-556b-48c9-8915-6cc75b7761ed"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("cb7fedc8-4ce8-4736-b76b-d7c1c64f2940"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("ea8f57e6-7928-4057-a859-dfe9ff109d7c"));

            migrationBuilder.Sql(@"ALTER TABLE ""Parcels"" ADD COLUMN IF NOT EXISTS ""DeliveredAt"" timestamp without time zone;");
            migrationBuilder.Sql(@"ALTER TABLE ""Parcels"" ADD COLUMN IF NOT EXISTS ""DeliveredById"" integer;");
            migrationBuilder.Sql(@"ALTER TABLE ""Parcels"" ADD COLUMN IF NOT EXISTS ""DispatchedById"" integer;");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Parcels_DeliveredById"" ON ""Parcels"" (""DeliveredById"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Parcels_DispatchedById"" ON ""Parcels"" (""DispatchedById"");");
            migrationBuilder.Sql(@"ALTER TABLE ""Parcels"" DROP CONSTRAINT IF EXISTS ""FK_Parcels_Users_DeliveredById""; ALTER TABLE ""Parcels"" ADD CONSTRAINT ""FK_Parcels_Users_DeliveredById"" FOREIGN KEY (""DeliveredById"") REFERENCES ""Users""(""Id"") ON DELETE SET NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""Parcels"" DROP CONSTRAINT IF EXISTS ""FK_Parcels_Users_DispatchedById""; ALTER TABLE ""Parcels"" ADD CONSTRAINT ""FK_Parcels_Users_DispatchedById"" FOREIGN KEY (""DispatchedById"") REFERENCES ""Users""(""Id"") ON DELETE SET NULL;");

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "FirstName", "Initials", "LastName", "LicenseNumber" },
                values: new object[,]
                {
                    { new Guid("0fd8ef93-a45e-453e-a624-ac4b232a068a"), "Erastus", "EK", "Kagwa", "DL123456" },
                    { new Guid("11bb5d8a-78e2-4d68-86d4-41f46e314226"), "Stephen", "SK", "Kimuyu", "DL123456" },
                    { new Guid("2f1c3599-3183-433f-83ad-60785246a895"), "Charles", "CM", "Maina", "DL123456" },
                    { new Guid("47d47543-23fe-4ff2-98c4-9a55aa2b11a0"), "David", "DK", "Kibet", "DL123456" },
                    { new Guid("9a849f84-3453-45fd-8331-a93722b1ec5b"), "James", "JG", "Gichohi", "DL123456" },
                    { new Guid("a2fd24cc-cc28-419f-90a7-3ac600469266"), "Robert", "RN", "Njuguna", "DL123456" },
                    { new Guid("b01408fa-52ad-4315-891f-f4d7541d19ce"), "John", "JM", "Mwai", "DL123456" },
                    { new Guid("bc4551e5-0c35-45c3-82f3-2606f63382f1"), "John", "JN", "Njuguna", "DL123456" },
                    { new Guid("c180a319-6268-4ef0-90d6-9bf657b34c58"), "David", "DM", "Mwangi", "DL123456" },
                    { new Guid("fc15b028-4e94-4f1a-afef-bed8e5883d33"), "Julius", "JK", "Kamula", "DL123456" }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 80, 248, 81, 67, 132, 169, 148, 101, 214, 147, 229, 58, 68, 175, 87, 72, 224, 173, 204, 75, 151, 87, 53, 228, 141, 5, 86, 81, 223, 44, 1, 23, 1, 25, 223, 143, 238, 242, 202, 187, 147, 123, 152, 104, 134, 64, 172, 102, 131, 229, 231, 76, 163, 248, 53, 154, 101, 230, 114, 156, 46, 235, 149, 141 }, new byte[] { 74, 181, 206, 70, 65, 17, 42, 80, 242, 117, 44, 155, 94, 106, 244, 110, 145, 186, 113, 244, 93, 20, 5, 107, 33, 69, 46, 71, 158, 9, 150, 242, 171, 40, 225, 22, 57, 242, 102, 213, 207, 135, 130, 33, 61, 178, 236, 6, 31, 34, 100, 19, 228, 187, 191, 53, 136, 220, 238, 113, 104, 29, 252, 255, 113, 146, 176, 149, 2, 94, 165, 112, 231, 120, 250, 162, 116, 135, 150, 117, 153, 224, 22, 0, 177, 107, 171, 139, 41, 110, 109, 11, 255, 231, 75, 226, 132, 20, 211, 211, 195, 146, 235, 67, 72, 201, 64, 240, 182, 225, 229, 248, 253, 145, 84, 118, 17, 89, 80, 145, 6, 162, 121, 159, 95, 160, 17, 138 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 80, 248, 81, 67, 132, 169, 148, 101, 214, 147, 229, 58, 68, 175, 87, 72, 224, 173, 204, 75, 151, 87, 53, 228, 141, 5, 86, 81, 223, 44, 1, 23, 1, 25, 223, 143, 238, 242, 202, 187, 147, 123, 152, 104, 134, 64, 172, 102, 131, 229, 231, 76, 163, 248, 53, 154, 101, 230, 114, 156, 46, 235, 149, 141 }, new byte[] { 74, 181, 206, 70, 65, 17, 42, 80, 242, 117, 44, 155, 94, 106, 244, 110, 145, 186, 113, 244, 93, 20, 5, 107, 33, 69, 46, 71, 158, 9, 150, 242, 171, 40, 225, 22, 57, 242, 102, 213, 207, 135, 130, 33, 61, 178, 236, 6, 31, 34, 100, 19, 228, 187, 191, 53, 136, 220, 238, 113, 104, 29, 252, 255, 113, 146, 176, 149, 2, 94, 165, 112, 231, 120, 250, 162, 116, 135, 150, 117, 153, 224, 22, 0, 177, 107, 171, 139, 41, 110, 109, 11, 255, 231, 75, 226, 132, 20, 211, 211, 195, 146, 235, 67, 72, 201, 64, 240, 182, 225, 229, 248, 253, 145, 84, 118, 17, 89, 80, 145, 6, 162, 121, 159, 95, 160, 17, 138 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 80, 248, 81, 67, 132, 169, 148, 101, 214, 147, 229, 58, 68, 175, 87, 72, 224, 173, 204, 75, 151, 87, 53, 228, 141, 5, 86, 81, 223, 44, 1, 23, 1, 25, 223, 143, 238, 242, 202, 187, 147, 123, 152, 104, 134, 64, 172, 102, 131, 229, 231, 76, 163, 248, 53, 154, 101, 230, 114, 156, 46, 235, 149, 141 }, new byte[] { 74, 181, 206, 70, 65, 17, 42, 80, 242, 117, 44, 155, 94, 106, 244, 110, 145, 186, 113, 244, 93, 20, 5, 107, 33, 69, 46, 71, 158, 9, 150, 242, 171, 40, 225, 22, 57, 242, 102, 213, 207, 135, 130, 33, 61, 178, 236, 6, 31, 34, 100, 19, 228, 187, 191, 53, 136, 220, 238, 113, 104, 29, 252, 255, 113, 146, 176, 149, 2, 94, 165, 112, 231, 120, 250, 162, 116, 135, 150, 117, 153, 224, 22, 0, 177, 107, 171, 139, 41, 110, 109, 11, 255, 231, 75, 226, 132, 20, 211, 211, 195, 146, 235, 67, 72, 201, 64, 240, 182, 225, 229, 248, 253, 145, 84, 118, 17, 89, 80, 145, 6, 162, 121, 159, 95, 160, 17, 138 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 80, 248, 81, 67, 132, 169, 148, 101, 214, 147, 229, 58, 68, 175, 87, 72, 224, 173, 204, 75, 151, 87, 53, 228, 141, 5, 86, 81, 223, 44, 1, 23, 1, 25, 223, 143, 238, 242, 202, 187, 147, 123, 152, 104, 134, 64, 172, 102, 131, 229, 231, 76, 163, 248, 53, 154, 101, 230, 114, 156, 46, 235, 149, 141 }, new byte[] { 74, 181, 206, 70, 65, 17, 42, 80, 242, 117, 44, 155, 94, 106, 244, 110, 145, 186, 113, 244, 93, 20, 5, 107, 33, 69, 46, 71, 158, 9, 150, 242, 171, 40, 225, 22, 57, 242, 102, 213, 207, 135, 130, 33, 61, 178, 236, 6, 31, 34, 100, 19, 228, 187, 191, 53, 136, 220, 238, 113, 104, 29, 252, 255, 113, 146, 176, 149, 2, 94, 165, 112, 231, 120, 250, 162, 116, 135, 150, 117, 153, 224, 22, 0, 177, 107, 171, 139, 41, 110, 109, 11, 255, 231, 75, 226, 132, 20, 211, 211, 195, 146, 235, 67, 72, 201, 64, 240, 182, 225, 229, 248, 253, 145, 84, 118, 17, 89, 80, 145, 6, 162, 121, 159, 95, 160, 17, 138 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 80, 248, 81, 67, 132, 169, 148, 101, 214, 147, 229, 58, 68, 175, 87, 72, 224, 173, 204, 75, 151, 87, 53, 228, 141, 5, 86, 81, 223, 44, 1, 23, 1, 25, 223, 143, 238, 242, 202, 187, 147, 123, 152, 104, 134, 64, 172, 102, 131, 229, 231, 76, 163, 248, 53, 154, 101, 230, 114, 156, 46, 235, 149, 141 }, new byte[] { 74, 181, 206, 70, 65, 17, 42, 80, 242, 117, 44, 155, 94, 106, 244, 110, 145, 186, 113, 244, 93, 20, 5, 107, 33, 69, 46, 71, 158, 9, 150, 242, 171, 40, 225, 22, 57, 242, 102, 213, 207, 135, 130, 33, 61, 178, 236, 6, 31, 34, 100, 19, 228, 187, 191, 53, 136, 220, 238, 113, 104, 29, 252, 255, 113, 146, 176, 149, 2, 94, 165, 112, 231, 120, 250, 162, 116, 135, 150, 117, 153, 224, 22, 0, 177, 107, 171, 139, 41, 110, 109, 11, 255, 231, 75, 226, 132, 20, 211, 211, 195, 146, 235, 67, 72, 201, 64, 240, 182, 225, 229, 248, 253, 145, 84, 118, 17, 89, 80, 145, 6, 162, 121, 159, 95, 160, 17, 138 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 80, 248, 81, 67, 132, 169, 148, 101, 214, 147, 229, 58, 68, 175, 87, 72, 224, 173, 204, 75, 151, 87, 53, 228, 141, 5, 86, 81, 223, 44, 1, 23, 1, 25, 223, 143, 238, 242, 202, 187, 147, 123, 152, 104, 134, 64, 172, 102, 131, 229, 231, 76, 163, 248, 53, 154, 101, 230, 114, 156, 46, 235, 149, 141 }, new byte[] { 74, 181, 206, 70, 65, 17, 42, 80, 242, 117, 44, 155, 94, 106, 244, 110, 145, 186, 113, 244, 93, 20, 5, 107, 33, 69, 46, 71, 158, 9, 150, 242, 171, 40, 225, 22, 57, 242, 102, 213, 207, 135, 130, 33, 61, 178, 236, 6, 31, 34, 100, 19, 228, 187, 191, 53, 136, 220, 238, 113, 104, 29, 252, 255, 113, 146, 176, 149, 2, 94, 165, 112, 231, 120, 250, 162, 116, 135, 150, 117, 153, 224, 22, 0, 177, 107, 171, 139, 41, 110, 109, 11, 255, 231, 75, 226, 132, 20, 211, 211, 195, 146, 235, 67, 72, 201, 64, 240, 182, 225, 229, 248, 253, 145, 84, 118, 17, 89, 80, 145, 6, 162, 121, 159, 95, 160, 17, 138 } });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "BodyType", "VehicleRegistrationNumber" },
                values: new object[,]
                {
                    { new Guid("842bba00-ab3a-4733-849f-9c05ba440f36"), "Truck", "KCZ 595L" },
                    { new Guid("899d34f3-bd37-478f-89a7-a93436fa6856"), "Truck", "KDL 085M" },
                    { new Guid("c7cbb8de-8af1-4eee-b123-c255934b10fc"), "Truck", "KCY 067A" },
                    { new Guid("c8d9ca95-6544-450e-bfde-34985e605fae"), "Van", "KAY 215H" },
                    { new Guid("dd183a50-9614-480a-ab2e-7e6260aa2324"), "Truck", "KDB 387Q" },
                    { new Guid("f488a9ce-4823-4582-8065-3a8110bc80e7"), "Truck", "KDE 228S" },
                    { new Guid("fde2526f-f5ed-417b-82c1-17565dbf983e"), "Van", "KBF 462A" }
                });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parcels_Users_DeliveredById",
                table: "Parcels");

            migrationBuilder.DropForeignKey(
                name: "FK_Parcels_Users_DispatchedById",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_DeliveredById",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_DispatchedById",
                table: "Parcels");

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("0fd8ef93-a45e-453e-a624-ac4b232a068a"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("11bb5d8a-78e2-4d68-86d4-41f46e314226"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("2f1c3599-3183-433f-83ad-60785246a895"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("47d47543-23fe-4ff2-98c4-9a55aa2b11a0"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("9a849f84-3453-45fd-8331-a93722b1ec5b"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("a2fd24cc-cc28-419f-90a7-3ac600469266"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("b01408fa-52ad-4315-891f-f4d7541d19ce"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("bc4551e5-0c35-45c3-82f3-2606f63382f1"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("c180a319-6268-4ef0-90d6-9bf657b34c58"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("fc15b028-4e94-4f1a-afef-bed8e5883d33"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("842bba00-ab3a-4733-849f-9c05ba440f36"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("899d34f3-bd37-478f-89a7-a93436fa6856"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("c7cbb8de-8af1-4eee-b123-c255934b10fc"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("c8d9ca95-6544-450e-bfde-34985e605fae"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("dd183a50-9614-480a-ab2e-7e6260aa2324"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("f488a9ce-4823-4582-8065-3a8110bc80e7"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("fde2526f-f5ed-417b-82c1-17565dbf983e"));

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "DeliveredById",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "DispatchedById",
                table: "Parcels");

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "FirstName", "Initials", "LastName", "LicenseNumber" },
                values: new object[,]
                {
                    { new Guid("2c0bcd1b-1f19-4e71-8e38-c99500e21ec6"), "David", "DK", "Kibet", "DL123456" },
                    { new Guid("6df25cda-ced8-4e10-8b48-980af0cd0565"), "James", "JG", "Gichohi", "DL123456" },
                    { new Guid("7eeda2bb-6b9f-4e63-b772-bce08e8b883b"), "Stephen", "SK", "Kimuyu", "DL123456" },
                    { new Guid("89712a85-3ce4-4044-95e8-c8df9662fc1e"), "John", "JN", "Njuguna", "DL123456" },
                    { new Guid("95fc4e0a-f45e-4afd-a738-3f2506b493a0"), "David", "DM", "Mwangi", "DL123456" },
                    { new Guid("9ba33fa5-1773-4cf1-9894-5d64c31d7910"), "Julius", "JK", "Kamula", "DL123456" },
                    { new Guid("b15ea148-6b3f-4061-b049-f9ef33523750"), "Charles", "CM", "Maina", "DL123456" },
                    { new Guid("ca03f44b-b77f-49cf-9ce9-a7fa8aa37aee"), "Erastus", "EK", "Kagwa", "DL123456" },
                    { new Guid("d513c897-3a41-4782-8be9-aa50fa38259e"), "John", "JM", "Mwai", "DL123456" },
                    { new Guid("efce749a-5292-4596-8443-0fe363f4faa7"), "Robert", "RN", "Njuguna", "DL123456" }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 195, 63, 107, 145, 247, 146, 130, 55, 84, 211, 101, 206, 74, 56, 190, 161, 89, 212, 199, 94, 213, 82, 154, 8, 229, 195, 103, 81, 10, 42, 206, 137, 139, 3, 232, 29, 22, 110, 74, 30, 38, 139, 29, 23, 176, 191, 16, 97, 30, 2, 255, 114, 111, 169, 120, 119, 229, 253, 37, 126, 149, 164, 221, 6 }, new byte[] { 117, 74, 220, 225, 122, 223, 184, 119, 124, 245, 186, 3, 3, 228, 183, 71, 67, 91, 129, 251, 122, 202, 195, 5, 128, 246, 95, 36, 54, 77, 38, 88, 250, 205, 72, 235, 138, 107, 218, 220, 114, 29, 79, 225, 85, 223, 59, 184, 159, 214, 155, 47, 192, 4, 194, 210, 147, 131, 34, 48, 158, 158, 176, 87, 27, 168, 207, 24, 80, 189, 186, 82, 51, 96, 181, 216, 172, 47, 161, 236, 248, 252, 245, 26, 18, 194, 132, 144, 63, 160, 20, 155, 48, 85, 226, 183, 242, 58, 216, 180, 180, 6, 21, 104, 74, 80, 95, 12, 1, 129, 235, 238, 85, 54, 194, 4, 202, 223, 228, 124, 212, 181, 12, 11, 53, 93, 215, 98 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 195, 63, 107, 145, 247, 146, 130, 55, 84, 211, 101, 206, 74, 56, 190, 161, 89, 212, 199, 94, 213, 82, 154, 8, 229, 195, 103, 81, 10, 42, 206, 137, 139, 3, 232, 29, 22, 110, 74, 30, 38, 139, 29, 23, 176, 191, 16, 97, 30, 2, 255, 114, 111, 169, 120, 119, 229, 253, 37, 126, 149, 164, 221, 6 }, new byte[] { 117, 74, 220, 225, 122, 223, 184, 119, 124, 245, 186, 3, 3, 228, 183, 71, 67, 91, 129, 251, 122, 202, 195, 5, 128, 246, 95, 36, 54, 77, 38, 88, 250, 205, 72, 235, 138, 107, 218, 220, 114, 29, 79, 225, 85, 223, 59, 184, 159, 214, 155, 47, 192, 4, 194, 210, 147, 131, 34, 48, 158, 158, 176, 87, 27, 168, 207, 24, 80, 189, 186, 82, 51, 96, 181, 216, 172, 47, 161, 236, 248, 252, 245, 26, 18, 194, 132, 144, 63, 160, 20, 155, 48, 85, 226, 183, 242, 58, 216, 180, 180, 6, 21, 104, 74, 80, 95, 12, 1, 129, 235, 238, 85, 54, 194, 4, 202, 223, 228, 124, 212, 181, 12, 11, 53, 93, 215, 98 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 195, 63, 107, 145, 247, 146, 130, 55, 84, 211, 101, 206, 74, 56, 190, 161, 89, 212, 199, 94, 213, 82, 154, 8, 229, 195, 103, 81, 10, 42, 206, 137, 139, 3, 232, 29, 22, 110, 74, 30, 38, 139, 29, 23, 176, 191, 16, 97, 30, 2, 255, 114, 111, 169, 120, 119, 229, 253, 37, 126, 149, 164, 221, 6 }, new byte[] { 117, 74, 220, 225, 122, 223, 184, 119, 124, 245, 186, 3, 3, 228, 183, 71, 67, 91, 129, 251, 122, 202, 195, 5, 128, 246, 95, 36, 54, 77, 38, 88, 250, 205, 72, 235, 138, 107, 218, 220, 114, 29, 79, 225, 85, 223, 59, 184, 159, 214, 155, 47, 192, 4, 194, 210, 147, 131, 34, 48, 158, 158, 176, 87, 27, 168, 207, 24, 80, 189, 186, 82, 51, 96, 181, 216, 172, 47, 161, 236, 248, 252, 245, 26, 18, 194, 132, 144, 63, 160, 20, 155, 48, 85, 226, 183, 242, 58, 216, 180, 180, 6, 21, 104, 74, 80, 95, 12, 1, 129, 235, 238, 85, 54, 194, 4, 202, 223, 228, 124, 212, 181, 12, 11, 53, 93, 215, 98 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 195, 63, 107, 145, 247, 146, 130, 55, 84, 211, 101, 206, 74, 56, 190, 161, 89, 212, 199, 94, 213, 82, 154, 8, 229, 195, 103, 81, 10, 42, 206, 137, 139, 3, 232, 29, 22, 110, 74, 30, 38, 139, 29, 23, 176, 191, 16, 97, 30, 2, 255, 114, 111, 169, 120, 119, 229, 253, 37, 126, 149, 164, 221, 6 }, new byte[] { 117, 74, 220, 225, 122, 223, 184, 119, 124, 245, 186, 3, 3, 228, 183, 71, 67, 91, 129, 251, 122, 202, 195, 5, 128, 246, 95, 36, 54, 77, 38, 88, 250, 205, 72, 235, 138, 107, 218, 220, 114, 29, 79, 225, 85, 223, 59, 184, 159, 214, 155, 47, 192, 4, 194, 210, 147, 131, 34, 48, 158, 158, 176, 87, 27, 168, 207, 24, 80, 189, 186, 82, 51, 96, 181, 216, 172, 47, 161, 236, 248, 252, 245, 26, 18, 194, 132, 144, 63, 160, 20, 155, 48, 85, 226, 183, 242, 58, 216, 180, 180, 6, 21, 104, 74, 80, 95, 12, 1, 129, 235, 238, 85, 54, 194, 4, 202, 223, 228, 124, 212, 181, 12, 11, 53, 93, 215, 98 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 195, 63, 107, 145, 247, 146, 130, 55, 84, 211, 101, 206, 74, 56, 190, 161, 89, 212, 199, 94, 213, 82, 154, 8, 229, 195, 103, 81, 10, 42, 206, 137, 139, 3, 232, 29, 22, 110, 74, 30, 38, 139, 29, 23, 176, 191, 16, 97, 30, 2, 255, 114, 111, 169, 120, 119, 229, 253, 37, 126, 149, 164, 221, 6 }, new byte[] { 117, 74, 220, 225, 122, 223, 184, 119, 124, 245, 186, 3, 3, 228, 183, 71, 67, 91, 129, 251, 122, 202, 195, 5, 128, 246, 95, 36, 54, 77, 38, 88, 250, 205, 72, 235, 138, 107, 218, 220, 114, 29, 79, 225, 85, 223, 59, 184, 159, 214, 155, 47, 192, 4, 194, 210, 147, 131, 34, 48, 158, 158, 176, 87, 27, 168, 207, 24, 80, 189, 186, 82, 51, 96, 181, 216, 172, 47, 161, 236, 248, 252, 245, 26, 18, 194, 132, 144, 63, 160, 20, 155, 48, 85, 226, 183, 242, 58, 216, 180, 180, 6, 21, 104, 74, 80, 95, 12, 1, 129, 235, 238, 85, 54, 194, 4, 202, 223, 228, 124, 212, 181, 12, 11, 53, 93, 215, 98 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 195, 63, 107, 145, 247, 146, 130, 55, 84, 211, 101, 206, 74, 56, 190, 161, 89, 212, 199, 94, 213, 82, 154, 8, 229, 195, 103, 81, 10, 42, 206, 137, 139, 3, 232, 29, 22, 110, 74, 30, 38, 139, 29, 23, 176, 191, 16, 97, 30, 2, 255, 114, 111, 169, 120, 119, 229, 253, 37, 126, 149, 164, 221, 6 }, new byte[] { 117, 74, 220, 225, 122, 223, 184, 119, 124, 245, 186, 3, 3, 228, 183, 71, 67, 91, 129, 251, 122, 202, 195, 5, 128, 246, 95, 36, 54, 77, 38, 88, 250, 205, 72, 235, 138, 107, 218, 220, 114, 29, 79, 225, 85, 223, 59, 184, 159, 214, 155, 47, 192, 4, 194, 210, 147, 131, 34, 48, 158, 158, 176, 87, 27, 168, 207, 24, 80, 189, 186, 82, 51, 96, 181, 216, 172, 47, 161, 236, 248, 252, 245, 26, 18, 194, 132, 144, 63, 160, 20, 155, 48, 85, 226, 183, 242, 58, 216, 180, 180, 6, 21, 104, 74, 80, 95, 12, 1, 129, 235, 238, 85, 54, 194, 4, 202, 223, 228, 124, 212, 181, 12, 11, 53, 93, 215, 98 } });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "BodyType", "VehicleRegistrationNumber" },
                values: new object[,]
                {
                    { new Guid("02f2ed27-c8bb-45fe-88d4-3d492eb8f402"), "Truck", "KDB 387Q" },
                    { new Guid("055b1412-f96f-4357-ba74-e44423c3dc0d"), "Van", "KAY 215H" },
                    { new Guid("9f2ab65c-2e4b-413f-ab41-f8eaa8a07dbd"), "Van", "KBF 462A" },
                    { new Guid("a2c426be-4da7-4292-9d92-422e96e9b32f"), "Truck", "KCZ 595L" },
                    { new Guid("c48cabd1-556b-48c9-8915-6cc75b7761ed"), "Truck", "KDL 085M" },
                    { new Guid("cb7fedc8-4ce8-4736-b76b-d7c1c64f2940"), "Truck", "KDE 228S" },
                    { new Guid("ea8f57e6-7928-4057-a859-dfe9ff109d7c"), "Truck", "KCY 067A" }
                });
        }
    }
}
