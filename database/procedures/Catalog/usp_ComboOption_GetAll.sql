-- =============================================================================
-- Object:      dbo.usp_ComboOption_GetAll
-- Type:        stored procedure
-- Purpose:     Returns the options a combo can be customized with: sizes, drinks, extras, and the ingredients that can be
--              left out. The menu is small, so the API reads all of them and picks the ones an order chose.
-- Parameters:  none
-- Returns:     Id uniqueidentifier, Type nvarchar(20) (Size, Drink, Extra, Exclusion), Name nvarchar(60),
--              PriceAdjustment decimal(10,2); ordered by type, then price, then name
-- Author:      GuillermoRH19
-- Jira:        ITACR-18
-- =============================================================================
CREATE OR ALTER PROCEDURE dbo.usp_ComboOption_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT o.Id, o.[Type], o.Name, o.PriceAdjustment
    FROM dbo.ComboOptions AS o
    ORDER BY o.[Type], o.PriceAdjustment, o.Name;
END;
