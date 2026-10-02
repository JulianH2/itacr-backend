using Microsoft.EntityFrameworkCore;

namespace Itacr.Api.Infrastructure.Persistence;

public sealed class ItacrDbContext(DbContextOptions<ItacrDbContext> options) : DbContext(options);
