using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace wms_android.api.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchSegmentSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("0dfbb9c6-5499-4e54-940c-67ec9dbeb7be"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("4b2b392c-33db-4865-9be2-bd1b9df250e3"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("60ef3dab-bd2e-421d-9977-2c81e9470405"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("63756509-979b-494b-9e66-e96a4e58af17"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("6496dd47-95e6-497f-a48d-d213a3fbcb07"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("ab94ee29-c601-4658-8fa7-d35e098c6eed"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("ad37940d-3460-4f29-a78e-9cc302b2182b"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("aee08f71-fce3-45f4-9d2b-60b94d4d345c"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("de86d3eb-707f-47e2-8ea9-7f75d3ce7126"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("eab8076f-5889-43fb-b7d7-bd76e8d8a42f"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("3a7fe92f-b5be-4783-9e82-cdc1ea1571f7"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("7b15c870-8f4c-471e-85d4-56b81a6aa657"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("80ecc1a4-305e-4ba8-99cb-3f0a9643aa00"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("80faee9a-765e-4c1d-9ab7-4df3ebd6cfa2"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("a20ab872-531b-4854-b040-e0b940d222f2"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("da8aafef-8069-4120-a1f3-3e23f01cf139"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("ff1e5f8d-8deb-43e0-8204-9798afa4512d"));

            migrationBuilder.CreateTable(
                name: "DispatchSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Destination = table.Column<string>(type: "text", nullable: false),
                    TotalParcels = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchSegments_Dispatches_DispatchId",
                        column: x => x.DispatchId,
                        principalTable: "Dispatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispatchSegmentParcels",
                columns: table => new
                {
                    DispatchSegmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParcelsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchSegmentParcels", x => new { x.DispatchSegmentId, x.ParcelsId });
                    table.ForeignKey(
                        name: "FK_DispatchSegmentParcels_DispatchSegments_DispatchSegmentId",
                        column: x => x.DispatchSegmentId,
                        principalTable: "DispatchSegments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DispatchSegmentParcels_Parcels_ParcelsId",
                        column: x => x.ParcelsId,
                        principalTable: "Parcels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "FirstName", "Initials", "LastName", "LicenseNumber" },
                values: new object[,]
                {
                    { new Guid("165e78b6-511f-4444-9d23-815f5996ac19"), "John", "JN", "Njuguna", "DL123456" },
                    { new Guid("1fc8914a-359b-442d-8a42-bd7cb49ffa57"), "Erastus", "EK", "Kagwa", "DL123456" },
                    { new Guid("57450ca2-72dd-4e15-951b-91c03177dad6"), "Julius", "JK", "Kamula", "DL123456" },
                    { new Guid("5b20eb78-6f71-4e94-bea4-4df88fcf80f3"), "David", "DK", "Kibet", "DL123456" },
                    { new Guid("83849ed5-baf3-4ab2-af46-dc4d88df3057"), "Stephen", "SK", "Kimuyu", "DL123456" },
                    { new Guid("8dba2978-f214-47da-b26a-d5af54ae3f49"), "David", "DM", "Mwangi", "DL123456" },
                    { new Guid("9222fc5b-742d-4139-b31b-4892c4ac4211"), "James", "JG", "Gichohi", "DL123456" },
                    { new Guid("ae5dea50-b910-44df-b0bd-9b78a7b5373c"), "John", "JM", "Mwai", "DL123456" },
                    { new Guid("cb8793cd-eab0-4ea2-92fa-008d4b23e53d"), "Robert", "RN", "Njuguna", "DL123456" },
                    { new Guid("f6b2680c-a335-4a2c-b651-37f19c400d12"), "Charles", "CM", "Maina", "DL123456" }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 54, 155, 82, 136, 60, 127, 229, 37, 83, 37, 201, 216, 36, 31, 121, 71, 111, 39, 130, 158, 91, 154, 225, 100, 59, 15, 124, 131, 99, 169, 62, 65, 188, 130, 79, 109, 199, 20, 93, 169, 41, 33, 217, 92, 238, 233, 212, 33, 38, 179, 116, 106, 69, 12, 46, 181, 188, 238, 74, 15, 175, 112, 42, 180 }, new byte[] { 250, 148, 48, 95, 253, 111, 167, 68, 71, 19, 236, 163, 97, 243, 121, 44, 29, 84, 81, 37, 161, 39, 9, 123, 83, 207, 209, 122, 149, 117, 72, 22, 105, 195, 138, 153, 243, 46, 195, 110, 60, 19, 193, 60, 82, 35, 157, 206, 28, 106, 142, 152, 138, 8, 174, 230, 14, 71, 203, 94, 245, 18, 49, 140, 123, 88, 126, 30, 55, 207, 183, 123, 171, 150, 36, 182, 128, 183, 22, 96, 95, 114, 99, 115, 227, 206, 214, 48, 151, 229, 249, 171, 70, 230, 172, 82, 145, 147, 23, 201, 111, 12, 27, 237, 58, 245, 213, 122, 81, 229, 73, 33, 148, 188, 125, 130, 241, 74, 85, 124, 46, 70, 53, 145, 210, 246, 152, 58 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 54, 155, 82, 136, 60, 127, 229, 37, 83, 37, 201, 216, 36, 31, 121, 71, 111, 39, 130, 158, 91, 154, 225, 100, 59, 15, 124, 131, 99, 169, 62, 65, 188, 130, 79, 109, 199, 20, 93, 169, 41, 33, 217, 92, 238, 233, 212, 33, 38, 179, 116, 106, 69, 12, 46, 181, 188, 238, 74, 15, 175, 112, 42, 180 }, new byte[] { 250, 148, 48, 95, 253, 111, 167, 68, 71, 19, 236, 163, 97, 243, 121, 44, 29, 84, 81, 37, 161, 39, 9, 123, 83, 207, 209, 122, 149, 117, 72, 22, 105, 195, 138, 153, 243, 46, 195, 110, 60, 19, 193, 60, 82, 35, 157, 206, 28, 106, 142, 152, 138, 8, 174, 230, 14, 71, 203, 94, 245, 18, 49, 140, 123, 88, 126, 30, 55, 207, 183, 123, 171, 150, 36, 182, 128, 183, 22, 96, 95, 114, 99, 115, 227, 206, 214, 48, 151, 229, 249, 171, 70, 230, 172, 82, 145, 147, 23, 201, 111, 12, 27, 237, 58, 245, 213, 122, 81, 229, 73, 33, 148, 188, 125, 130, 241, 74, 85, 124, 46, 70, 53, 145, 210, 246, 152, 58 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 54, 155, 82, 136, 60, 127, 229, 37, 83, 37, 201, 216, 36, 31, 121, 71, 111, 39, 130, 158, 91, 154, 225, 100, 59, 15, 124, 131, 99, 169, 62, 65, 188, 130, 79, 109, 199, 20, 93, 169, 41, 33, 217, 92, 238, 233, 212, 33, 38, 179, 116, 106, 69, 12, 46, 181, 188, 238, 74, 15, 175, 112, 42, 180 }, new byte[] { 250, 148, 48, 95, 253, 111, 167, 68, 71, 19, 236, 163, 97, 243, 121, 44, 29, 84, 81, 37, 161, 39, 9, 123, 83, 207, 209, 122, 149, 117, 72, 22, 105, 195, 138, 153, 243, 46, 195, 110, 60, 19, 193, 60, 82, 35, 157, 206, 28, 106, 142, 152, 138, 8, 174, 230, 14, 71, 203, 94, 245, 18, 49, 140, 123, 88, 126, 30, 55, 207, 183, 123, 171, 150, 36, 182, 128, 183, 22, 96, 95, 114, 99, 115, 227, 206, 214, 48, 151, 229, 249, 171, 70, 230, 172, 82, 145, 147, 23, 201, 111, 12, 27, 237, 58, 245, 213, 122, 81, 229, 73, 33, 148, 188, 125, 130, 241, 74, 85, 124, 46, 70, 53, 145, 210, 246, 152, 58 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 54, 155, 82, 136, 60, 127, 229, 37, 83, 37, 201, 216, 36, 31, 121, 71, 111, 39, 130, 158, 91, 154, 225, 100, 59, 15, 124, 131, 99, 169, 62, 65, 188, 130, 79, 109, 199, 20, 93, 169, 41, 33, 217, 92, 238, 233, 212, 33, 38, 179, 116, 106, 69, 12, 46, 181, 188, 238, 74, 15, 175, 112, 42, 180 }, new byte[] { 250, 148, 48, 95, 253, 111, 167, 68, 71, 19, 236, 163, 97, 243, 121, 44, 29, 84, 81, 37, 161, 39, 9, 123, 83, 207, 209, 122, 149, 117, 72, 22, 105, 195, 138, 153, 243, 46, 195, 110, 60, 19, 193, 60, 82, 35, 157, 206, 28, 106, 142, 152, 138, 8, 174, 230, 14, 71, 203, 94, 245, 18, 49, 140, 123, 88, 126, 30, 55, 207, 183, 123, 171, 150, 36, 182, 128, 183, 22, 96, 95, 114, 99, 115, 227, 206, 214, 48, 151, 229, 249, 171, 70, 230, 172, 82, 145, 147, 23, 201, 111, 12, 27, 237, 58, 245, 213, 122, 81, 229, 73, 33, 148, 188, 125, 130, 241, 74, 85, 124, 46, 70, 53, 145, 210, 246, 152, 58 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 54, 155, 82, 136, 60, 127, 229, 37, 83, 37, 201, 216, 36, 31, 121, 71, 111, 39, 130, 158, 91, 154, 225, 100, 59, 15, 124, 131, 99, 169, 62, 65, 188, 130, 79, 109, 199, 20, 93, 169, 41, 33, 217, 92, 238, 233, 212, 33, 38, 179, 116, 106, 69, 12, 46, 181, 188, 238, 74, 15, 175, 112, 42, 180 }, new byte[] { 250, 148, 48, 95, 253, 111, 167, 68, 71, 19, 236, 163, 97, 243, 121, 44, 29, 84, 81, 37, 161, 39, 9, 123, 83, 207, 209, 122, 149, 117, 72, 22, 105, 195, 138, 153, 243, 46, 195, 110, 60, 19, 193, 60, 82, 35, 157, 206, 28, 106, 142, 152, 138, 8, 174, 230, 14, 71, 203, 94, 245, 18, 49, 140, 123, 88, 126, 30, 55, 207, 183, 123, 171, 150, 36, 182, 128, 183, 22, 96, 95, 114, 99, 115, 227, 206, 214, 48, 151, 229, 249, 171, 70, 230, 172, 82, 145, 147, 23, 201, 111, 12, 27, 237, 58, 245, 213, 122, 81, 229, 73, 33, 148, 188, 125, 130, 241, 74, 85, 124, 46, 70, 53, 145, 210, 246, 152, 58 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 54, 155, 82, 136, 60, 127, 229, 37, 83, 37, 201, 216, 36, 31, 121, 71, 111, 39, 130, 158, 91, 154, 225, 100, 59, 15, 124, 131, 99, 169, 62, 65, 188, 130, 79, 109, 199, 20, 93, 169, 41, 33, 217, 92, 238, 233, 212, 33, 38, 179, 116, 106, 69, 12, 46, 181, 188, 238, 74, 15, 175, 112, 42, 180 }, new byte[] { 250, 148, 48, 95, 253, 111, 167, 68, 71, 19, 236, 163, 97, 243, 121, 44, 29, 84, 81, 37, 161, 39, 9, 123, 83, 207, 209, 122, 149, 117, 72, 22, 105, 195, 138, 153, 243, 46, 195, 110, 60, 19, 193, 60, 82, 35, 157, 206, 28, 106, 142, 152, 138, 8, 174, 230, 14, 71, 203, 94, 245, 18, 49, 140, 123, 88, 126, 30, 55, 207, 183, 123, 171, 150, 36, 182, 128, 183, 22, 96, 95, 114, 99, 115, 227, 206, 214, 48, 151, 229, 249, 171, 70, 230, 172, 82, 145, 147, 23, 201, 111, 12, 27, 237, 58, 245, 213, 122, 81, 229, 73, 33, 148, 188, 125, 130, 241, 74, 85, 124, 46, 70, 53, 145, 210, 246, 152, 58 } });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "BodyType", "VehicleRegistrationNumber" },
                values: new object[,]
                {
                    { new Guid("1200dd83-21e2-4765-9671-ae52463ad873"), "Van", "KBF 462A" },
                    { new Guid("2563c554-fcb3-4c45-a09d-f20056e2cc96"), "Truck", "KCY 067A" },
                    { new Guid("45c58ddd-2cf2-4d66-8e50-55578705b0ea"), "Van", "KAY 215H" },
                    { new Guid("938854fb-75ac-4eae-882d-5aa1f069005e"), "Truck", "KCZ 595L" },
                    { new Guid("d714b432-601e-4872-9b0a-7409b4502190"), "Truck", "KDL 085M" },
                    { new Guid("e0497ba1-f6ba-4e3f-a35d-8b90a37b24cb"), "Truck", "KDB 387Q" },
                    { new Guid("e6930f93-2769-4f96-a654-36a71293c3a6"), "Truck", "KDE 228S" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchSegmentParcels_ParcelsId",
                table: "DispatchSegmentParcels",
                column: "ParcelsId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchSegments_DispatchId",
                table: "DispatchSegments",
                column: "DispatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DispatchSegmentParcels");

            migrationBuilder.DropTable(
                name: "DispatchSegments");

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("165e78b6-511f-4444-9d23-815f5996ac19"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("1fc8914a-359b-442d-8a42-bd7cb49ffa57"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("57450ca2-72dd-4e15-951b-91c03177dad6"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("5b20eb78-6f71-4e94-bea4-4df88fcf80f3"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("83849ed5-baf3-4ab2-af46-dc4d88df3057"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("8dba2978-f214-47da-b26a-d5af54ae3f49"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("9222fc5b-742d-4139-b31b-4892c4ac4211"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("ae5dea50-b910-44df-b0bd-9b78a7b5373c"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("cb8793cd-eab0-4ea2-92fa-008d4b23e53d"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("f6b2680c-a335-4a2c-b651-37f19c400d12"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("1200dd83-21e2-4765-9671-ae52463ad873"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("2563c554-fcb3-4c45-a09d-f20056e2cc96"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("45c58ddd-2cf2-4d66-8e50-55578705b0ea"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("938854fb-75ac-4eae-882d-5aa1f069005e"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("d714b432-601e-4872-9b0a-7409b4502190"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("e0497ba1-f6ba-4e3f-a35d-8b90a37b24cb"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("e6930f93-2769-4f96-a654-36a71293c3a6"));

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "FirstName", "Initials", "LastName", "LicenseNumber" },
                values: new object[,]
                {
                    { new Guid("0dfbb9c6-5499-4e54-940c-67ec9dbeb7be"), "Charles", "CM", "Maina", "DL123456" },
                    { new Guid("4b2b392c-33db-4865-9be2-bd1b9df250e3"), "Julius", "JK", "Kamula", "DL123456" },
                    { new Guid("60ef3dab-bd2e-421d-9977-2c81e9470405"), "James", "JG", "Gichohi", "DL123456" },
                    { new Guid("63756509-979b-494b-9e66-e96a4e58af17"), "Robert", "RN", "Njuguna", "DL123456" },
                    { new Guid("6496dd47-95e6-497f-a48d-d213a3fbcb07"), "David", "DM", "Mwangi", "DL123456" },
                    { new Guid("ab94ee29-c601-4658-8fa7-d35e098c6eed"), "David", "DK", "Kibet", "DL123456" },
                    { new Guid("ad37940d-3460-4f29-a78e-9cc302b2182b"), "Stephen", "SK", "Kimuyu", "DL123456" },
                    { new Guid("aee08f71-fce3-45f4-9d2b-60b94d4d345c"), "John", "JM", "Mwai", "DL123456" },
                    { new Guid("de86d3eb-707f-47e2-8ea9-7f75d3ce7126"), "John", "JN", "Njuguna", "DL123456" },
                    { new Guid("eab8076f-5889-43fb-b7d7-bd76e8d8a42f"), "Erastus", "EK", "Kagwa", "DL123456" }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 11, 85, 28, 91, 189, 249, 182, 113, 154, 180, 243, 130, 182, 111, 209, 186, 160, 32, 142, 36, 35, 35, 246, 171, 100, 36, 210, 170, 3, 32, 235, 119, 216, 83, 132, 47, 194, 66, 220, 182, 179, 244, 150, 84, 249, 141, 114, 36, 5, 37, 162, 32, 38, 171, 88, 248, 104, 26, 131, 158, 151, 102, 130, 161 }, new byte[] { 120, 54, 33, 190, 131, 59, 144, 245, 159, 199, 250, 137, 160, 47, 56, 114, 141, 230, 192, 69, 141, 2, 173, 197, 236, 30, 157, 83, 146, 18, 52, 122, 175, 79, 130, 21, 244, 179, 110, 254, 68, 74, 214, 132, 18, 230, 12, 70, 228, 76, 224, 129, 179, 68, 250, 37, 171, 43, 34, 162, 200, 96, 231, 246, 29, 154, 164, 201, 118, 119, 142, 185, 33, 242, 223, 77, 145, 204, 116, 222, 134, 41, 39, 150, 103, 205, 166, 46, 151, 24, 93, 73, 216, 99, 215, 216, 88, 36, 8, 56, 101, 88, 18, 7, 148, 142, 177, 219, 168, 66, 42, 41, 82, 118, 6, 35, 211, 90, 212, 209, 129, 137, 95, 172, 40, 13, 144, 161 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 11, 85, 28, 91, 189, 249, 182, 113, 154, 180, 243, 130, 182, 111, 209, 186, 160, 32, 142, 36, 35, 35, 246, 171, 100, 36, 210, 170, 3, 32, 235, 119, 216, 83, 132, 47, 194, 66, 220, 182, 179, 244, 150, 84, 249, 141, 114, 36, 5, 37, 162, 32, 38, 171, 88, 248, 104, 26, 131, 158, 151, 102, 130, 161 }, new byte[] { 120, 54, 33, 190, 131, 59, 144, 245, 159, 199, 250, 137, 160, 47, 56, 114, 141, 230, 192, 69, 141, 2, 173, 197, 236, 30, 157, 83, 146, 18, 52, 122, 175, 79, 130, 21, 244, 179, 110, 254, 68, 74, 214, 132, 18, 230, 12, 70, 228, 76, 224, 129, 179, 68, 250, 37, 171, 43, 34, 162, 200, 96, 231, 246, 29, 154, 164, 201, 118, 119, 142, 185, 33, 242, 223, 77, 145, 204, 116, 222, 134, 41, 39, 150, 103, 205, 166, 46, 151, 24, 93, 73, 216, 99, 215, 216, 88, 36, 8, 56, 101, 88, 18, 7, 148, 142, 177, 219, 168, 66, 42, 41, 82, 118, 6, 35, 211, 90, 212, 209, 129, 137, 95, 172, 40, 13, 144, 161 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 11, 85, 28, 91, 189, 249, 182, 113, 154, 180, 243, 130, 182, 111, 209, 186, 160, 32, 142, 36, 35, 35, 246, 171, 100, 36, 210, 170, 3, 32, 235, 119, 216, 83, 132, 47, 194, 66, 220, 182, 179, 244, 150, 84, 249, 141, 114, 36, 5, 37, 162, 32, 38, 171, 88, 248, 104, 26, 131, 158, 151, 102, 130, 161 }, new byte[] { 120, 54, 33, 190, 131, 59, 144, 245, 159, 199, 250, 137, 160, 47, 56, 114, 141, 230, 192, 69, 141, 2, 173, 197, 236, 30, 157, 83, 146, 18, 52, 122, 175, 79, 130, 21, 244, 179, 110, 254, 68, 74, 214, 132, 18, 230, 12, 70, 228, 76, 224, 129, 179, 68, 250, 37, 171, 43, 34, 162, 200, 96, 231, 246, 29, 154, 164, 201, 118, 119, 142, 185, 33, 242, 223, 77, 145, 204, 116, 222, 134, 41, 39, 150, 103, 205, 166, 46, 151, 24, 93, 73, 216, 99, 215, 216, 88, 36, 8, 56, 101, 88, 18, 7, 148, 142, 177, 219, 168, 66, 42, 41, 82, 118, 6, 35, 211, 90, 212, 209, 129, 137, 95, 172, 40, 13, 144, 161 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 11, 85, 28, 91, 189, 249, 182, 113, 154, 180, 243, 130, 182, 111, 209, 186, 160, 32, 142, 36, 35, 35, 246, 171, 100, 36, 210, 170, 3, 32, 235, 119, 216, 83, 132, 47, 194, 66, 220, 182, 179, 244, 150, 84, 249, 141, 114, 36, 5, 37, 162, 32, 38, 171, 88, 248, 104, 26, 131, 158, 151, 102, 130, 161 }, new byte[] { 120, 54, 33, 190, 131, 59, 144, 245, 159, 199, 250, 137, 160, 47, 56, 114, 141, 230, 192, 69, 141, 2, 173, 197, 236, 30, 157, 83, 146, 18, 52, 122, 175, 79, 130, 21, 244, 179, 110, 254, 68, 74, 214, 132, 18, 230, 12, 70, 228, 76, 224, 129, 179, 68, 250, 37, 171, 43, 34, 162, 200, 96, 231, 246, 29, 154, 164, 201, 118, 119, 142, 185, 33, 242, 223, 77, 145, 204, 116, 222, 134, 41, 39, 150, 103, 205, 166, 46, 151, 24, 93, 73, 216, 99, 215, 216, 88, 36, 8, 56, 101, 88, 18, 7, 148, 142, 177, 219, 168, 66, 42, 41, 82, 118, 6, 35, 211, 90, 212, 209, 129, 137, 95, 172, 40, 13, 144, 161 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 11, 85, 28, 91, 189, 249, 182, 113, 154, 180, 243, 130, 182, 111, 209, 186, 160, 32, 142, 36, 35, 35, 246, 171, 100, 36, 210, 170, 3, 32, 235, 119, 216, 83, 132, 47, 194, 66, 220, 182, 179, 244, 150, 84, 249, 141, 114, 36, 5, 37, 162, 32, 38, 171, 88, 248, 104, 26, 131, 158, 151, 102, 130, 161 }, new byte[] { 120, 54, 33, 190, 131, 59, 144, 245, 159, 199, 250, 137, 160, 47, 56, 114, 141, 230, 192, 69, 141, 2, 173, 197, 236, 30, 157, 83, 146, 18, 52, 122, 175, 79, 130, 21, 244, 179, 110, 254, 68, 74, 214, 132, 18, 230, 12, 70, 228, 76, 224, 129, 179, 68, 250, 37, 171, 43, 34, 162, 200, 96, 231, 246, 29, 154, 164, 201, 118, 119, 142, 185, 33, 242, 223, 77, 145, 204, 116, 222, 134, 41, 39, 150, 103, 205, 166, 46, 151, 24, 93, 73, 216, 99, 215, 216, 88, 36, 8, 56, 101, 88, 18, 7, 148, 142, 177, 219, 168, 66, 42, 41, 82, 118, 6, 35, 211, 90, 212, 209, 129, 137, 95, 172, 40, 13, 144, 161 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 11, 85, 28, 91, 189, 249, 182, 113, 154, 180, 243, 130, 182, 111, 209, 186, 160, 32, 142, 36, 35, 35, 246, 171, 100, 36, 210, 170, 3, 32, 235, 119, 216, 83, 132, 47, 194, 66, 220, 182, 179, 244, 150, 84, 249, 141, 114, 36, 5, 37, 162, 32, 38, 171, 88, 248, 104, 26, 131, 158, 151, 102, 130, 161 }, new byte[] { 120, 54, 33, 190, 131, 59, 144, 245, 159, 199, 250, 137, 160, 47, 56, 114, 141, 230, 192, 69, 141, 2, 173, 197, 236, 30, 157, 83, 146, 18, 52, 122, 175, 79, 130, 21, 244, 179, 110, 254, 68, 74, 214, 132, 18, 230, 12, 70, 228, 76, 224, 129, 179, 68, 250, 37, 171, 43, 34, 162, 200, 96, 231, 246, 29, 154, 164, 201, 118, 119, 142, 185, 33, 242, 223, 77, 145, 204, 116, 222, 134, 41, 39, 150, 103, 205, 166, 46, 151, 24, 93, 73, 216, 99, 215, 216, 88, 36, 8, 56, 101, 88, 18, 7, 148, 142, 177, 219, 168, 66, 42, 41, 82, 118, 6, 35, 211, 90, 212, 209, 129, 137, 95, 172, 40, 13, 144, 161 } });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "BodyType", "VehicleRegistrationNumber" },
                values: new object[,]
                {
                    { new Guid("3a7fe92f-b5be-4783-9e82-cdc1ea1571f7"), "Van", "KAY 215H" },
                    { new Guid("7b15c870-8f4c-471e-85d4-56b81a6aa657"), "Truck", "KDL 085M" },
                    { new Guid("80ecc1a4-305e-4ba8-99cb-3f0a9643aa00"), "Van", "KBF 462A" },
                    { new Guid("80faee9a-765e-4c1d-9ab7-4df3ebd6cfa2"), "Truck", "KCZ 595L" },
                    { new Guid("a20ab872-531b-4854-b040-e0b940d222f2"), "Truck", "KDB 387Q" },
                    { new Guid("da8aafef-8069-4120-a1f3-3e23f01cf139"), "Truck", "KCY 067A" },
                    { new Guid("ff1e5f8d-8deb-43e0-8204-9798afa4512d"), "Truck", "KDE 228S" }
                });
        }
    }
}
