using Microsoft.EntityFrameworkCore;

namespace API.Repositories.Abstract;

public interface IUnitOfWork : IAsyncDisposable
{
    IDatasetRepository DatasetRepository { get; }
    ISimCardRepository SimCardRepository { get; }
    Task<int> SaveChangesAsync();
}
