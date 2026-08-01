using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace wms_android.api.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceItemSenderReceiverClerk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<string>(
                name: "ClerkName",
                table: "InvoiceItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Receiver",
                table: "InvoiceItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sender",
                table: "InvoiceItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "BranchDeposits",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedById",
                table: "BranchDeposits",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "BranchDeposits",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "BranchDeposits",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "pending");

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "FirstName", "Initials", "LastName", "LicenseNumber" },
                values: new object[,]
                {
                    { new Guid("0992e101-222d-44ab-8f9f-0e2fd6eea8c9"), "Charles", "CM", "Maina", "DL123456" },
                    { new Guid("09bb029f-b546-4bdb-b346-4e47383fcbc4"), "John", "JM", "Mwai", "DL123456" },
                    { new Guid("469e6fd3-b467-44b5-9e5e-e83753a39f20"), "Erastus", "EK", "Kagwa", "DL123456" },
                    { new Guid("71f3ddc3-13e9-4034-8f8e-555de7d2c95e"), "John", "JN", "Njuguna", "DL123456" },
                    { new Guid("89e2cdbc-54c5-40d2-98cb-555da6a05e1e"), "David", "DM", "Mwangi", "DL123456" },
                    { new Guid("9faff5e4-540c-48e9-9801-41f9a6771109"), "Julius", "JK", "Kamula", "DL123456" },
                    { new Guid("acd2b420-6428-4b74-867a-67cd9e655229"), "David", "DK", "Kibet", "DL123456" },
                    { new Guid("adef1ee5-2bde-4bba-85b5-2c9f07c852cf"), "Stephen", "SK", "Kimuyu", "DL123456" },
                    { new Guid("f10a6a12-7b6a-4355-8c16-4084b02557ce"), "James", "JG", "Gichohi", "DL123456" },
                    { new Guid("f4fb4103-1c44-44a3-b08f-4dd9cbed6b85"), "Robert", "RN", "Njuguna", "DL123456" }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 17, 76, 213, 139, 2, 222, 208, 180, 52, 23, 215, 76, 123, 131, 176, 124, 214, 197, 114, 7, 192, 141, 31, 21, 136, 72, 210, 40, 197, 191, 224, 118, 48, 97, 165, 135, 70, 229, 228, 120, 167, 223, 110, 244, 181, 155, 161, 51, 108, 210, 66, 98, 141, 137, 136, 60, 19, 20, 85, 197, 241, 133, 154, 212 }, new byte[] { 79, 240, 208, 75, 202, 111, 52, 98, 53, 92, 239, 50, 71, 43, 138, 55, 11, 209, 145, 133, 184, 136, 159, 245, 196, 224, 46, 6, 15, 141, 99, 150, 108, 24, 23, 205, 146, 99, 222, 156, 138, 73, 165, 150, 144, 230, 56, 58, 97, 251, 106, 79, 1, 248, 172, 124, 107, 128, 246, 126, 128, 225, 52, 69, 47, 7, 28, 128, 90, 171, 191, 109, 206, 57, 103, 37, 95, 158, 182, 56, 105, 155, 22, 181, 88, 243, 12, 52, 246, 172, 226, 231, 177, 144, 98, 155, 93, 159, 96, 141, 95, 69, 116, 173, 61, 19, 201, 205, 200, 67, 123, 83, 51, 34, 12, 58, 3, 111, 55, 133, 177, 6, 231, 214, 196, 66, 79, 199 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 17, 76, 213, 139, 2, 222, 208, 180, 52, 23, 215, 76, 123, 131, 176, 124, 214, 197, 114, 7, 192, 141, 31, 21, 136, 72, 210, 40, 197, 191, 224, 118, 48, 97, 165, 135, 70, 229, 228, 120, 167, 223, 110, 244, 181, 155, 161, 51, 108, 210, 66, 98, 141, 137, 136, 60, 19, 20, 85, 197, 241, 133, 154, 212 }, new byte[] { 79, 240, 208, 75, 202, 111, 52, 98, 53, 92, 239, 50, 71, 43, 138, 55, 11, 209, 145, 133, 184, 136, 159, 245, 196, 224, 46, 6, 15, 141, 99, 150, 108, 24, 23, 205, 146, 99, 222, 156, 138, 73, 165, 150, 144, 230, 56, 58, 97, 251, 106, 79, 1, 248, 172, 124, 107, 128, 246, 126, 128, 225, 52, 69, 47, 7, 28, 128, 90, 171, 191, 109, 206, 57, 103, 37, 95, 158, 182, 56, 105, 155, 22, 181, 88, 243, 12, 52, 246, 172, 226, 231, 177, 144, 98, 155, 93, 159, 96, 141, 95, 69, 116, 173, 61, 19, 201, 205, 200, 67, 123, 83, 51, 34, 12, 58, 3, 111, 55, 133, 177, 6, 231, 214, 196, 66, 79, 199 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 17, 76, 213, 139, 2, 222, 208, 180, 52, 23, 215, 76, 123, 131, 176, 124, 214, 197, 114, 7, 192, 141, 31, 21, 136, 72, 210, 40, 197, 191, 224, 118, 48, 97, 165, 135, 70, 229, 228, 120, 167, 223, 110, 244, 181, 155, 161, 51, 108, 210, 66, 98, 141, 137, 136, 60, 19, 20, 85, 197, 241, 133, 154, 212 }, new byte[] { 79, 240, 208, 75, 202, 111, 52, 98, 53, 92, 239, 50, 71, 43, 138, 55, 11, 209, 145, 133, 184, 136, 159, 245, 196, 224, 46, 6, 15, 141, 99, 150, 108, 24, 23, 205, 146, 99, 222, 156, 138, 73, 165, 150, 144, 230, 56, 58, 97, 251, 106, 79, 1, 248, 172, 124, 107, 128, 246, 126, 128, 225, 52, 69, 47, 7, 28, 128, 90, 171, 191, 109, 206, 57, 103, 37, 95, 158, 182, 56, 105, 155, 22, 181, 88, 243, 12, 52, 246, 172, 226, 231, 177, 144, 98, 155, 93, 159, 96, 141, 95, 69, 116, 173, 61, 19, 201, 205, 200, 67, 123, 83, 51, 34, 12, 58, 3, 111, 55, 133, 177, 6, 231, 214, 196, 66, 79, 199 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 17, 76, 213, 139, 2, 222, 208, 180, 52, 23, 215, 76, 123, 131, 176, 124, 214, 197, 114, 7, 192, 141, 31, 21, 136, 72, 210, 40, 197, 191, 224, 118, 48, 97, 165, 135, 70, 229, 228, 120, 167, 223, 110, 244, 181, 155, 161, 51, 108, 210, 66, 98, 141, 137, 136, 60, 19, 20, 85, 197, 241, 133, 154, 212 }, new byte[] { 79, 240, 208, 75, 202, 111, 52, 98, 53, 92, 239, 50, 71, 43, 138, 55, 11, 209, 145, 133, 184, 136, 159, 245, 196, 224, 46, 6, 15, 141, 99, 150, 108, 24, 23, 205, 146, 99, 222, 156, 138, 73, 165, 150, 144, 230, 56, 58, 97, 251, 106, 79, 1, 248, 172, 124, 107, 128, 246, 126, 128, 225, 52, 69, 47, 7, 28, 128, 90, 171, 191, 109, 206, 57, 103, 37, 95, 158, 182, 56, 105, 155, 22, 181, 88, 243, 12, 52, 246, 172, 226, 231, 177, 144, 98, 155, 93, 159, 96, 141, 95, 69, 116, 173, 61, 19, 201, 205, 200, 67, 123, 83, 51, 34, 12, 58, 3, 111, 55, 133, 177, 6, 231, 214, 196, 66, 79, 199 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 17, 76, 213, 139, 2, 222, 208, 180, 52, 23, 215, 76, 123, 131, 176, 124, 214, 197, 114, 7, 192, 141, 31, 21, 136, 72, 210, 40, 197, 191, 224, 118, 48, 97, 165, 135, 70, 229, 228, 120, 167, 223, 110, 244, 181, 155, 161, 51, 108, 210, 66, 98, 141, 137, 136, 60, 19, 20, 85, 197, 241, 133, 154, 212 }, new byte[] { 79, 240, 208, 75, 202, 111, 52, 98, 53, 92, 239, 50, 71, 43, 138, 55, 11, 209, 145, 133, 184, 136, 159, 245, 196, 224, 46, 6, 15, 141, 99, 150, 108, 24, 23, 205, 146, 99, 222, 156, 138, 73, 165, 150, 144, 230, 56, 58, 97, 251, 106, 79, 1, 248, 172, 124, 107, 128, 246, 126, 128, 225, 52, 69, 47, 7, 28, 128, 90, 171, 191, 109, 206, 57, 103, 37, 95, 158, 182, 56, 105, 155, 22, 181, 88, 243, 12, 52, 246, 172, 226, 231, 177, 144, 98, 155, 93, 159, 96, 141, 95, 69, 116, 173, 61, 19, 201, 205, 200, 67, 123, 83, 51, 34, 12, 58, 3, 111, 55, 133, 177, 6, 231, 214, 196, 66, 79, 199 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 17, 76, 213, 139, 2, 222, 208, 180, 52, 23, 215, 76, 123, 131, 176, 124, 214, 197, 114, 7, 192, 141, 31, 21, 136, 72, 210, 40, 197, 191, 224, 118, 48, 97, 165, 135, 70, 229, 228, 120, 167, 223, 110, 244, 181, 155, 161, 51, 108, 210, 66, 98, 141, 137, 136, 60, 19, 20, 85, 197, 241, 133, 154, 212 }, new byte[] { 79, 240, 208, 75, 202, 111, 52, 98, 53, 92, 239, 50, 71, 43, 138, 55, 11, 209, 145, 133, 184, 136, 159, 245, 196, 224, 46, 6, 15, 141, 99, 150, 108, 24, 23, 205, 146, 99, 222, 156, 138, 73, 165, 150, 144, 230, 56, 58, 97, 251, 106, 79, 1, 248, 172, 124, 107, 128, 246, 126, 128, 225, 52, 69, 47, 7, 28, 128, 90, 171, 191, 109, 206, 57, 103, 37, 95, 158, 182, 56, 105, 155, 22, 181, 88, 243, 12, 52, 246, 172, 226, 231, 177, 144, 98, 155, 93, 159, 96, 141, 95, 69, 116, 173, 61, 19, 201, 205, 200, 67, 123, 83, 51, 34, 12, 58, 3, 111, 55, 133, 177, 6, 231, 214, 196, 66, 79, 199 } });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "BodyType", "VehicleRegistrationNumber" },
                values: new object[,]
                {
                    { new Guid("3e726138-800c-4ce6-bb2f-402e36d0ec61"), "Truck", "KDE 228S" },
                    { new Guid("5422e6bf-3b1d-4978-a04c-1626aa461769"), "Van", "KBF 462A" },
                    { new Guid("609b6f26-7998-4e19-b37b-7d9a0a65aeee"), "Truck", "KDL 085M" },
                    { new Guid("7cec0413-417f-4ef3-8103-7f04b7a70574"), "Truck", "KCZ 595L" },
                    { new Guid("93528e20-086c-4b62-8a13-e941cb6e90e1"), "Van", "KAY 215H" },
                    { new Guid("cbd969ab-b456-483c-94be-8d0f8dac05e6"), "Truck", "KCY 067A" },
                    { new Guid("f58e952b-fc29-4e4b-a176-dcaf310ec57e"), "Truck", "KDB 387Q" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BranchDeposits_ApprovedById",
                table: "BranchDeposits",
                column: "ApprovedById");

            migrationBuilder.AddForeignKey(
                name: "FK_BranchDeposits_Users_ApprovedById",
                table: "BranchDeposits",
                column: "ApprovedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BranchDeposits_Users_ApprovedById",
                table: "BranchDeposits");

            migrationBuilder.DropIndex(
                name: "IX_BranchDeposits_ApprovedById",
                table: "BranchDeposits");

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("0992e101-222d-44ab-8f9f-0e2fd6eea8c9"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("09bb029f-b546-4bdb-b346-4e47383fcbc4"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("469e6fd3-b467-44b5-9e5e-e83753a39f20"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("71f3ddc3-13e9-4034-8f8e-555de7d2c95e"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("89e2cdbc-54c5-40d2-98cb-555da6a05e1e"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("9faff5e4-540c-48e9-9801-41f9a6771109"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("acd2b420-6428-4b74-867a-67cd9e655229"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("adef1ee5-2bde-4bba-85b5-2c9f07c852cf"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("f10a6a12-7b6a-4355-8c16-4084b02557ce"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("f4fb4103-1c44-44a3-b08f-4dd9cbed6b85"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("3e726138-800c-4ce6-bb2f-402e36d0ec61"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("5422e6bf-3b1d-4978-a04c-1626aa461769"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("609b6f26-7998-4e19-b37b-7d9a0a65aeee"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("7cec0413-417f-4ef3-8103-7f04b7a70574"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("93528e20-086c-4b62-8a13-e941cb6e90e1"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("cbd969ab-b456-483c-94be-8d0f8dac05e6"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("f58e952b-fc29-4e4b-a176-dcaf310ec57e"));

            migrationBuilder.DropColumn(
                name: "ClerkName",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "Receiver",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "Sender",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "BranchDeposits");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "BranchDeposits");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "BranchDeposits");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BranchDeposits");

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
    }
}
