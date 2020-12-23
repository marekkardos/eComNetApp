using Core.Entities;

namespace Core.Interfaces
{
    public interface IGenericRepositoryResolver
    {
        IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;
    }
}