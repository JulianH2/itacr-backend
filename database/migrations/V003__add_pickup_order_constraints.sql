-- =============================================================================
-- Migration:   V003__add_pickup_order_constraints
-- Purpose:     Prepares the data for the pickup orders of customers without an account (HU_06, decided in ITACR-42):
--              one customer per phone, a phone that is always 10 digits, and a channel that always matches who is
--              involved (a MobileApp order has a customer and no table and no waiter; the others have no customer).
--              It adds constraints only; no table or column changes. The script stops with a clear message, before
--              changing anything, when existing rows would break a rule.
-- Author:      GuillermoRH19
-- Jira:        ITACR-12
-- =============================================================================

IF EXISTS (SELECT 1 FROM dbo.Customers WHERE Phone LIKE N'%[^0-9]%' OR LEN(Phone) <> 10)
BEGIN
    THROW 50001, N'V003 stopped: a customer has a phone that is not 10 digits. Fix dbo.Customers before running it.', 1;
END;

IF EXISTS (SELECT 1 FROM dbo.Customers GROUP BY Phone HAVING COUNT(*) > 1)
BEGIN
    THROW 50002, N'V003 stopped: two customers share a phone. Merge them in dbo.Customers before running it.', 1;
END;

IF EXISTS (
    SELECT 1 FROM dbo.Orders
    WHERE NOT (
        (Channel = N'MobileApp' AND CustomerId IS NOT NULL AND TableId IS NULL AND WaiterId IS NULL)
        OR (Channel IN (N'DineIn', N'TakeAway') AND CustomerId IS NULL)))
BEGIN
    THROW 50003, N'V003 stopped: an order has a channel that does not match its customer, table, or waiter. Fix dbo.Orders before running it.', 1;
END;
GO

ALTER TABLE [dbo].[Customers] ADD CONSTRAINT [CK_Customers_Phone]
    CHECK ([Phone] NOT LIKE N'%[^0-9]%' AND LEN([Phone]) = 10);
GO

CREATE UNIQUE INDEX [IX_Customers_Phone] ON [dbo].[Customers] ([Phone]);
GO

ALTER TABLE [dbo].[Orders] ADD CONSTRAINT [CK_Orders_ChannelParties]
    CHECK (
        ([Channel] = N'MobileApp' AND [CustomerId] IS NOT NULL AND [TableId] IS NULL AND [WaiterId] IS NULL)
        OR ([Channel] IN (N'DineIn', N'TakeAway') AND [CustomerId] IS NULL));
GO
