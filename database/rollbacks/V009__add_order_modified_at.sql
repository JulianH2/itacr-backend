-- =============================================================================
-- Rollback:    V009__add_order_modified_at
-- Purpose:     Reverses V009: drops Orders.ModifiedAt. Run only through the migrator
--              (rollback V009__add_order_modified_at.sql), which also removes V009 from dbo.SchemaVersions so the next
--              run applies it again. Development only.
-- Author:      JulianH2
-- Jira:        ITACR-19
-- =============================================================================

IF COL_LENGTH(N'dbo.Orders', N'ModifiedAt') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Orders DROP COLUMN ModifiedAt;
END;
