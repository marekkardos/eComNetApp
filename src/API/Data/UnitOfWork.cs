using Core.Entities;
using Core.Interfaces;

namespace Data;

public class UnitOfWork(StoreContext context, IGenericRepositoryResolver genericRepoResolver) : IUnitOfWork
{
    private bool _disposed;

    public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity
    {
        return genericRepoResolver.Repository<TEntity>();
    }

    public async Task<int> Complete()
    {
        return await context.SaveChangesAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                context.Dispose();
            }
            _disposed = true;
        }
    }
}