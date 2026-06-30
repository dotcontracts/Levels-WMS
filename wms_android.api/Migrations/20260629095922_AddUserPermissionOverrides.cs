using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace wms_android.api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPermissionOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("0618ed00-dca0-47ed-b4cd-c65301eb64a2"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("1507290d-18e4-4ed9-bac5-0217f351f58a"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("1f1d55fe-5d56-4779-aed4-f5b05a1e95df"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("3b45e80f-7d87-4b67-81e7-7c66571a3347"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("5280e83f-c600-4de0-8b23-f457b36fec81"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("53751b4a-7551-4b6d-824d-f372e90595ae"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("6eb4a017-7591-463e-b7d1-865ec9001223"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("778f0188-44f8-4c97-9f5d-1539d8f49c53"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("89181e2d-a072-409c-add3-9c1afc424a37"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("caecb4f6-a3b5-41a3-adec-48d8a4d25ae0"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("103cc7fc-5ee5-4550-8054-7c39508e205a"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("21e3faeb-c997-42b5-877c-94849b7e60ad"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("33550007-7d8a-4ef3-b2ff-f5ac03722d1a"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("41eb4491-48f3-4000-b225-be5726589178"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("514eabce-b25b-435a-b392-da82132e22de"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("557c3972-9d58-42f3-a37f-34cf50becfbe"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("f75fc171-d918-425f-9ce4-4eb72f3193d0"));

            migrationBuilder.AddColumn<bool>(
                name: "CanConfirm",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanDispatch",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CanConfirm", "CanDispatch" },
                values: new object[] { false, false });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CanConfirm", "CanDispatch" },
                values: new object[] { false, false });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CanConfirm", "CanDispatch" },
                values: new object[] { false, false });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CanConfirm", "CanDispatch" },
                values: new object[] { false, false });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CanConfirm", "CanDispatch", "PasswordHash", "PasswordSalt" },
                values: new object[] { false, false, new byte[] { 195, 63, 107, 145, 247, 146, 130, 55, 84, 211, 101, 206, 74, 56, 190, 161, 89, 212, 199, 94, 213, 82, 154, 8, 229, 195, 103, 81, 10, 42, 206, 137, 139, 3, 232, 29, 22, 110, 74, 30, 38, 139, 29, 23, 176, 191, 16, 97, 30, 2, 255, 114, 111, 169, 120, 119, 229, 253, 37, 126, 149, 164, 221, 6 }, new byte[] { 117, 74, 220, 225, 122, 223, 184, 119, 124, 245, 186, 3, 3, 228, 183, 71, 67, 91, 129, 251, 122, 202, 195, 5, 128, 246, 95, 36, 54, 77, 38, 88, 250, 205, 72, 235, 138, 107, 218, 220, 114, 29, 79, 225, 85, 223, 59, 184, 159, 214, 155, 47, 192, 4, 194, 210, 147, 131, 34, 48, 158, 158, 176, 87, 27, 168, 207, 24, 80, 189, 186, 82, 51, 96, 181, 216, 172, 47, 161, 236, 248, 252, 245, 26, 18, 194, 132, 144, 63, 160, 20, 155, 48, 85, 226, 183, 242, 58, 216, 180, 180, 6, 21, 104, 74, 80, 95, 12, 1, 129, 235, 238, 85, 54, 194, 4, 202, 223, 228, 124, 212, 181, 12, 11, 53, 93, 215, 98 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CanConfirm", "CanDispatch", "PasswordHash", "PasswordSalt" },
                values: new object[] { false, false, new byte[] { 195, 63, 107, 145, 247, 146, 130, 55, 84, 211, 101, 206, 74, 56, 190, 161, 89, 212, 199, 94, 213, 82, 154, 8, 229, 195, 103, 81, 10, 42, 206, 137, 139, 3, 232, 29, 22, 110, 74, 30, 38, 139, 29, 23, 176, 191, 16, 97, 30, 2, 255, 114, 111, 169, 120, 119, 229, 253, 37, 126, 149, 164, 221, 6 }, new byte[] { 117, 74, 220, 225, 122, 223, 184, 119, 124, 245, 186, 3, 3, 228, 183, 71, 67, 91, 129, 251, 122, 202, 195, 5, 128, 246, 95, 36, 54, 77, 38, 88, 250, 205, 72, 235, 138, 107, 218, 220, 114, 29, 79, 225, 85, 223, 59, 184, 159, 214, 155, 47, 192, 4, 194, 210, 147, 131, 34, 48, 158, 158, 176, 87, 27, 168, 207, 24, 80, 189, 186, 82, 51, 96, 181, 216, 172, 47, 161, 236, 248, 252, 245, 26, 18, 194, 132, 144, 63, 160, 20, 155, 48, 85, 226, 183, 242, 58, 216, 180, 180, 6, 21, 104, 74, 80, 95, 12, 1, 129, 235, 238, 85, 54, 194, 4, 202, 223, 228, 124, 212, 181, 12, 11, 53, 93, 215, 98 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CanConfirm", "CanDispatch", "PasswordHash", "PasswordSalt" },
                values: new object[] { false, false, new byte[] { 195, 63, 107, 145, 247, 146, 130, 55, 84, 211, 101, 206, 74, 56, 190, 161, 89, 212, 199, 94, 213, 82, 154, 8, 229, 195, 103, 81, 10, 42, 206, 137, 139, 3, 232, 29, 22, 110, 74, 30, 38, 139, 29, 23, 176, 191, 16, 97, 30, 2, 255, 114, 111, 169, 120, 119, 229, 253, 37, 126, 149, 164, 221, 6 }, new byte[] { 117, 74, 220, 225, 122, 223, 184, 119, 124, 245, 186, 3, 3, 228, 183, 71, 67, 91, 129, 251, 122, 202, 195, 5, 128, 246, 95, 36, 54, 77, 38, 88, 250, 205, 72, 235, 138, 107, 218, 220, 114, 29, 79, 225, 85, 223, 59, 184, 159, 214, 155, 47, 192, 4, 194, 210, 147, 131, 34, 48, 158, 158, 176, 87, 27, 168, 207, 24, 80, 189, 186, 82, 51, 96, 181, 216, 172, 47, 161, 236, 248, 252, 245, 26, 18, 194, 132, 144, 63, 160, 20, 155, 48, 85, 226, 183, 242, 58, 216, 180, 180, 6, 21, 104, 74, 80, 95, 12, 1, 129, 235, 238, 85, 54, 194, 4, 202, 223, 228, 124, 212, 181, 12, 11, 53, 93, 215, 98 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CanConfirm", "CanDispatch", "PasswordHash", "PasswordSalt" },
                values: new object[] { false, false, new byte[] { 195, 63, 107, 145, 247, 146, 130, 55, 84, 211, 101, 206, 74, 56, 190, 161, 89, 212, 199, 94, 213, 82, 154, 8, 229, 195, 103, 81, 10, 42, 206, 137, 139, 3, 232, 29, 22, 110, 74, 30, 38, 139, 29, 23, 176, 191, 16, 97, 30, 2, 255, 114, 111, 169, 120, 119, 229, 253, 37, 126, 149, 164, 221, 6 }, new byte[] { 117, 74, 220, 225, 122, 223, 184, 119, 124, 245, 186, 3, 3, 228, 183, 71, 67, 91, 129, 251, 122, 202, 195, 5, 128, 246, 95, 36, 54, 77, 38, 88, 250, 205, 72, 235, 138, 107, 218, 220, 114, 29, 79, 225, 85, 223, 59, 184, 159, 214, 155, 47, 192, 4, 194, 210, 147, 131, 34, 48, 158, 158, 176, 87, 27, 168, 207, 24, 80, 189, 186, 82, 51, 96, 181, 216, 172, 47, 161, 236, 248, 252, 245, 26, 18, 194, 132, 144, 63, 160, 20, 155, 48, 85, 226, 183, 242, 58, 216, 180, 180, 6, 21, 104, 74, 80, 95, 12, 1, 129, 235, 238, 85, 54, 194, 4, 202, 223, 228, 124, 212, 181, 12, 11, 53, 93, 215, 98 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CanConfirm", "CanDispatch", "PasswordHash", "PasswordSalt" },
                values: new object[] { false, false, new byte[] { 195, 63, 107, 145, 247, 146, 130, 55, 84, 211, 101, 206, 74, 56, 190, 161, 89, 212, 199, 94, 213, 82, 154, 8, 229, 195, 103, 81, 10, 42, 206, 137, 139, 3, 232, 29, 22, 110, 74, 30, 38, 139, 29, 23, 176, 191, 16, 97, 30, 2, 255, 114, 111, 169, 120, 119, 229, 253, 37, 126, 149, 164, 221, 6 }, new byte[] { 117, 74, 220, 225, 122, 223, 184, 119, 124, 245, 186, 3, 3, 228, 183, 71, 67, 91, 129, 251, 122, 202, 195, 5, 128, 246, 95, 36, 54, 77, 38, 88, 250, 205, 72, 235, 138, 107, 218, 220, 114, 29, 79, 225, 85, 223, 59, 184, 159, 214, 155, 47, 192, 4, 194, 210, 147, 131, 34, 48, 158, 158, 176, 87, 27, 168, 207, 24, 80, 189, 186, 82, 51, 96, 181, 216, 172, 47, 161, 236, 248, 252, 245, 26, 18, 194, 132, 144, 63, 160, 20, 155, 48, 85, 226, 183, 242, 58, 216, 180, 180, 6, 21, 104, 74, 80, 95, 12, 1, 129, 235, 238, 85, 54, 194, 4, 202, 223, 228, 124, 212, 181, 12, 11, 53, 93, 215, 98 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CanConfirm", "CanDispatch", "PasswordHash", "PasswordSalt" },
                values: new object[] { false, false, new byte[] { 195, 63, 107, 145, 247, 146, 130, 55, 84, 211, 101, 206, 74, 56, 190, 161, 89, 212, 199, 94, 213, 82, 154, 8, 229, 195, 103, 81, 10, 42, 206, 137, 139, 3, 232, 29, 22, 110, 74, 30, 38, 139, 29, 23, 176, 191, 16, 97, 30, 2, 255, 114, 111, 169, 120, 119, 229, 253, 37, 126, 149, 164, 221, 6 }, new byte[] { 117, 74, 220, 225, 122, 223, 184, 119, 124, 245, 186, 3, 3, 228, 183, 71, 67, 91, 129, 251, 122, 202, 195, 5, 128, 246, 95, 36, 54, 77, 38, 88, 250, 205, 72, 235, 138, 107, 218, 220, 114, 29, 79, 225, 85, 223, 59, 184, 159, 214, 155, 47, 192, 4, 194, 210, 147, 131, 34, 48, 158, 158, 176, 87, 27, 168, 207, 24, 80, 189, 186, 82, 51, 96, 181, 216, 172, 47, 161, 236, 248, 252, 245, 26, 18, 194, 132, 144, 63, 160, 20, 155, 48, 85, 226, 183, 242, 58, 216, 180, 180, 6, 21, 104, 74, 80, 95, 12, 1, 129, 235, 238, 85, 54, 194, 4, 202, 223, 228, 124, 212, 181, 12, 11, 53, 93, 215, 98 } });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "CanConfirm",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CanDispatch",
                table: "Users");

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "FirstName", "Initials", "LastName", "LicenseNumber" },
                values: new object[,]
                {
                    { new Guid("0618ed00-dca0-47ed-b4cd-c65301eb64a2"), "David", "DK", "Kibet", "DL123456" },
                    { new Guid("1507290d-18e4-4ed9-bac5-0217f351f58a"), "Stephen", "SK", "Kimuyu", "DL123456" },
                    { new Guid("1f1d55fe-5d56-4779-aed4-f5b05a1e95df"), "Robert", "RN", "Njuguna", "DL123456" },
                    { new Guid("3b45e80f-7d87-4b67-81e7-7c66571a3347"), "James", "JG", "Gichohi", "DL123456" },
                    { new Guid("5280e83f-c600-4de0-8b23-f457b36fec81"), "Erastus", "EK", "Kagwa", "DL123456" },
                    { new Guid("53751b4a-7551-4b6d-824d-f372e90595ae"), "John", "JM", "Mwai", "DL123456" },
                    { new Guid("6eb4a017-7591-463e-b7d1-865ec9001223"), "Charles", "CM", "Maina", "DL123456" },
                    { new Guid("778f0188-44f8-4c97-9f5d-1539d8f49c53"), "David", "DM", "Mwangi", "DL123456" },
                    { new Guid("89181e2d-a072-409c-add3-9c1afc424a37"), "Julius", "JK", "Kamula", "DL123456" },
                    { new Guid("caecb4f6-a3b5-41a3-adec-48d8a4d25ae0"), "John", "JN", "Njuguna", "DL123456" }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 31, 84, 107, 231, 78, 158, 35, 213, 98, 215, 173, 237, 245, 234, 123, 105, 171, 113, 27, 240, 229, 148, 59, 218, 203, 172, 163, 10, 43, 155, 185, 91, 215, 24, 200, 49, 26, 150, 178, 244, 224, 36, 219, 43, 91, 101, 140, 129, 103, 195, 168, 208, 248, 228, 208, 1, 174, 77, 164, 76, 130, 227, 255, 122 }, new byte[] { 57, 59, 146, 33, 134, 153, 222, 237, 224, 2, 76, 79, 14, 165, 55, 200, 154, 13, 29, 97, 243, 23, 119, 107, 89, 251, 226, 89, 1, 20, 69, 233, 35, 92, 79, 128, 33, 98, 140, 236, 241, 177, 142, 130, 163, 188, 84, 235, 26, 136, 61, 54, 67, 69, 162, 150, 100, 239, 121, 96, 106, 146, 32, 228, 202, 85, 102, 213, 7, 89, 207, 89, 226, 184, 66, 162, 114, 198, 14, 28, 242, 132, 167, 222, 55, 66, 150, 37, 48, 179, 84, 122, 10, 233, 226, 127, 108, 253, 122, 14, 154, 53, 21, 234, 47, 84, 27, 73, 54, 159, 111, 62, 241, 68, 158, 84, 207, 4, 179, 111, 115, 140, 57, 47, 105, 69, 140, 7 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 31, 84, 107, 231, 78, 158, 35, 213, 98, 215, 173, 237, 245, 234, 123, 105, 171, 113, 27, 240, 229, 148, 59, 218, 203, 172, 163, 10, 43, 155, 185, 91, 215, 24, 200, 49, 26, 150, 178, 244, 224, 36, 219, 43, 91, 101, 140, 129, 103, 195, 168, 208, 248, 228, 208, 1, 174, 77, 164, 76, 130, 227, 255, 122 }, new byte[] { 57, 59, 146, 33, 134, 153, 222, 237, 224, 2, 76, 79, 14, 165, 55, 200, 154, 13, 29, 97, 243, 23, 119, 107, 89, 251, 226, 89, 1, 20, 69, 233, 35, 92, 79, 128, 33, 98, 140, 236, 241, 177, 142, 130, 163, 188, 84, 235, 26, 136, 61, 54, 67, 69, 162, 150, 100, 239, 121, 96, 106, 146, 32, 228, 202, 85, 102, 213, 7, 89, 207, 89, 226, 184, 66, 162, 114, 198, 14, 28, 242, 132, 167, 222, 55, 66, 150, 37, 48, 179, 84, 122, 10, 233, 226, 127, 108, 253, 122, 14, 154, 53, 21, 234, 47, 84, 27, 73, 54, 159, 111, 62, 241, 68, 158, 84, 207, 4, 179, 111, 115, 140, 57, 47, 105, 69, 140, 7 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 31, 84, 107, 231, 78, 158, 35, 213, 98, 215, 173, 237, 245, 234, 123, 105, 171, 113, 27, 240, 229, 148, 59, 218, 203, 172, 163, 10, 43, 155, 185, 91, 215, 24, 200, 49, 26, 150, 178, 244, 224, 36, 219, 43, 91, 101, 140, 129, 103, 195, 168, 208, 248, 228, 208, 1, 174, 77, 164, 76, 130, 227, 255, 122 }, new byte[] { 57, 59, 146, 33, 134, 153, 222, 237, 224, 2, 76, 79, 14, 165, 55, 200, 154, 13, 29, 97, 243, 23, 119, 107, 89, 251, 226, 89, 1, 20, 69, 233, 35, 92, 79, 128, 33, 98, 140, 236, 241, 177, 142, 130, 163, 188, 84, 235, 26, 136, 61, 54, 67, 69, 162, 150, 100, 239, 121, 96, 106, 146, 32, 228, 202, 85, 102, 213, 7, 89, 207, 89, 226, 184, 66, 162, 114, 198, 14, 28, 242, 132, 167, 222, 55, 66, 150, 37, 48, 179, 84, 122, 10, 233, 226, 127, 108, 253, 122, 14, 154, 53, 21, 234, 47, 84, 27, 73, 54, 159, 111, 62, 241, 68, 158, 84, 207, 4, 179, 111, 115, 140, 57, 47, 105, 69, 140, 7 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 31, 84, 107, 231, 78, 158, 35, 213, 98, 215, 173, 237, 245, 234, 123, 105, 171, 113, 27, 240, 229, 148, 59, 218, 203, 172, 163, 10, 43, 155, 185, 91, 215, 24, 200, 49, 26, 150, 178, 244, 224, 36, 219, 43, 91, 101, 140, 129, 103, 195, 168, 208, 248, 228, 208, 1, 174, 77, 164, 76, 130, 227, 255, 122 }, new byte[] { 57, 59, 146, 33, 134, 153, 222, 237, 224, 2, 76, 79, 14, 165, 55, 200, 154, 13, 29, 97, 243, 23, 119, 107, 89, 251, 226, 89, 1, 20, 69, 233, 35, 92, 79, 128, 33, 98, 140, 236, 241, 177, 142, 130, 163, 188, 84, 235, 26, 136, 61, 54, 67, 69, 162, 150, 100, 239, 121, 96, 106, 146, 32, 228, 202, 85, 102, 213, 7, 89, 207, 89, 226, 184, 66, 162, 114, 198, 14, 28, 242, 132, 167, 222, 55, 66, 150, 37, 48, 179, 84, 122, 10, 233, 226, 127, 108, 253, 122, 14, 154, 53, 21, 234, 47, 84, 27, 73, 54, 159, 111, 62, 241, 68, 158, 84, 207, 4, 179, 111, 115, 140, 57, 47, 105, 69, 140, 7 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 31, 84, 107, 231, 78, 158, 35, 213, 98, 215, 173, 237, 245, 234, 123, 105, 171, 113, 27, 240, 229, 148, 59, 218, 203, 172, 163, 10, 43, 155, 185, 91, 215, 24, 200, 49, 26, 150, 178, 244, 224, 36, 219, 43, 91, 101, 140, 129, 103, 195, 168, 208, 248, 228, 208, 1, 174, 77, 164, 76, 130, 227, 255, 122 }, new byte[] { 57, 59, 146, 33, 134, 153, 222, 237, 224, 2, 76, 79, 14, 165, 55, 200, 154, 13, 29, 97, 243, 23, 119, 107, 89, 251, 226, 89, 1, 20, 69, 233, 35, 92, 79, 128, 33, 98, 140, 236, 241, 177, 142, 130, 163, 188, 84, 235, 26, 136, 61, 54, 67, 69, 162, 150, 100, 239, 121, 96, 106, 146, 32, 228, 202, 85, 102, 213, 7, 89, 207, 89, 226, 184, 66, 162, 114, 198, 14, 28, 242, 132, 167, 222, 55, 66, 150, 37, 48, 179, 84, 122, 10, 233, 226, 127, 108, 253, 122, 14, 154, 53, 21, 234, 47, 84, 27, 73, 54, 159, 111, 62, 241, 68, 158, 84, 207, 4, 179, 111, 115, 140, 57, 47, 105, 69, 140, 7 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 31, 84, 107, 231, 78, 158, 35, 213, 98, 215, 173, 237, 245, 234, 123, 105, 171, 113, 27, 240, 229, 148, 59, 218, 203, 172, 163, 10, 43, 155, 185, 91, 215, 24, 200, 49, 26, 150, 178, 244, 224, 36, 219, 43, 91, 101, 140, 129, 103, 195, 168, 208, 248, 228, 208, 1, 174, 77, 164, 76, 130, 227, 255, 122 }, new byte[] { 57, 59, 146, 33, 134, 153, 222, 237, 224, 2, 76, 79, 14, 165, 55, 200, 154, 13, 29, 97, 243, 23, 119, 107, 89, 251, 226, 89, 1, 20, 69, 233, 35, 92, 79, 128, 33, 98, 140, 236, 241, 177, 142, 130, 163, 188, 84, 235, 26, 136, 61, 54, 67, 69, 162, 150, 100, 239, 121, 96, 106, 146, 32, 228, 202, 85, 102, 213, 7, 89, 207, 89, 226, 184, 66, 162, 114, 198, 14, 28, 242, 132, 167, 222, 55, 66, 150, 37, 48, 179, 84, 122, 10, 233, 226, 127, 108, 253, 122, 14, 154, 53, 21, 234, 47, 84, 27, 73, 54, 159, 111, 62, 241, 68, 158, 84, 207, 4, 179, 111, 115, 140, 57, 47, 105, 69, 140, 7 } });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "BodyType", "VehicleRegistrationNumber" },
                values: new object[,]
                {
                    { new Guid("103cc7fc-5ee5-4550-8054-7c39508e205a"), "Truck", "KCZ 595L" },
                    { new Guid("21e3faeb-c997-42b5-877c-94849b7e60ad"), "Truck", "KDE 228S" },
                    { new Guid("33550007-7d8a-4ef3-b2ff-f5ac03722d1a"), "Van", "KBF 462A" },
                    { new Guid("41eb4491-48f3-4000-b225-be5726589178"), "Truck", "KDL 085M" },
                    { new Guid("514eabce-b25b-435a-b392-da82132e22de"), "Van", "KAY 215H" },
                    { new Guid("557c3972-9d58-42f3-a37f-34cf50becfbe"), "Truck", "KDB 387Q" },
                    { new Guid("f75fc171-d918-425f-9ce4-4eb72f3193d0"), "Truck", "KCY 067A" }
                });
        }
    }
}
