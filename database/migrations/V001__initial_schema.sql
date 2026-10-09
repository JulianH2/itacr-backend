-- =============================================================================
-- Migration:   V001__initial_schema
-- Purpose:     Schema at the end of sprint 1, as created by the EF Core migrations up to
--              20261002060049_AddPinLockoutAndPinResetSubject, including the catalog seed data.
--              dbo.fn_GetProductsByCategory lives in database/functions/Catalog/.
--              On a database created by EF Core this script is marked as executed, never run.
-- Author:      JulianH2
-- Jira:        ITACR-37
-- =============================================================================

-- Tables, sequence, keys, and indexes (EF Core migration 20261002050918_InitialCreate)
CREATE SEQUENCE [OrderNumbers] START WITH 1 INCREMENT BY 1 NO CYCLE;

CREATE TABLE [Categories] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(60) NOT NULL,
    [SortOrder] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
);

CREATE TABLE [ComboOptions] (
    [Id] uniqueidentifier NOT NULL,
    [Type] nvarchar(20) NOT NULL,
    [Name] nvarchar(60) NOT NULL,
    [PriceAdjustment] decimal(10,2) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ComboOptions] PRIMARY KEY ([Id])
);

CREATE TABLE [Customers] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(120) NOT NULL,
    [Phone] nvarchar(10) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
);

CREATE TABLE [DiningTables] (
    [Id] uniqueidentifier NOT NULL,
    [Number] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_DiningTables] PRIMARY KEY ([Id])
);

