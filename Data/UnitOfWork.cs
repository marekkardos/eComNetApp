using System;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly StoreContext _context;
        private readonly IServiceProvider _serviceProvider;

        public UnitOfWork(StoreContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
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
            return _serviceProvider.GetService<IGenericRepository<TEntity>>();
        }
    }
}