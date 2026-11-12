-- =============================================================================
-- Migration:   V009__add_order_modified_at
-- Purpose:     A waiter can change an order that the kitchen has not finished (HU_13). This column keeps when the order was
--              last modified, so the kitchen board shows the Modificado mark. It is NULL for an order that was never
--              modified, which includes every order that existed before. It only adds the column when it is missing, so
--              running it again changes nothing.
-- Author:      JulianH2
-- Jira:        ITACR-19
-- =============================================================================

IF COL_LENGTH(N'dbo.Orders', N'ModifiedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Orders ADD ModifiedAt datetime2 NULL;
END;
