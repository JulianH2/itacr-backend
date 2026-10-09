-- =============================================================================
-- Object:      dbo.usp_Order_GetActive
-- Type:        stored procedure
-- Purpose:     Returns the orders the kitchen has to deal with (New, InPreparation, and Ready) in one queue, oldest
--              first, whatever their channel. The items come as JSON in the same row (one result set), so an order
--              and its items are always read together.
-- Parameters:  none
-- Returns:     0..n rows ordered by CreatedAt, then OrderNumber: Id, OrderNumber, Channel, Status, TableNumber (null
--              when the order has no table), CreatedAt, ItemsJson (array of ProductName, Quantity, Size, Drink,
--              Extras, Exclusions; Size and Drink are left out when null)
-- Author:      GuillermoRH19
-- Jira:        ITACR-10
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Order_GetActive
AS
BEGIN
    SET NOCOUNT ON;

    SELECT o.Id, o.OrderNumber, o.Channel, o.Status, t.Number AS TableNumber, o.CreatedAt,
        (
            SELECT p.Name AS ProductName, i.Quantity, i.Size, i.Drink,
                JSON_QUERY(CAST(i.Extras AS nvarchar(max))) AS Extras,
                JSON_QUERY(CAST(i.Exclusions AS nvarchar(max))) AS Exclusions
            FROM dbo.OrderItems AS i
            JOIN dbo.Products AS p ON p.Id = i.ProductId
            WHERE i.OrderId = o.Id
            ORDER BY i.Id
            FOR JSON PATH
        ) AS ItemsJson
    FROM dbo.Orders AS o
    LEFT JOIN dbo.DiningTables AS t ON t.Id = o.TableId
    WHERE o.Status IN (N'New', N'InPreparation', N'Ready')
    ORDER BY o.CreatedAt, o.OrderNumber;
END;
