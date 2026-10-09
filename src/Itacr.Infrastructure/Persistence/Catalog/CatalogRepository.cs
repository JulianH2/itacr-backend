using System.Text.Json;
using Dapper;
using Itacr.Application.Catalog;
using Itacr.Domain.Catalog;

namespace Itacr.Infrastructure.Persistence.Catalog;

internal sealed class CatalogRepository(DbSession session) : ICatalogRepository
{
    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var categories = await connection.QueryAsync<Category>(session.Procedure("dbo.usp_Category_GetAll", null, cancellationToken));
        return [.. categories];
    }

    public async Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var products = await connection.QueryAsync<Product>(
            session.Procedure("dbo.usp_Product_GetByCategory", new { CategoryId = categoryId }, cancellationToken));
        return [.. products];
    }

    public async Task<IReadOnlyList<Product>> GetProductsByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        var products = await connection.QueryAsync<Product>(
            session.Procedure("dbo.usp_Product_GetByIds", new { ProductIdsJson = JsonSerializer.Serialize(ids) }, cancellationToken));
        return [.. products];
    }
}
