-- =============================================================================
-- Object:      dbo.usp_Manager_GetSalesTrend
-- Type:        stored procedure
-- Purpose:     The trend of the manager's dashboard (HU_16): for each hour of a date range that had something, the orders
--              created in that hour and the sales of the orders delivered in that hour. The hours are whole hours of UTC,
--              which are whole hours of the restaurant too (Mexico has a whole-hour offset), so the client only has to
--              write them in its own time zone, and it can add the hours up into days for a long range. Hours with no order
--              and no sale are not returned.
-- Parameters:  @From datetime2 - start of the range, inclusive (UTC)
--              @To datetime2 - end of the range, exclusive (UTC)
-- Returns:     0..n rows ordered by time: HourStart (UTC), Orders, Sales
-- Author:      GuillermoRH19
-- Jira:        ITACR-22
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_Manager_GetSalesTrend
    @From datetime2,
    @To datetime2
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Origin datetime2 = CAST(N'2000-01-01T00:00:00' AS datetime2);

    SELECT
        events.HourStart,
        SUM(events.Orders) AS Orders,
        CAST(SUM(events.Sales) AS decimal(12, 2)) AS Sales
    FROM (
        SELECT DATEADD(HOUR, DATEDIFF(HOUR, @Origin, o.CreatedAt), @Origin) AS HourStart, 1 AS Orders, CAST(0 AS decimal(12, 2)) AS Sales
        FROM dbo.Orders AS o
        WHERE o.CreatedAt >= @From AND o.CreatedAt < @To
        UNION ALL
        SELECT DATEADD(HOUR, DATEDIFF(HOUR, @Origin, o.DeliveredAt), @Origin), 0, CAST(o.Total AS decimal(12, 2))
        FROM dbo.Orders AS o
        WHERE o.Status = N'Delivered' AND o.DeliveredAt >= @From AND o.DeliveredAt < @To
    ) AS events
    GROUP BY events.HourStart
    ORDER BY events.HourStart;
END;
