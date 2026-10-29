-- =============================================================================
-- Object:      dbo.usp_Order_GetById
-- Type:        stored procedure
-- Purpose:     Returns an order with its items and its whole status history, for the order detail and its timeline.
--              Items and history come as JSON in the same row (one result set).
-- Parameters:  @Id uniqueidentifier - order id
-- Returns:     0 or 1 row: Id, OrderNumber, Channel, Status, TableNumber, WaiterId (null for a pickup order; the API uses it
--              to show a waiter only their own orders and the pickup ones, and never returns it), Total, CreatedAt, LastStatusChangeAt, ReadyAt,
--              DeliveredAt,
--              ItemsJson (array of Name, Quantity, UnitPrice, Subtotal, in the order they were added),
--              HistoryJson (array of FromStatus, ToStatus, ChangedById, ChangedAt, oldest first)
-- Author:      JulianH2
-- Jira:        ITACR-11, ITACR-14, ITACR-16
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Order_GetById
    @Id uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    SELECT o.Id, o.OrderNumber, o.Channel, o.Status, t.Number AS TableNumber, o.WaiterId, o.Total, o.CreatedAt, o.LastStatusChangeAt, o.ReadyAt, o.DeliveredAt,
        (
            SELECT p.Name, i.Quantity, i.UnitPrice, i.Subtotal
            FROM dbo.OrderItems AS i
            JOIN dbo.Products AS p ON p.Id = i.ProductId
            WHERE i.OrderId = o.Id
            ORDER BY i.Id
            FOR JSON PATH
        ) AS ItemsJson,
        (
            SELECT h.FromStatus, h.ToStatus, h.ChangedById, h.CreatedAt AS ChangedAt
            FROM dbo.OrderStatusHistory AS h
            WHERE h.OrderId = o.Id
            ORDER BY h.CreatedAt, h.Id
            FOR JSON PATH
        ) AS HistoryJson
    FROM dbo.Orders AS o
    LEFT JOIN dbo.DiningTables AS t ON t.Id = o.TableId
    WHERE o.Id = @Id;
END;
