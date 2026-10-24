-- =============================================================================
-- Migration:   V005__add_product_is_available
-- Purpose:     Lets the restaurant mark a product as sold out without deleting it: adds Products.IsAvailable, true for
--              every existing product. Its reversal is database/rollbacks/V005__add_product_is_available.sql.
-- Author:      JulianH2
-- Jira:        ITACR-52
-- =============================================================================

IF COL_LENGTH(N'dbo.Products', N'IsAvailable') IS NULL
BEGIN
    ALTER TABLE dbo.Products
        ADD IsAvailable bit NOT NULL CONSTRAINT DF_Products_IsAvailable DEFAULT (1);
END;
