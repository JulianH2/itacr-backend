using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Itacr.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedCatalogData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "Name", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Combos", 1, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000002"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Hamburguesas", 2, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000003"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Complementos", 3, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000004"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bebidas", 4, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000005"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Postres", 5, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ComboOptions",
                columns: new[] { "Id", "CreatedAt", "Name", "PriceAdjustment", "Type", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000001"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Chico", 0m, "Size", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("30000000-0000-0000-0000-000000000002"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mediano", 15m, "Size", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("30000000-0000-0000-0000-000000000003"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Grande", 25m, "Size", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("30000000-0000-0000-0000-000000000004"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Refresco de cola", 0m, "Drink", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("30000000-0000-0000-0000-000000000005"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Refresco de lima-limón", 0m, "Drink", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("30000000-0000-0000-0000-000000000006"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Agua natural", 0m, "Drink", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("30000000-0000-0000-0000-000000000007"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Queso extra", 12m, "Extra", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("30000000-0000-0000-0000-000000000008"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tocino", 18m, "Extra", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("30000000-0000-0000-0000-000000000009"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Jalapeños", 8m, "Extra", new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "DiningTables",
                columns: new[] { "Id", "CreatedAt", "Number", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000001"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("40000000-0000-0000-0000-000000000002"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("40000000-0000-0000-0000-000000000003"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("40000000-0000-0000-0000-000000000004"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("40000000-0000-0000-0000-000000000005"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("40000000-0000-0000-0000-000000000006"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("40000000-0000-0000-0000-000000000007"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("40000000-0000-0000-0000-000000000008"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("40000000-0000-0000-0000-000000000009"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), 9, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("40000000-0000-0000-0000-000000000010"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "CategoryId", "CreatedAt", "Description", "ImageUrl", "IsCombo", "Name", "Price", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000001"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Hamburguesa clásica, papas a la francesa y refresco.", "/assets/images/products/combo-clasico.png", true, "Combo Clásico", 119m, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000001"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Hamburguesa doble carne, papas a la francesa y refresco.", "/assets/images/products/combo-doble.png", true, "Combo Doble", 149m, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000001"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Hamburguesa de pollo crujiente, papas a la francesa y refresco.", "/assets/images/products/combo-pollo.png", true, "Combo Pollo", 129m, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000001"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mini hamburguesa, papas a la francesa y jugo.", "/assets/images/products/combo-infantil.png", true, "Combo Infantil", 89m, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000002"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Carne de res, lechuga, jitomate y cebolla.", "/assets/images/products/hamburguesa-clasica.png", false, "Hamburguesa Clásica", 79m, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000002"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Carne de res, queso americano y aderezo de la casa.", "/assets/images/products/hamburguesa-con-queso.png", false, "Hamburguesa con Queso", 89m, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000002"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Doble carne de res con doble queso.", "/assets/images/products/hamburguesa-doble.png", false, "Hamburguesa Doble", 109m, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000002"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pechuga de pollo empanizada con lechuga y mayonesa.", "/assets/images/products/hamburguesa-de-pollo.png", false, "Hamburguesa de Pollo", 95m, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000003"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Porción de papas fritas con sal.", "/assets/images/products/papas-a-la-francesa.png", false, "Papas a la Francesa", 39m, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000003"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Aros de cebolla empanizados y crujientes.", "/assets/images/products/aros-de-cebolla.png", false, "Aros de Cebolla", 45m, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000003"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seis piezas de nuggets con salsa a elegir.", "/assets/images/products/nuggets-de-pollo.png", false, "Nuggets de Pollo", 55m, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000004"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Refresco de cola de 355 ml.", "/assets/images/products/refresco-de-cola.png", false, "Refresco de Cola", 29m, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000004"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Refresco de lima-limón de 355 ml.", "/assets/images/products/refresco-de-lima-limon.png", false, "Refresco de Lima-Limón", 29m, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000004"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Botella de agua natural de 600 ml.", "/assets/images/products/agua-natural.png", false, "Agua Natural", 20m, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000004"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Limonada natural preparada al momento.", "/assets/images/products/limonada.png", false, "Limonada", 32m, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000005"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Copa de helado de vainilla.", "/assets/images/products/helado-de-vainilla.png", false, "Helado de Vainilla", 35m, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000005"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pay caliente de manzana con canela.", "/assets/images/products/pay-de-manzana.png", false, "Pay de Manzana", 38m, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000005"), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Brownie de chocolate con una bola de helado.", "/assets/images/products/brownie-con-helado.png", false, "Brownie con Helado", 49m, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ComboOptions",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "ComboOptions",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "ComboOptions",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "ComboOptions",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "ComboOptions",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "ComboOptions",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "ComboOptions",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "ComboOptions",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "ComboOptions",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "DiningTables",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000018"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"));
        }
    }
}
