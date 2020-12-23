using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;

namespace Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly StoreContext _context;
        private readonly IGenericRepositoryResolver _genericRepoResolver;

        public UnitOfWork(StoreContext context, IGenericRepositoryResolver genericRepoResolver)
        {
            _context = context;
            _genericRepoResolver = genericRepoResolver;
        }

        public async Task<int> Complete()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity
        {
            return _genericRepoResolver.Repository<TEntity>();
        }
    }
}