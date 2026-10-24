-- =============================================================================
-- Rollback:    V005__add_product_is_available
-- Purpose:     Reverses V005: drops Products.IsAvailable and its default. Run only through the migrator
--              (`rollback V005__add_product_is_available.sql`), which also removes V005 from dbo.SchemaVersions so
--              the next run applies it again. Development only.
-- Author:      JulianH2
-- Jira:        ITACR-52
-- =============================================================================

IF OBJECT_ID(N'dbo.DF_Products_IsAvailable', N'D') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Products DROP CONSTRAINT DF_Products_IsAvailable;
END;

IF COL_LENGTH(N'dbo.Products', N'IsAvailable') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Products DROP COLUMN IsAvailable;
END;
