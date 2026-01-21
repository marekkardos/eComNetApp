using Core.Specifications.Base;

namespace Core.Specifications.Builder
{
    public class SpecificationBuilder<T> : ISpecificationBuilder<T>
    {
        public BaseSpecification<T> Specification { get; }

        public SpecificationBuilder(BaseSpecification<T> specification)
        {
            this.Specification = specification;
        }
    }
}