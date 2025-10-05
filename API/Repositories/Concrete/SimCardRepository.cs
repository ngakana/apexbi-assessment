using API.Data;
using API.Data.Entities;
using API.Repositories.Abstract;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories.Concrete;

public class SimCardRepository(AppDbContext dbContext) : ISimCardRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task AddAsync(SimCard entity, CancellationToken ct = default)
    {
        await _dbContext.SimCards.AddAsync(entity, ct);
    }

    public async Task AddRangeAsync(ICollection<SimCard> entities, CancellationToken ct = default)
    {
        await _dbContext.SimCards.AddRangeAsync(entities, ct);
    }

    public async Task<List<SimCard>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbContext.SimCards.AsNoTracking().ToListAsync(ct);
    }

    public async Task<SimCard?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.SimCards.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public IQueryable<SimCard> GetAsQueryable()
    {
        return _dbContext.SimCards.AsQueryable();
    }
}
