-- =============================================================================
-- Migration:   V004__use_webp_product_images
-- Purpose:     The product photos of the PWA are WebP files (public/assets/images/products/<name>.webp in
--              itacr-frontend, ITACR-48). V001 seeded the 18 products with the same paths ending in .png, so this
--              script changes the extension. It only touches the ImageUrl of the products that still end in .png
--              inside that folder, so running it on a database that already has .webp changes nothing.
-- Author:      GuillermoRH19
-- Jira:        ITACR-48
-- =============================================================================

UPDATE dbo.Products
SET ImageUrl = LEFT(ImageUrl, LEN(ImageUrl) - 4) + N'.webp',
    UpdatedAt = SYSUTCDATETIME()
WHERE ImageUrl LIKE N'/assets/images/products/%.png';
