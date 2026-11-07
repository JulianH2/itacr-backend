-- =============================================================================
-- Migration:   V007__add_combo_exclusion_options
-- Purpose:     A combo can be ordered without some of its ingredients (HU_12, "sin cebolla"). The ingredients a customer
--              can leave out are options of the combo catalog with type Exclusion and no price, so the waiter and the
--              customer pick from the same list and the kitchen reads the name. It only adds the rows that are missing,
--              so running it again changes nothing.
-- Author:      GuillermoRH19
-- Jira:        ITACR-18
-- =============================================================================

INSERT INTO dbo.ComboOptions (Id, [Type], Name, PriceAdjustment, CreatedAt, UpdatedAt)
SELECT option_values.Id, N'Exclusion', option_values.Name, 0, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM (VALUES
    (CAST('30000000-0000-0000-0000-000000000010' AS uniqueidentifier), N'Cebolla'),
    (CAST('30000000-0000-0000-0000-000000000011' AS uniqueidentifier), N'Jitomate'),
    (CAST('30000000-0000-0000-0000-000000000012' AS uniqueidentifier), N'Lechuga'),
    (CAST('30000000-0000-0000-0000-000000000013' AS uniqueidentifier), N'Pepinillos'),
    (CAST('30000000-0000-0000-0000-000000000014' AS uniqueidentifier), N'Mayonesa'),
    (CAST('30000000-0000-0000-0000-000000000015' AS uniqueidentifier), N'Catsup'),
    (CAST('30000000-0000-0000-0000-000000000016' AS uniqueidentifier), N'Mostaza')
) AS option_values (Id, Name)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.ComboOptions AS existing
    WHERE existing.[Type] = N'Exclusion' AND existing.Name = option_values.Name);
