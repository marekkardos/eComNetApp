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
        //private Hashtable _repositories;

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

            //_repositories ??= new Hashtable();

            //var type = typeof(TEntity).Name;

            //if (_repositories.ContainsKey(type))
            //{
            //    return (IGenericRepository<TEntity>) _repositories[type];
            //}

            //var repositoryType = typeof(GenericRepository<>);
            //var repositoryInstance =
            //    Activator.CreateInstance(repositoryType.MakeGenericType(typeof(TEntity)), _context);

            //_repositories.Add(type, repositoryInstance);

            //return (IGenericRepository<TEntity>) _repositories[type];
        }
    }
}