-- =============================================================================
-- Rollback:    V007__add_combo_exclusion_options
-- Purpose:     Reverses V007: deletes the Exclusion options of the combo catalog. The items of an order keep the names they
--              were saved with (they do not reference the catalog), so no order changes. Run only through the migrator
--              (`rollback V007__add_combo_exclusion_options.sql`), which also removes V007 from dbo.SchemaVersions so the
--              next run applies it again. Development only.
-- Author:      GuillermoRH19
-- Jira:        ITACR-18
-- =============================================================================

DELETE FROM dbo.ComboOptions WHERE [Type] = N'Exclusion';