CREATE TABLE [Employees] (
    [Id] uniqueidentifier NOT NULL,
    [FullName] nvarchar(120) NOT NULL,
    [Email] nvarchar(254) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Role] nvarchar(20) NOT NULL,
    [PinHash] nvarchar(255) NOT NULL,
    [PasswordHash] nvarchar(255) NULL,
    [Status] nvarchar(20) NOT NULL,
    [ApprovedById] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Employees_Employees_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Products] (
    [Id] uniqueidentifier NOT NULL,
    [CategoryId] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [Price] decimal(10,2) NOT NULL,
    [ImageUrl] nvarchar(500) NULL,
    [IsCombo] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Orders] (
    [Id] uniqueidentifier NOT NULL,
    [ClientOrderId] uniqueidentifier NOT NULL,
    [OrderNumber] int NOT NULL DEFAULT (NEXT VALUE FOR [OrderNumbers]),
    [Channel] nvarchar(20) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [TableId] uniqueidentifier NULL,
    [WaiterId] uniqueidentifier NULL,
    [CustomerId] uniqueidentifier NULL,
    [Total] decimal(10,2) NOT NULL,
    [LastStatusChangeAt] datetime2 NOT NULL,
    [ReadyAt] datetime2 NULL,
    [DeliveredAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Orders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Orders_DiningTables_TableId] FOREIGN KEY ([TableId]) REFERENCES [DiningTables] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Orders_Employees_WaiterId] FOREIGN KEY ([WaiterId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PushSubscriptions] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NULL,
    [CustomerId] uniqueidentifier NULL,
    [Endpoint] nvarchar(450) NOT NULL,
    [P256dh] nvarchar(200) NOT NULL,
    [Auth] nvarchar(200) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PushSubscriptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PushSubscriptions_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PushSubscriptions_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Notifications] (
    [Id] uniqueidentifier NOT NULL,
    [Type] nvarchar(30) NOT NULL,
    [RecipientEmployeeId] uniqueidentifier NULL,
    [RecipientCustomerId] uniqueidentifier NULL,
    [OrderId] uniqueidentifier NULL,
    [SentAt] datetime2 NOT NULL,
    [ReadAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_Customers_RecipientCustomerId] FOREIGN KEY ([RecipientCustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Notifications_Employees_RecipientEmployeeId] FOREIGN KEY ([RecipientEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Notifications_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OrderItems] (
    [Id] uniqueidentifier NOT NULL,
    [OrderId] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(10,2) NOT NULL,
    [Subtotal] decimal(10,2) NOT NULL,
    [Size] nvarchar(60) NULL,
    [Drink] nvarchar(60) NULL,
    [Extras] json NOT NULL,
    [Exclusions] json NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OrderNotes] (
    [Id] uniqueidentifier NOT NULL,
    [OrderId] uniqueidentifier NOT NULL,
    [Text] nvarchar(500) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_OrderNotes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderNotes_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);

CREATE UNIQUE INDEX [IX_ComboOptions_Type_Name] ON [ComboOptions] ([Type], [Name]);

CREATE UNIQUE INDEX [IX_DiningTables_Number] ON [DiningTables] ([Number]);

CREATE INDEX [IX_Employees_ApprovedById] ON [Employees] ([ApprovedById]);

CREATE UNIQUE INDEX [IX_Employees_Email] ON [Employees] ([Email]);

CREATE INDEX [IX_Employees_Status] ON [Employees] ([Status]);

CREATE INDEX [IX_Notifications_OrderId] ON [Notifications] ([OrderId]);

CREATE INDEX [IX_Notifications_RecipientCustomerId] ON [Notifications] ([RecipientCustomerId]);

CREATE INDEX [IX_Notifications_RecipientEmployeeId] ON [Notifications] ([RecipientEmployeeId]);

CREATE INDEX [IX_Notifications_Type] ON [Notifications] ([Type]);

CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);

CREATE INDEX [IX_OrderItems_ProductId] ON [OrderItems] ([ProductId]);

CREATE INDEX [IX_OrderNotes_OrderId] ON [OrderNotes] ([OrderId]);

CREATE UNIQUE INDEX [IX_Orders_ClientOrderId] ON [Orders] ([ClientOrderId]);

CREATE INDEX [IX_Orders_CreatedAt] ON [Orders] ([CreatedAt]);

CREATE INDEX [IX_Orders_CustomerId] ON [Orders] ([CustomerId]);

CREATE UNIQUE INDEX [IX_Orders_OrderNumber] ON [Orders] ([OrderNumber]);

CREATE INDEX [IX_Orders_Status] ON [Orders] ([Status]);

CREATE INDEX [IX_Orders_TableId] ON [Orders] ([TableId]);

CREATE INDEX [IX_Orders_WaiterId] ON [Orders] ([WaiterId]);

CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);

CREATE INDEX [IX_PushSubscriptions_CustomerId] ON [PushSubscriptions] ([CustomerId]);

CREATE INDEX [IX_PushSubscriptions_EmployeeId] ON [PushSubscriptions] ([EmployeeId]);

CREATE UNIQUE INDEX [IX_PushSubscriptions_Endpoint] ON [PushSubscriptions] ([Endpoint]);
GO

-- Catalog seed data (EF Core migration 20261002052403_SeedCatalogData)
INSERT INTO [Categories] ([Id], [CreatedAt], [Name], [SortOrder], [UpdatedAt])
VALUES ('10000000-0000-0000-0000-000000000001', '2026-10-01T00:00:00.0000000Z', N'Combos', 1, '2026-10-01T00:00:00.0000000Z'),
('10000000-0000-0000-0000-000000000002', '2026-10-01T00:00:00.0000000Z', N'Hamburguesas', 2, '2026-10-01T00:00:00.0000000Z'),
('10000000-0000-0000-0000-000000000003', '2026-10-01T00:00:00.0000000Z', N'Complementos', 3, '2026-10-01T00:00:00.0000000Z'),
('10000000-0000-0000-0000-000000000004', '2026-10-01T00:00:00.0000000Z', N'Bebidas', 4, '2026-10-01T00:00:00.0000000Z'),
('10000000-0000-0000-0000-000000000005', '2026-10-01T00:00:00.0000000Z', N'Postres', 5, '2026-10-01T00:00:00.0000000Z');

INSERT INTO [ComboOptions] ([Id], [CreatedAt], [Name], [PriceAdjustment], [Type], [UpdatedAt])
VALUES ('30000000-0000-0000-0000-000000000001', '2026-10-01T00:00:00.0000000Z', N'Chico', 0.0, N'Size', '2026-10-01T00:00:00.0000000Z'),
('30000000-0000-0000-0000-000000000002', '2026-10-01T00:00:00.0000000Z', N'Mediano', 15.0, N'Size', '2026-10-01T00:00:00.0000000Z'),
('30000000-0000-0000-0000-000000000003', '2026-10-01T00:00:00.0000000Z', N'Grande', 25.0, N'Size', '2026-10-01T00:00:00.0000000Z'),
('30000000-0000-0000-0000-000000000004', '2026-10-01T00:00:00.0000000Z', N'Refresco de cola', 0.0, N'Drink', '2026-10-01T00:00:00.0000000Z'),
('30000000-0000-0000-0000-000000000005', '2026-10-01T00:00:00.0000000Z', N'Refresco de lima-limón', 0.0, N'Drink', '2026-10-01T00:00:00.0000000Z'),
('30000000-0000-0000-0000-000000000006', '2026-10-01T00:00:00.0000000Z', N'Agua natural', 0.0, N'Drink', '2026-10-01T00:00:00.0000000Z'),
('30000000-0000-0000-0000-000000000007', '2026-10-01T00:00:00.0000000Z', N'Queso extra', 12.0, N'Extra', '2026-10-01T00:00:00.0000000Z'),
('30000000-0000-0000-0000-000000000008', '2026-10-01T00:00:00.0000000Z', N'Tocino', 18.0, N'Extra', '2026-10-01T00:00:00.0000000Z'),
('30000000-0000-0000-0000-000000000009', '2026-10-01T00:00:00.0000000Z', N'Jalapeños', 8.0, N'Extra', '2026-10-01T00:00:00.0000000Z');

INSERT INTO [DiningTables] ([Id], [CreatedAt], [Number], [UpdatedAt])
VALUES ('40000000-0000-0000-0000-000000000001', '2026-10-01T00:00:00.0000000Z', 1, '2026-10-01T00:00:00.0000000Z'),
('40000000-0000-0000-0000-000000000002', '2026-10-01T00:00:00.0000000Z', 2, '2026-10-01T00:00:00.0000000Z'),
('40000000-0000-0000-0000-000000000003', '2026-10-01T00:00:00.0000000Z', 3, '2026-10-01T00:00:00.0000000Z'),
('40000000-0000-0000-0000-000000000004', '2026-10-01T00:00:00.0000000Z', 4, '2026-10-01T00:00:00.0000000Z'),
('40000000-0000-0000-0000-000000000005', '2026-10-01T00:00:00.0000000Z', 5, '2026-10-01T00:00:00.0000000Z'),
('40000000-0000-0000-0000-000000000006', '2026-10-01T00:00:00.0000000Z', 6, '2026-10-01T00:00:00.0000000Z'),
('40000000-0000-0000-0000-000000000007', '2026-10-01T00:00:00.0000000Z', 7, '2026-10-01T00:00:00.0000000Z'),
('40000000-0000-0000-0000-000000000008', '2026-10-01T00:00:00.0000000Z', 8, '2026-10-01T00:00:00.0000000Z'),
('40000000-0000-0000-0000-000000000009', '2026-10-01T00:00:00.0000000Z', 9, '2026-10-01T00:00:00.0000000Z'),
('40000000-0000-0000-0000-000000000010', '2026-10-01T00:00:00.0000000Z', 10, '2026-10-01T00:00:00.0000000Z');

INSERT INTO [Products] ([Id], [CategoryId], [CreatedAt], [Description], [ImageUrl], [IsCombo], [Name], [Price], [UpdatedAt])
VALUES ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', '2026-10-01T00:00:00.0000000Z', N'Hamburguesa clásica, papas a la francesa y refresco.', N'/assets/images/products/combo-clasico.png', CAST(1 AS bit), N'Combo Clásico', 119.0, '2026-10-01T00:00:00.0000000Z'),
('20000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', '2026-10-01T00:00:00.0000000Z', N'Hamburguesa doble carne, papas a la francesa y refresco.', N'/assets/images/products/combo-doble.png', CAST(1 AS bit), N'Combo Doble', 149.0, '2026-10-01T00:00:00.0000000Z'),
('20000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000001', '2026-10-01T00:00:00.0000000Z', N'Hamburguesa de pollo crujiente, papas a la francesa y refresco.', N'/assets/images/products/combo-pollo.png', CAST(1 AS bit), N'Combo Pollo', 129.0, '2026-10-01T00:00:00.0000000Z'),
('20000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000001', '2026-10-01T00:00:00.0000000Z', N'Mini hamburguesa, papas a la francesa y jugo.', N'/assets/images/products/combo-infantil.png', CAST(1 AS bit), N'Combo Infantil', 89.0, '2026-10-01T00:00:00.0000000Z'),
('20000000-0000-0000-0000-000000000005', '10000000-0000-0000-0000-000000000002', '2026-10-01T00:00:00.0000000Z', N'Carne de res, lechuga, jitomate y cebolla.', N'/assets/images/products/hamburguesa-clasica.png', CAST(0 AS bit), N'Hamburguesa Clásica', 79.0, '2026-10-01T00:00:00.0000000Z'),
('20000000-0000-0000-0000-000000000006', '10000000-0000-0000-0000-000000000002', '2026-10-01T00:00:00.0000000Z', N'Carne de res, queso americano y aderezo de la casa.', N'/assets/images/products/hamburguesa-con-queso.png', CAST(0 AS bit), N'Hamburguesa con Queso', 89.0, '2026-10-01T00:00:00.0000000Z'),
('20000000-0000-0000-0000-000000000007', '10000000-0000-0000-0000-000000000002', '2026-10-01T00:00:00.0000000Z', N'Doble carne de res con doble queso.', N'/assets/images/products/hamburguesa-doble.png', CAST(0 AS bit), N'Hamburguesa Doble', 109.0, '2026-10-01T00:00:00.0000000Z'),
('20000000-0000-0000-0000-000000000008', '10000000-0000-0000-0000-000000000002', '2026-10-01T00:00:00.0000000Z', N'Pechuga de pollo empanizada con lechuga y mayonesa.', N'/assets/images/products/hamburguesa-de-pollo.png', CAST(0 AS bit), N'Hamburguesa de Pollo', 95.0, '2026-10-01T00:00:00.0000000Z'),
('20000000-0000-0000-0000-000000000009', '10000000-0000-0000-0000-000000000003', '2026-10-01T00:00:00.0000000Z', N'Porción de papas fritas con sal.', N'/assets/images/products/papas-a-la-francesa.png', CAST(0 AS bit), N'Papas a la Francesa', 39.0, '2026-10-01T00:00:00.0000000Z'),
('20000000-0000-0000-0000-000000000010', '10000000-0000-0000-0000-000000000003', '2026-10-01T00:00:00.0000000Z', N'Aros de cebolla empanizados y crujientes.', N'/assets/images/products/aros-de-cebolla.png', CAST(0 AS bit), N'Aros de Cebolla', 45.0, '2026-10-01T00:00:00.0000000Z'),
('20000000-0000-0000-0000-000000000011', '10000000-0000-0000-0000-000000000003', '2026-10-01T00:00:00.0000000Z', N'Seis piezas de nuggets con salsa a elegir.', N'/assets/images/products/nuggets-de-pollo.png', CAST(0 AS bit), N'Nuggets de Pollo', 55.0, '2026-10-01T00:00:00.0000000Z'),
('20000000-0000-0000-0000-000000000012', '10000000-0000-0000-0000-000000000004', '2026-10-01T00:00:00.0000000Z', N'Refresco de cola de 355 ml.', N'/assets/images/products/refresco-de-cola.png', CAST(0 AS bit), N'Refresco de Cola', 29.0, '2026-10-01T00:00:00.0000000Z'),
('20000000-0000-0000-0000-000000000013', '10000000-0000-0000-0000-000000000004', '2026-10-01T00:00:00.0000000Z', N'Refresco de lima-limón de 355 ml.', N'/assets/images/products/refresco-de-lima-limon.png', CAST(0 AS bit), N'Refresco de Lima-Limón', 29.0, '2026-10-01T00:00:00.0000000Z'),
('20000000-0000-0000-0000-000000000014', '10000000-0000-0000-0000-000000000004', '2026-10-01T00:00:00.0000000Z', N'Botella de agua natural de 600 ml.', N'/assets/images/products/agua-natural.png', CAST(0 AS bit), N'Agua Natural', 20.0, '2026-10-01T00:00:00.0000000Z'),
('20000000-0000-0000-0000-000000000015', '10000000-0000-0000-0000-000000000004', '2026-10-01T00:00:00.0000000Z', N'Limonada natural preparada al momento.', N'/assets/images/products/limonada.png', CAST(0 AS bit), N'Limonada', 32.0, '2026-10-01T00:00:00.0000000Z'),
('20000000-0000-0000-0000-000000000016', '10000000-0000-0000-0000-000000000005', '2026-10-01T00:00:00.0000000Z', N'Copa de helado de vainilla.', N'/assets/images/products/helado-de-vainilla.png', CAST(0 AS bit), N'Helado de Vainilla', 35.0, '2026-10-01T00:00:00.0000000Z'),
('20000000-0000-0000-0000-000000000017', '10000000-0000-0000-0000-000000000005', '2026-10-01T00:00:00.0000000Z', N'Pay caliente de manzana con canela.', N'/assets/images/products/pay-de-manzana.png', CAST(0 AS bit), N'Pay de Manzana', 38.0, '2026-10-01T00:00:00.0000000Z'),
('20000000-0000-0000-0000-000000000018', '10000000-0000-0000-0000-000000000005', '2026-10-01T00:00:00.0000000Z', N'Brownie de chocolate con una bola de helado.', N'/assets/images/products/brownie-con-helado.png', CAST(0 AS bit), N'Brownie con Helado', 49.0, '2026-10-01T00:00:00.0000000Z');
GO

-- PIN lockout and PIN reset subject (EF Core migration 20261002060049_AddPinLockoutAndPinResetSubject)
ALTER TABLE [Notifications] ADD [SubjectEmployeeId] uniqueidentifier NULL;

ALTER TABLE [Employees] ADD [FailedLoginAttempts] int NOT NULL DEFAULT 0;

ALTER TABLE [Employees] ADD [LockoutEndAt] datetime2 NULL;

CREATE INDEX [IX_Notifications_SubjectEmployeeId] ON [Notifications] ([SubjectEmployeeId]);

ALTER TABLE [Notifications] ADD CONSTRAINT [FK_Notifications_Employees_SubjectEmployeeId] FOREIGN KEY ([SubjectEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION;
GO
