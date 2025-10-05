using API.Data;
using API.Repositories.Abstract;

namespace API.Repositories.Concrete;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;
    public IDatasetRepository DatasetRepository { get; }
    public ISimCardRepository SimCardRepository { get; }

    public UnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
        DatasetRepository = new DatasetRepository(dbContext);
        SimCardRepository = new SimCardRepository(dbContext);
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    async Task<int> IUnitOfWork.SaveChangesAsync()
    {
        UpdateAuditableEntities();
        return await _dbContext.SaveChangesAsync();
    }

    private void  UpdateAuditableEntities()
    {
        var entities = _dbContext
            .ChangeTracker
            .Entries<IAuditableEntity>()
            .ToList();
        foreach (var entity in entities)
        {
            entity.Property(e => e.UploadDate).CurrentValue = DateTime.UtcNow;
        }
    }
}
