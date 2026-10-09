-- =============================================================================
-- Migration:   V002__add_order_status_history
-- Purpose:     Adds dbo.OrderStatusHistory, one row per status change of an order (including its creation, which has no
--              FromStatus), so the kitchen and the manager can see when and by whom each step happened.
--              dbo.Orders and dbo.OrderItems already exist in V001 and do not change.
-- Author:      GuillermoRH19
-- Jira:        ITACR-40
-- =============================================================================

CREATE TABLE [dbo].[OrderStatusHistory] (
    [Id] uniqueidentifier NOT NULL,
    [OrderId] uniqueidentifier NOT NULL,
    [FromStatus] nvarchar(20) NULL,
    [ToStatus] nvarchar(20) NOT NULL,
    [ChangedById] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_OrderStatusHistory] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderStatusHistory_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderStatusHistory_Employees_ChangedById] FOREIGN KEY ([ChangedById]) REFERENCES [dbo].[Employees] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_OrderStatusHistory_OrderId_CreatedAt] ON [dbo].[OrderStatusHistory] ([OrderId], [CreatedAt]);

CREATE INDEX [IX_OrderStatusHistory_ChangedById] ON [dbo].[OrderStatusHistory] ([ChangedById]);
