using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace wms_android.api.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    { new Guid("434a0380-76b5-4836-a70a-938769a3a090"), "Erastus", "EK", "Kagwa", "DL123456" },
                    { new Guid("49c26444-6381-4f1e-91e4-199a92ad521b"), "Stephen", "SK", "Kimuyu", "DL123456" },
                    { new Guid("5fa968b1-e1d5-4207-8cbb-499b337cd3ab"), "Charles", "CM", "Maina", "DL123456" },
                    { new Guid("685f4b1a-0a79-41a8-8f3b-5db2e7f6f097"), "Julius", "JK", "Kamula", "DL123456" },
                    { new Guid("8aec772f-812b-46b4-96bd-4fd07885f094"), "David", "DM", "Mwangi", "DL123456" },
                    { new Guid("ae2d1e5e-704f-4c97-9a64-1f48b1bf9c13"), "Robert", "RN", "Njuguna", "DL123456" },
                    { new Guid("e2d3e3d3-7f02-4209-a527-cc7f1d5cc84c"), "John", "JM", "Mwai", "DL123456" },
                    { new Guid("eaf359d1-3e0e-4bc8-b380-6686ad1efcce"), "James", "JG", "Gichohi", "DL123456" },
                    { new Guid("efbcc10e-95cc-417d-bfd0-630f9980204f"), "David", "DK", "Kibet", "DL123456" },
                    { new Guid("f7c42fef-e182-4ded-9dcf-891a3d355cbd"), "John", "JN", "Njuguna", "DL123456" }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 50, 154, 69, 102, 83, 40, 28, 137, 180, 61, 109, 159, 181, 51, 30, 195, 144, 184, 243, 185, 118, 100, 146, 251, 121, 252, 129, 22, 113, 191, 106, 171, 2, 35, 235, 15, 253, 195, 35, 17, 97, 44, 114, 169, 67, 238, 144, 105, 44, 130, 40, 9, 60, 112, 24, 213, 231, 253, 73, 181, 25, 131, 100, 253 }, new byte[] { 232, 111, 194, 50, 228, 232, 134, 98, 3, 194, 212, 82, 227, 37, 164, 228, 16, 241, 133, 171, 46, 61, 208, 173, 34, 130, 26, 185, 31, 212, 239, 221, 192, 120, 224, 80, 23, 247, 204, 181, 77, 155, 195, 40, 4, 71, 181, 169, 162, 75, 180, 123, 205, 76, 161, 40, 39, 96, 134, 251, 166, 253, 77, 71, 223, 155, 85, 69, 248, 79, 153, 65, 227, 103, 97, 86, 215, 23, 99, 61, 25, 82, 49, 3, 127, 220, 238, 31, 181, 31, 148, 200, 99, 46, 97, 34, 100, 25, 91, 93, 173, 161, 176, 3, 219, 55, 87, 220, 235, 168, 229, 146, 62, 128, 177, 124, 63, 195, 101, 136, 178, 20, 238, 36, 127, 176, 16, 135 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 50, 154, 69, 102, 83, 40, 28, 137, 180, 61, 109, 159, 181, 51, 30, 195, 144, 184, 243, 185, 118, 100, 146, 251, 121, 252, 129, 22, 113, 191, 106, 171, 2, 35, 235, 15, 253, 195, 35, 17, 97, 44, 114, 169, 67, 238, 144, 105, 44, 130, 40, 9, 60, 112, 24, 213, 231, 253, 73, 181, 25, 131, 100, 253 }, new byte[] { 232, 111, 194, 50, 228, 232, 134, 98, 3, 194, 212, 82, 227, 37, 164, 228, 16, 241, 133, 171, 46, 61, 208, 173, 34, 130, 26, 185, 31, 212, 239, 221, 192, 120, 224, 80, 23, 247, 204, 181, 77, 155, 195, 40, 4, 71, 181, 169, 162, 75, 180, 123, 205, 76, 161, 40, 39, 96, 134, 251, 166, 253, 77, 71, 223, 155, 85, 69, 248, 79, 153, 65, 227, 103, 97, 86, 215, 23, 99, 61, 25, 82, 49, 3, 127, 220, 238, 31, 181, 31, 148, 200, 99, 46, 97, 34, 100, 25, 91, 93, 173, 161, 176, 3, 219, 55, 87, 220, 235, 168, 229, 146, 62, 128, 177, 124, 63, 195, 101, 136, 178, 20, 238, 36, 127, 176, 16, 135 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 50, 154, 69, 102, 83, 40, 28, 137, 180, 61, 109, 159, 181, 51, 30, 195, 144, 184, 243, 185, 118, 100, 146, 251, 121, 252, 129, 22, 113, 191, 106, 171, 2, 35, 235, 15, 253, 195, 35, 17, 97, 44, 114, 169, 67, 238, 144, 105, 44, 130, 40, 9, 60, 112, 24, 213, 231, 253, 73, 181, 25, 131, 100, 253 }, new byte[] { 232, 111, 194, 50, 228, 232, 134, 98, 3, 194, 212, 82, 227, 37, 164, 228, 16, 241, 133, 171, 46, 61, 208, 173, 34, 130, 26, 185, 31, 212, 239, 221, 192, 120, 224, 80, 23, 247, 204, 181, 77, 155, 195, 40, 4, 71, 181, 169, 162, 75, 180, 123, 205, 76, 161, 40, 39, 96, 134, 251, 166, 253, 77, 71, 223, 155, 85, 69, 248, 79, 153, 65, 227, 103, 97, 86, 215, 23, 99, 61, 25, 82, 49, 3, 127, 220, 238, 31, 181, 31, 148, 200, 99, 46, 97, 34, 100, 25, 91, 93, 173, 161, 176, 3, 219, 55, 87, 220, 235, 168, 229, 146, 62, 128, 177, 124, 63, 195, 101, 136, 178, 20, 238, 36, 127, 176, 16, 135 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 50, 154, 69, 102, 83, 40, 28, 137, 180, 61, 109, 159, 181, 51, 30, 195, 144, 184, 243, 185, 118, 100, 146, 251, 121, 252, 129, 22, 113, 191, 106, 171, 2, 35, 235, 15, 253, 195, 35, 17, 97, 44, 114, 169, 67, 238, 144, 105, 44, 130, 40, 9, 60, 112, 24, 213, 231, 253, 73, 181, 25, 131, 100, 253 }, new byte[] { 232, 111, 194, 50, 228, 232, 134, 98, 3, 194, 212, 82, 227, 37, 164, 228, 16, 241, 133, 171, 46, 61, 208, 173, 34, 130, 26, 185, 31, 212, 239, 221, 192, 120, 224, 80, 23, 247, 204, 181, 77, 155, 195, 40, 4, 71, 181, 169, 162, 75, 180, 123, 205, 76, 161, 40, 39, 96, 134, 251, 166, 253, 77, 71, 223, 155, 85, 69, 248, 79, 153, 65, 227, 103, 97, 86, 215, 23, 99, 61, 25, 82, 49, 3, 127, 220, 238, 31, 181, 31, 148, 200, 99, 46, 97, 34, 100, 25, 91, 93, 173, 161, 176, 3, 219, 55, 87, 220, 235, 168, 229, 146, 62, 128, 177, 124, 63, 195, 101, 136, 178, 20, 238, 36, 127, 176, 16, 135 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 50, 154, 69, 102, 83, 40, 28, 137, 180, 61, 109, 159, 181, 51, 30, 195, 144, 184, 243, 185, 118, 100, 146, 251, 121, 252, 129, 22, 113, 191, 106, 171, 2, 35, 235, 15, 253, 195, 35, 17, 97, 44, 114, 169, 67, 238, 144, 105, 44, 130, 40, 9, 60, 112, 24, 213, 231, 253, 73, 181, 25, 131, 100, 253 }, new byte[] { 232, 111, 194, 50, 228, 232, 134, 98, 3, 194, 212, 82, 227, 37, 164, 228, 16, 241, 133, 171, 46, 61, 208, 173, 34, 130, 26, 185, 31, 212, 239, 221, 192, 120, 224, 80, 23, 247, 204, 181, 77, 155, 195, 40, 4, 71, 181, 169, 162, 75, 180, 123, 205, 76, 161, 40, 39, 96, 134, 251, 166, 253, 77, 71, 223, 155, 85, 69, 248, 79, 153, 65, 227, 103, 97, 86, 215, 23, 99, 61, 25, 82, 49, 3, 127, 220, 238, 31, 181, 31, 148, 200, 99, 46, 97, 34, 100, 25, 91, 93, 173, 161, 176, 3, 219, 55, 87, 220, 235, 168, 229, 146, 62, 128, 177, 124, 63, 195, 101, 136, 178, 20, 238, 36, 127, 176, 16, 135 } });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "PasswordHash", "PasswordSalt" },
                values: new object[] { new byte[] { 50, 154, 69, 102, 83, 40, 28, 137, 180, 61, 109, 159, 181, 51, 30, 195, 144, 184, 243, 185, 118, 100, 146, 251, 121, 252, 129, 22, 113, 191, 106, 171, 2, 35, 235, 15, 253, 195, 35, 17, 97, 44, 114, 169, 67, 238, 144, 105, 44, 130, 40, 9, 60, 112, 24, 213, 231, 253, 73, 181, 25, 131, 100, 253 }, new byte[] { 232, 111, 194, 50, 228, 232, 134, 98, 3, 194, 212, 82, 227, 37, 164, 228, 16, 241, 133, 171, 46, 61, 208, 173, 34, 130, 26, 185, 31, 212, 239, 221, 192, 120, 224, 80, 23, 247, 204, 181, 77, 155, 195, 40, 4, 71, 181, 169, 162, 75, 180, 123, 205, 76, 161, 40, 39, 96, 134, 251, 166, 253, 77, 71, 223, 155, 85, 69, 248, 79, 153, 65, 227, 103, 97, 86, 215, 23, 99, 61, 25, 82, 49, 3, 127, 220, 238, 31, 181, 31, 148, 200, 99, 46, 97, 34, 100, 25, 91, 93, 173, 161, 176, 3, 219, 55, 87, 220, 235, 168, 229, 146, 62, 128, 177, 124, 63, 195, 101, 136, 178, 20, 238, 36, 127, 176, 16, 135 } });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "BodyType", "VehicleRegistrationNumber" },
                values: new object[,]
                {
                    { new Guid("21bd0322-9744-467f-80d9-225a80eff2f8"), "Truck", "KDB 387Q" },
                    { new Guid("7cd332a5-6c6d-4edd-8799-7434b47c0adc"), "Van", "KBF 462A" },
                    { new Guid("8624ea18-9810-4d09-8176-f7a5ab2b2884"), "Truck", "KDE 228S" },
                    { new Guid("bd592b26-e041-4c8c-9e44-6be82f1ec84b"), "Truck", "KCZ 595L" },
                    { new Guid("d6ea277a-b043-4e37-a919-a59969c63ecc"), "Truck", "KDL 085M" },
                    { new Guid("f8047722-556c-43e6-a893-2239e614280a"), "Truck", "KCY 067A" },
                    { new Guid("fdeaa2a0-2081-42aa-a518-5658ffad022f"), "Van", "KAY 215H" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_CreatedAt",
                table: "Parcels",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_Destination",
                table: "Parcels",
                column: "Destination");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_Destination_Status",
                table: "Parcels",
                columns: new[] { "Destination", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_DispatchTrackingCode",
                table: "Parcels",
                column: "DispatchTrackingCode");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_Status",
                table: "Parcels",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_Status_CreatedAt",
                table: "Parcels",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchSegments_Destination",
                table: "DispatchSegments",
                column: "Destination");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchSegments_DispatchId_SequenceNumber",
                table: "DispatchSegments",
                columns: new[] { "DispatchId", "SequenceNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Parcels_CreatedAt",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_Destination",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_Destination_Status",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_DispatchTrackingCode",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_Status",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_Status_CreatedAt",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_DispatchSegments_Destination",
                table: "DispatchSegments");

            migrationBuilder.DropIndex(
                name: "IX_DispatchSegments_DispatchId_SequenceNumber",
                table: "DispatchSegments");

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("434a0380-76b5-4836-a70a-938769a3a090"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("49c26444-6381-4f1e-91e4-199a92ad521b"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("5fa968b1-e1d5-4207-8cbb-499b337cd3ab"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("685f4b1a-0a79-41a8-8f3b-5db2e7f6f097"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("8aec772f-812b-46b4-96bd-4fd07885f094"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("ae2d1e5e-704f-4c97-9a64-1f48b1bf9c13"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("e2d3e3d3-7f02-4209-a527-cc7f1d5cc84c"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("eaf359d1-3e0e-4bc8-b380-6686ad1efcce"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("efbcc10e-95cc-417d-bfd0-630f9980204f"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("f7c42fef-e182-4ded-9dcf-891a3d355cbd"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("21bd0322-9744-467f-80d9-225a80eff2f8"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("7cd332a5-6c6d-4edd-8799-7434b47c0adc"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("8624ea18-9810-4d09-8176-f7a5ab2b2884"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("bd592b26-e041-4c8c-9e44-6be82f1ec84b"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("d6ea277a-b043-4e37-a919-a59969c63ecc"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("f8047722-556c-43e6-a893-2239e614280a"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("fdeaa2a0-2081-42aa-a518-5658ffad022f"));

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
        }
    }
}
