using System;
using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Data
{
    public class GenericRepositoryResolver : IGenericRepositoryResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public GenericRepositoryResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity
        {
            return _serviceProvider.GetService<IGenericRepository<TEntity>>();
        }
    }
}