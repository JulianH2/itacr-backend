-- =============================================================================
-- Object:      dbo.usp_Order_GetById
-- Type:        stored procedure
-- Purpose:     Returns an order with its items and its whole status history, for the order detail and its timeline.
--              Items and history come as JSON in the same row (one result set).
-- Parameters:  @Id uniqueidentifier - order id
-- Returns:     0 or 1 row: Id, OrderNumber, Channel, Status, TableNumber, Total, CreatedAt, LastStatusChangeAt, ReadyAt,
--              ItemsJson (array of Name, Quantity, UnitPrice, Subtotal, in the order they were added),
--              HistoryJson (array of FromStatus, ToStatus, ChangedById, ChangedAt, oldest first)
-- Author:      JulianH2
-- Jira:        ITACR-11
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Order_GetById
    @Id uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    SELECT o.Id, o.OrderNumber, o.Channel, o.Status, t.Number AS TableNumber, o.Total, o.CreatedAt, o.LastStatusChangeAt, o.ReadyAt,
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
