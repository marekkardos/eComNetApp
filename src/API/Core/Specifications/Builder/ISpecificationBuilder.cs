using Core.Specifications.Base;

namespace Core.Specifications.Builder
{
    public interface ISpecificationBuilder<T>
    {
        BaseSpecification<T> Specification { get; }
    }
}