using API.Data;
using API.Data.Entities;
using API.Repositories.Abstract;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories.Concrete;

public class DatasetRepository (AppDbContext dbContext) : IDatasetRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task AddAsync(Dataset entity, CancellationToken ct = default)
    {
        await _dbContext.Datasets.AddAsync(entity, ct);
    }

    public async Task AddRangeAsync(ICollection<Dataset> entities, CancellationToken ct = default)
    {
        await _dbContext.Datasets.AddRangeAsync(entities, ct);
    }

    public async Task<List<Dataset>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbContext.Datasets.AsNoTracking().ToListAsync(ct);
    }

    public async Task<Dataset?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Datasets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public IQueryable<Dataset> GetAsQueryable()
    {
        return _dbContext.Datasets.AsQueryable();
    }
}
