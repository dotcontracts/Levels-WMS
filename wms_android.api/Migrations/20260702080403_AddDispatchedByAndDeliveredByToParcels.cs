using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace wms_android.api.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchedByAndDeliveredByToParcels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parcels_Users_ConfirmedById",
                table: "Parcels");

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("3278f9ea-a172-460f-be8d-3fee1e0cd042"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("49d1bbbd-ad49-4846-9d8c-194cfa02ca42"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("56334594-b646-4ecf-a7dd-f9a39b04f9b1"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("5bced61c-2589-4a84-86f6-b42c193f32c7"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("64dde835-7ae0-4b42-892f-f273ab8a63ba"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("74020777-ccec-4aa4-a014-a3759698ec75"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("82f09f0a-d63c-4e95-aabf-7f5a0661a153"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("9f719efc-83e7-4d37-993c-ff7de539645c"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("e2edf8f9-1314-44dd-86b0-c947bedc9dbd"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("e95926c2-bc26-44c2-b709-6d29a9421357"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("43d5101d-8535-4df7-aa15-d52ce34f8206"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("47e5800a-8d59-43d6-ab1d-3584c46a8eb6"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("55b70b65-a894-4e29-82b1-8dbbcae52310"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("8693033c-adfd-4f20-967c-b9ceabcd0bdb"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("ac62557e-c804-4a62-9983-4d05e54c49ef"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("cb5f1a6f-961b-44fa-82ac-4366824dd764"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("fa15d8b7-a262-4bca-b9d0-84d9e1ccd8bf"));

            migrationBuilder.AlterColumn<DateTime>(
                name: "ConfirmedAt",
                table: "Parcels",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Parcels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveredById",
                table: "Parcels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DispatchedById",
                table: "Parcels",
                type: "integer",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "FirstName", "Initials", "LastName", "LicenseNumber" },
                values: new object[,]
                {
                    { new Guid("0796a06b-1e70-479b-89d5-25c0da5ef44a"), "Stephen", "SK", "Kimuyu", "DL123456" },
                    { new Guid("0e4a170e-b551-448a-babc-db7406cda302"), "Charles", "CM", "Maina", "DL123456" },
                    { new Guid("5c8347b5-0fd5-4010-ae94-64df3afcf21f"), "James", "JG", "Gichohi", "DL123456" },
                    { new Guid("7a624544-bc82-433d-ba2c-57ee7641fccd"), "David", "DM", "Mwangi", "DL123456" },
                    { new Guid("a88f4c84-cb9b-449c-809e-53dde9c7a2ed"), "David", "DK", "Kibet", "DL123456" },
                    { new Guid("be41eb8d-7ced-46ec-8ffa-f91e19de8a94"), "Erastus", "EK", "Kagwa", "DL123456" },
                    { new Guid("c867c947-3fa6-4515-b34a-94c0b98944d5"), "John", "JN", "Njuguna", "DL123456" },
                    { new Guid("f05d9f65-2d5e-4fb9-bf32-7747220d9f4e"), "John", "JM", "Mwai", "DL123456" },
                    { new Guid("f399279c-5682-483d-83fa-a9967e806b6a"), "Robert", "RN", "Njuguna", "DL123456" },
                    { new Guid("f4a1a7e0-ffa1-4370-ab99-cbeeb11b45a4"), "Julius", "JK", "Kamula", "DL123456" }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 35, 248, 146, 105, 108, 30, 141, 208, 20, 5, 52, 35, 86, 141, 89, 68, 169, 180, 208, 85, 188, 79, 71, 196, 5, 179, 240, 141, 218, 136, 169, 64, 232, 228, 22, 140, 194, 132, 201, 145, 117, 128, 179, 252, 121, 74, 38, 83, 222, 210, 165, 72, 89, 123, 205, 201, 140, 137, 97, 123, 63, 27, 135, 113 }, new byte[] { 194, 134, 21, 5, 199, 143, 213, 245, 185, 227, 119, 229, 30, 171, 239, 210, 111, 255, 159, 22, 35, 240, 168, 40, 195, 185, 54, 238, 241, 130, 57, 223, 182, 77, 141, 137, 13, 169, 163, 69, 208, 129, 17, 95, 53, 120, 94, 113, 121, 43, 144, 230, 13, 94, 60, 89, 56, 11, 198, 123, 25, 114, 17, 100, 228, 178, 246, 186, 226, 55, 116, 72, 178, 245, 67, 114, 197, 23, 216, 132, 236, 192, 20, 253, 7, 76, 24, 156, 15, 106, 237, 103, 222, 104, 62, 220, 194, 223, 205, 221, 61, 156, 58, 14, 82, 57, 224, 51, 19, 3, 67, 138, 74, 50, 199, 222, 125, 236, 84, 251, 220, 207, 239, 34, 104, 34, 41, 151 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 35, 248, 146, 105, 108, 30, 141, 208, 20, 5, 52, 35, 86, 141, 89, 68, 169, 180, 208, 85, 188, 79, 71, 196, 5, 179, 240, 141, 218, 136, 169, 64, 232, 228, 22, 140, 194, 132, 201, 145, 117, 128, 179, 252, 121, 74, 38, 83, 222, 210, 165, 72, 89, 123, 205, 201, 140, 137, 97, 123, 63, 27, 135, 113 }, new byte[] { 194, 134, 21, 5, 199, 143, 213, 245, 185, 227, 119, 229, 30, 171, 239, 210, 111, 255, 159, 22, 35, 240, 168, 40, 195, 185, 54, 238, 241, 130, 57, 223, 182, 77, 141, 137, 13, 169, 163, 69, 208, 129, 17, 95, 53, 120, 94, 113, 121, 43, 144, 230, 13, 94, 60, 89, 56, 11, 198, 123, 25, 114, 17, 100, 228, 178, 246, 186, 226, 55, 116, 72, 178, 245, 67, 114, 197, 23, 216, 132, 236, 192, 20, 253, 7, 76, 24, 156, 15, 106, 237, 103, 222, 104, 62, 220, 194, 223, 205, 221, 61, 156, 58, 14, 82, 57, 224, 51, 19, 3, 67, 138, 74, 50, 199, 222, 125, 236, 84, 251, 220, 207, 239, 34, 104, 34, 41, 151 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 35, 248, 146, 105, 108, 30, 141, 208, 20, 5, 52, 35, 86, 141, 89, 68, 169, 180, 208, 85, 188, 79, 71, 196, 5, 179, 240, 141, 218, 136, 169, 64, 232, 228, 22, 140, 194, 132, 201, 145, 117, 128, 179, 252, 121, 74, 38, 83, 222, 210, 165, 72, 89, 123, 205, 201, 140, 137, 97, 123, 63, 27, 135, 113 }, new byte[] { 194, 134, 21, 5, 199, 143, 213, 245, 185, 227, 119, 229, 30, 171, 239, 210, 111, 255, 159, 22, 35, 240, 168, 40, 195, 185, 54, 238, 241, 130, 57, 223, 182, 77, 141, 137, 13, 169, 163, 69, 208, 129, 17, 95, 53, 120, 94, 113, 121, 43, 144, 230, 13, 94, 60, 89, 56, 11, 198, 123, 25, 114, 17, 100, 228, 178, 246, 186, 226, 55, 116, 72, 178, 245, 67, 114, 197, 23, 216, 132, 236, 192, 20, 253, 7, 76, 24, 156, 15, 106, 237, 103, 222, 104, 62, 220, 194, 223, 205, 221, 61, 156, 58, 14, 82, 57, 224, 51, 19, 3, 67, 138, 74, 50, 199, 222, 125, 236, 84, 251, 220, 207, 239, 34, 104, 34, 41, 151 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 35, 248, 146, 105, 108, 30, 141, 208, 20, 5, 52, 35, 86, 141, 89, 68, 169, 180, 208, 85, 188, 79, 71, 196, 5, 179, 240, 141, 218, 136, 169, 64, 232, 228, 22, 140, 194, 132, 201, 145, 117, 128, 179, 252, 121, 74, 38, 83, 222, 210, 165, 72, 89, 123, 205, 201, 140, 137, 97, 123, 63, 27, 135, 113 }, new byte[] { 194, 134, 21, 5, 199, 143, 213, 245, 185, 227, 119, 229, 30, 171, 239, 210, 111, 255, 159, 22, 35, 240, 168, 40, 195, 185, 54, 238, 241, 130, 57, 223, 182, 77, 141, 137, 13, 169, 163, 69, 208, 129, 17, 95, 53, 120, 94, 113, 121, 43, 144, 230, 13, 94, 60, 89, 56, 11, 198, 123, 25, 114, 17, 100, 228, 178, 246, 186, 226, 55, 116, 72, 178, 245, 67, 114, 197, 23, 216, 132, 236, 192, 20, 253, 7, 76, 24, 156, 15, 106, 237, 103, 222, 104, 62, 220, 194, 223, 205, 221, 61, 156, 58, 14, 82, 57, 224, 51, 19, 3, 67, 138, 74, 50, 199, 222, 125, 236, 84, 251, 220, 207, 239, 34, 104, 34, 41, 151 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 35, 248, 146, 105, 108, 30, 141, 208, 20, 5, 52, 35, 86, 141, 89, 68, 169, 180, 208, 85, 188, 79, 71, 196, 5, 179, 240, 141, 218, 136, 169, 64, 232, 228, 22, 140, 194, 132, 201, 145, 117, 128, 179, 252, 121, 74, 38, 83, 222, 210, 165, 72, 89, 123, 205, 201, 140, 137, 97, 123, 63, 27, 135, 113 }, new byte[] { 194, 134, 21, 5, 199, 143, 213, 245, 185, 227, 119, 229, 30, 171, 239, 210, 111, 255, 159, 22, 35, 240, 168, 40, 195, 185, 54, 238, 241, 130, 57, 223, 182, 77, 141, 137, 13, 169, 163, 69, 208, 129, 17, 95, 53, 120, 94, 113, 121, 43, 144, 230, 13, 94, 60, 89, 56, 11, 198, 123, 25, 114, 17, 100, 228, 178, 246, 186, 226, 55, 116, 72, 178, 245, 67, 114, 197, 23, 216, 132, 236, 192, 20, 253, 7, 76, 24, 156, 15, 106, 237, 103, 222, 104, 62, 220, 194, 223, 205, 221, 61, 156, 58, 14, 82, 57, 224, 51, 19, 3, 67, 138, 74, 50, 199, 222, 125, 236, 84, 251, 220, 207, 239, 34, 104, 34, 41, 151 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 35, 248, 146, 105, 108, 30, 141, 208, 20, 5, 52, 35, 86, 141, 89, 68, 169, 180, 208, 85, 188, 79, 71, 196, 5, 179, 240, 141, 218, 136, 169, 64, 232, 228, 22, 140, 194, 132, 201, 145, 117, 128, 179, 252, 121, 74, 38, 83, 222, 210, 165, 72, 89, 123, 205, 201, 140, 137, 97, 123, 63, 27, 135, 113 }, new byte[] { 194, 134, 21, 5, 199, 143, 213, 245, 185, 227, 119, 229, 30, 171, 239, 210, 111, 255, 159, 22, 35, 240, 168, 40, 195, 185, 54, 238, 241, 130, 57, 223, 182, 77, 141, 137, 13, 169, 163, 69, 208, 129, 17, 95, 53, 120, 94, 113, 121, 43, 144, 230, 13, 94, 60, 89, 56, 11, 198, 123, 25, 114, 17, 100, 228, 178, 246, 186, 226, 55, 116, 72, 178, 245, 67, 114, 197, 23, 216, 132, 236, 192, 20, 253, 7, 76, 24, 156, 15, 106, 237, 103, 222, 104, 62, 220, 194, 223, 205, 221, 61, 156, 58, 14, 82, 57, 224, 51, 19, 3, 67, 138, 74, 50, 199, 222, 125, 236, 84, 251, 220, 207, 239, 34, 104, 34, 41, 151 } });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "BodyType", "VehicleRegistrationNumber" },
                values: new object[,]
                {
                    { new Guid("2b9b1b68-ce83-48df-809a-50bc192bcfc6"), "Truck", "KDE 228S" },
                    { new Guid("538be3af-24dd-40af-8c21-d390c94c892d"), "Van", "KBF 462A" },
                    { new Guid("8868f970-c944-404c-895b-5bbe75ed3cf7"), "Truck", "KCZ 595L" },
                    { new Guid("8bfc1d60-0be9-401e-8f01-c73f22c29efc"), "Truck", "KDL 085M" },
                    { new Guid("a1046220-0603-4b1e-ab6e-1f5f4fb32e0a"), "Van", "KAY 215H" },
                    { new Guid("e7239c81-41b5-48b3-adc1-3ab6e50cb313"), "Truck", "KCY 067A" },
                    { new Guid("ff3be2f6-69a4-4567-be0f-6874781dd765"), "Truck", "KDB 387Q" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_DeliveredById",
                table: "Parcels",
                column: "DeliveredById");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_DispatchedById",
                table: "Parcels",
                column: "DispatchedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Parcels_Users_ConfirmedById",
                table: "Parcels",
                column: "ConfirmedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Parcels_Users_DeliveredById",
                table: "Parcels",
                column: "DeliveredById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Parcels_Users_DispatchedById",
                table: "Parcels",
                column: "DispatchedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parcels_Users_ConfirmedById",
                table: "Parcels");

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
                keyValue: new Guid("0796a06b-1e70-479b-89d5-25c0da5ef44a"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("0e4a170e-b551-448a-babc-db7406cda302"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("5c8347b5-0fd5-4010-ae94-64df3afcf21f"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("7a624544-bc82-433d-ba2c-57ee7641fccd"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("a88f4c84-cb9b-449c-809e-53dde9c7a2ed"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("be41eb8d-7ced-46ec-8ffa-f91e19de8a94"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("c867c947-3fa6-4515-b34a-94c0b98944d5"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("f05d9f65-2d5e-4fb9-bf32-7747220d9f4e"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("f399279c-5682-483d-83fa-a9967e806b6a"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("f4a1a7e0-ffa1-4370-ab99-cbeeb11b45a4"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("2b9b1b68-ce83-48df-809a-50bc192bcfc6"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("538be3af-24dd-40af-8c21-d390c94c892d"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("8868f970-c944-404c-895b-5bbe75ed3cf7"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("8bfc1d60-0be9-401e-8f01-c73f22c29efc"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("a1046220-0603-4b1e-ab6e-1f5f4fb32e0a"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("e7239c81-41b5-48b3-adc1-3ab6e50cb313"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("ff3be2f6-69a4-4567-be0f-6874781dd765"));

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "DeliveredById",
                table: "Parcels");

            migrationBuilder.DropColumn(
                name: "DispatchedById",
                table: "Parcels");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ConfirmedAt",
                table: "Parcels",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "FirstName", "Initials", "LastName", "LicenseNumber" },
                values: new object[,]
                {
                    { new Guid("3278f9ea-a172-460f-be8d-3fee1e0cd042"), "James", "JG", "Gichohi", "DL123456" },
                    { new Guid("49d1bbbd-ad49-4846-9d8c-194cfa02ca42"), "Julius", "JK", "Kamula", "DL123456" },
                    { new Guid("56334594-b646-4ecf-a7dd-f9a39b04f9b1"), "David", "DK", "Kibet", "DL123456" },
                    { new Guid("5bced61c-2589-4a84-86f6-b42c193f32c7"), "John", "JN", "Njuguna", "DL123456" },
                    { new Guid("64dde835-7ae0-4b42-892f-f273ab8a63ba"), "Erastus", "EK", "Kagwa", "DL123456" },
                    { new Guid("74020777-ccec-4aa4-a014-a3759698ec75"), "Stephen", "SK", "Kimuyu", "DL123456" },
                    { new Guid("82f09f0a-d63c-4e95-aabf-7f5a0661a153"), "Robert", "RN", "Njuguna", "DL123456" },
                    { new Guid("9f719efc-83e7-4d37-993c-ff7de539645c"), "David", "DM", "Mwangi", "DL123456" },
                    { new Guid("e2edf8f9-1314-44dd-86b0-c947bedc9dbd"), "Charles", "CM", "Maina", "DL123456" },
                    { new Guid("e95926c2-bc26-44c2-b709-6d29a9421357"), "John", "JM", "Mwai", "DL123456" }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 42, 231, 183, 52, 246, 66, 2, 234, 156, 53, 90, 52, 70, 18, 104, 182, 48, 186, 242, 17, 127, 221, 80, 18, 250, 128, 158, 18, 93, 241, 88, 178, 100, 43, 83, 205, 175, 212, 151, 74, 70, 192, 188, 63, 152, 153, 46, 211, 154, 97, 62, 232, 154, 127, 43, 173, 24, 71, 208, 9, 43, 153, 59, 231 }, new byte[] { 42, 231, 10, 108, 120, 24, 15, 29, 81, 88, 197, 216, 15, 160, 63, 24, 13, 59, 99, 164, 153, 41, 182, 169, 239, 157, 151, 110, 152, 11, 242, 52, 196, 85, 45, 63, 0, 69, 99, 61, 81, 73, 239, 162, 242, 119, 43, 75, 87, 241, 136, 215, 252, 192, 32, 161, 119, 73, 254, 38, 239, 232, 174, 42, 136, 225, 16, 37, 34, 201, 234, 127, 138, 134, 206, 234, 200, 73, 178, 81, 197, 123, 60, 172, 49, 90, 160, 59, 178, 226, 31, 161, 170, 79, 137, 193, 170, 221, 11, 168, 248, 11, 161, 115, 45, 228, 23, 68, 148, 39, 247, 236, 196, 242, 59, 123, 234, 120, 141, 166, 103, 227, 74, 34, 81, 143, 13, 129 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 42, 231, 183, 52, 246, 66, 2, 234, 156, 53, 90, 52, 70, 18, 104, 182, 48, 186, 242, 17, 127, 221, 80, 18, 250, 128, 158, 18, 93, 241, 88, 178, 100, 43, 83, 205, 175, 212, 151, 74, 70, 192, 188, 63, 152, 153, 46, 211, 154, 97, 62, 232, 154, 127, 43, 173, 24, 71, 208, 9, 43, 153, 59, 231 }, new byte[] { 42, 231, 10, 108, 120, 24, 15, 29, 81, 88, 197, 216, 15, 160, 63, 24, 13, 59, 99, 164, 153, 41, 182, 169, 239, 157, 151, 110, 152, 11, 242, 52, 196, 85, 45, 63, 0, 69, 99, 61, 81, 73, 239, 162, 242, 119, 43, 75, 87, 241, 136, 215, 252, 192, 32, 161, 119, 73, 254, 38, 239, 232, 174, 42, 136, 225, 16, 37, 34, 201, 234, 127, 138, 134, 206, 234, 200, 73, 178, 81, 197, 123, 60, 172, 49, 90, 160, 59, 178, 226, 31, 161, 170, 79, 137, 193, 170, 221, 11, 168, 248, 11, 161, 115, 45, 228, 23, 68, 148, 39, 247, 236, 196, 242, 59, 123, 234, 120, 141, 166, 103, 227, 74, 34, 81, 143, 13, 129 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 42, 231, 183, 52, 246, 66, 2, 234, 156, 53, 90, 52, 70, 18, 104, 182, 48, 186, 242, 17, 127, 221, 80, 18, 250, 128, 158, 18, 93, 241, 88, 178, 100, 43, 83, 205, 175, 212, 151, 74, 70, 192, 188, 63, 152, 153, 46, 211, 154, 97, 62, 232, 154, 127, 43, 173, 24, 71, 208, 9, 43, 153, 59, 231 }, new byte[] { 42, 231, 10, 108, 120, 24, 15, 29, 81, 88, 197, 216, 15, 160, 63, 24, 13, 59, 99, 164, 153, 41, 182, 169, 239, 157, 151, 110, 152, 11, 242, 52, 196, 85, 45, 63, 0, 69, 99, 61, 81, 73, 239, 162, 242, 119, 43, 75, 87, 241, 136, 215, 252, 192, 32, 161, 119, 73, 254, 38, 239, 232, 174, 42, 136, 225, 16, 37, 34, 201, 234, 127, 138, 134, 206, 234, 200, 73, 178, 81, 197, 123, 60, 172, 49, 90, 160, 59, 178, 226, 31, 161, 170, 79, 137, 193, 170, 221, 11, 168, 248, 11, 161, 115, 45, 228, 23, 68, 148, 39, 247, 236, 196, 242, 59, 123, 234, 120, 141, 166, 103, 227, 74, 34, 81, 143, 13, 129 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 42, 231, 183, 52, 246, 66, 2, 234, 156, 53, 90, 52, 70, 18, 104, 182, 48, 186, 242, 17, 127, 221, 80, 18, 250, 128, 158, 18, 93, 241, 88, 178, 100, 43, 83, 205, 175, 212, 151, 74, 70, 192, 188, 63, 152, 153, 46, 211, 154, 97, 62, 232, 154, 127, 43, 173, 24, 71, 208, 9, 43, 153, 59, 231 }, new byte[] { 42, 231, 10, 108, 120, 24, 15, 29, 81, 88, 197, 216, 15, 160, 63, 24, 13, 59, 99, 164, 153, 41, 182, 169, 239, 157, 151, 110, 152, 11, 242, 52, 196, 85, 45, 63, 0, 69, 99, 61, 81, 73, 239, 162, 242, 119, 43, 75, 87, 241, 136, 215, 252, 192, 32, 161, 119, 73, 254, 38, 239, 232, 174, 42, 136, 225, 16, 37, 34, 201, 234, 127, 138, 134, 206, 234, 200, 73, 178, 81, 197, 123, 60, 172, 49, 90, 160, 59, 178, 226, 31, 161, 170, 79, 137, 193, 170, 221, 11, 168, 248, 11, 161, 115, 45, 228, 23, 68, 148, 39, 247, 236, 196, 242, 59, 123, 234, 120, 141, 166, 103, 227, 74, 34, 81, 143, 13, 129 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 42, 231, 183, 52, 246, 66, 2, 234, 156, 53, 90, 52, 70, 18, 104, 182, 48, 186, 242, 17, 127, 221, 80, 18, 250, 128, 158, 18, 93, 241, 88, 178, 100, 43, 83, 205, 175, 212, 151, 74, 70, 192, 188, 63, 152, 153, 46, 211, 154, 97, 62, 232, 154, 127, 43, 173, 24, 71, 208, 9, 43, 153, 59, 231 }, new byte[] { 42, 231, 10, 108, 120, 24, 15, 29, 81, 88, 197, 216, 15, 160, 63, 24, 13, 59, 99, 164, 153, 41, 182, 169, 239, 157, 151, 110, 152, 11, 242, 52, 196, 85, 45, 63, 0, 69, 99, 61, 81, 73, 239, 162, 242, 119, 43, 75, 87, 241, 136, 215, 252, 192, 32, 161, 119, 73, 254, 38, 239, 232, 174, 42, 136, 225, 16, 37, 34, 201, 234, 127, 138, 134, 206, 234, 200, 73, 178, 81, 197, 123, 60, 172, 49, 90, 160, 59, 178, 226, 31, 161, 170, 79, 137, 193, 170, 221, 11, 168, 248, 11, 161, 115, 45, 228, 23, 68, 148, 39, 247, 236, 196, 242, 59, 123, 234, 120, 141, 166, 103, 227, 74, 34, 81, 143, 13, 129 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 42, 231, 183, 52, 246, 66, 2, 234, 156, 53, 90, 52, 70, 18, 104, 182, 48, 186, 242, 17, 127, 221, 80, 18, 250, 128, 158, 18, 93, 241, 88, 178, 100, 43, 83, 205, 175, 212, 151, 74, 70, 192, 188, 63, 152, 153, 46, 211, 154, 97, 62, 232, 154, 127, 43, 173, 24, 71, 208, 9, 43, 153, 59, 231 }, new byte[] { 42, 231, 10, 108, 120, 24, 15, 29, 81, 88, 197, 216, 15, 160, 63, 24, 13, 59, 99, 164, 153, 41, 182, 169, 239, 157, 151, 110, 152, 11, 242, 52, 196, 85, 45, 63, 0, 69, 99, 61, 81, 73, 239, 162, 242, 119, 43, 75, 87, 241, 136, 215, 252, 192, 32, 161, 119, 73, 254, 38, 239, 232, 174, 42, 136, 225, 16, 37, 34, 201, 234, 127, 138, 134, 206, 234, 200, 73, 178, 81, 197, 123, 60, 172, 49, 90, 160, 59, 178, 226, 31, 161, 170, 79, 137, 193, 170, 221, 11, 168, 248, 11, 161, 115, 45, 228, 23, 68, 148, 39, 247, 236, 196, 242, 59, 123, 234, 120, 141, 166, 103, 227, 74, 34, 81, 143, 13, 129 } });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "BodyType", "VehicleRegistrationNumber" },
                values: new object[,]
                {
                    { new Guid("43d5101d-8535-4df7-aa15-d52ce34f8206"), "Truck", "KCZ 595L" },
                    { new Guid("47e5800a-8d59-43d6-ab1d-3584c46a8eb6"), "Truck", "KDE 228S" },
                    { new Guid("55b70b65-a894-4e29-82b1-8dbbcae52310"), "Truck", "KDB 387Q" },
                    { new Guid("8693033c-adfd-4f20-967c-b9ceabcd0bdb"), "Van", "KBF 462A" },
                    { new Guid("ac62557e-c804-4a62-9983-4d05e54c49ef"), "Truck", "KCY 067A" },
                    { new Guid("cb5f1a6f-961b-44fa-82ac-4366824dd764"), "Truck", "KDL 085M" },
                    { new Guid("fa15d8b7-a262-4bca-b9d0-84d9e1ccd8bf"), "Van", "KAY 215H" }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Parcels_Users_ConfirmedById",
                table: "Parcels",
                column: "ConfirmedById",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
