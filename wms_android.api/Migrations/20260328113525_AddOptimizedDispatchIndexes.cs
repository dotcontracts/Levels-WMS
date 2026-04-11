using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace wms_android.api.Migrations
{
    /// <inheritdoc />
    public partial class AddOptimizedDispatchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_Destination_Status_CreatedAt",
                table: "Parcels",
                columns: new[] { "Destination", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Parcels_Status_CreatedAt_CreatedById",
                table: "Parcels",
                columns: new[] { "Status", "CreatedAt", "CreatedById" });

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_DispatchTime",
                table: "Dispatches",
                column: "DispatchTime");

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_DispatchTime_Status",
                table: "Dispatches",
                columns: new[] { "DispatchTime", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_SourceBranch_DispatchTime",
                table: "Dispatches",
                columns: new[] { "SourceBranch", "DispatchTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_Status",
                table: "Dispatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Dispatches_VehicleNumber",
                table: "Dispatches",
                column: "VehicleNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Parcels_Destination_Status_CreatedAt",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Parcels_Status_CreatedAt_CreatedById",
                table: "Parcels");

            migrationBuilder.DropIndex(
                name: "IX_Dispatches_DispatchTime",
                table: "Dispatches");

            migrationBuilder.DropIndex(
                name: "IX_Dispatches_DispatchTime_Status",
                table: "Dispatches");

            migrationBuilder.DropIndex(
                name: "IX_Dispatches_SourceBranch_DispatchTime",
                table: "Dispatches");

            migrationBuilder.DropIndex(
                name: "IX_Dispatches_Status",
                table: "Dispatches");

            migrationBuilder.DropIndex(
                name: "IX_Dispatches_VehicleNumber",
                table: "Dispatches");

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
        }
    }
}
