using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Core.Specifications.Base;
using Core.Specifications.Exceptions;

namespace Core.Specifications.Builder
{
    public static class SpecificationBuilderExtensions
    {
        public static ISpecificationBuilder<T> Where<T>(
            this ISpecificationBuilder<T> specificationBuilder,
            Expression<Func<T, bool>> criteria)
        {
            ((List<Expression<Func<T, bool>>>)specificationBuilder.Specification.Criteria).Add(criteria);

            return specificationBuilder;
        }

        public static ISpecificationBuilder<T> OrderBy<T>(
            this ISpecificationBuilder<T> specificationBuilder,
            Expression<Func<T, object>> orderExpression)
        {
            specificationBuilder.Specification.OrderBy = orderExpression;
            return specificationBuilder;
        }

        public static ISpecificationBuilder<T> OrderByDescending<T>(
            this ISpecificationBuilder<T> specificationBuilder,
            Expression<Func<T, object>> orderExpression)
        {
            specificationBuilder.Specification.OrderByDescending = orderExpression;
            return specificationBuilder;
        }

        public static ISpecificationBuilder<T> Include<T>(
            this ISpecificationBuilder<T> specificationBuilder,
            Expression<Func<T, object>> includeExpression)
        {
            ((List<Expression<Func<T, object>>>)specificationBuilder.Specification.Includes).Add(includeExpression);
            return specificationBuilder;
        }

        public static ISpecificationBuilder<T> Paginate<T>(
            this ISpecificationBuilder<T> specificationBuilder,
            IPagingSpecParams pagingParams)
        {
            specificationBuilder.Skip(pagingParams.PageSize * (pagingParams.PageIndex - 1));
            specificationBuilder.Take(pagingParams.PageSize);

            specificationBuilder.Specification.IsPagingEnabled = true;

            return specificationBuilder;
        }

        public static ISpecificationBuilder<T> Take<T>(
            this ISpecificationBuilder<T> specificationBuilder,
            int take)
        {
            if (specificationBuilder.Specification.Take != null)
            {
                throw new DuplicateTakeException();
            }

            specificationBuilder.Specification.Take = take;
            specificationBuilder.Specification.IsPagingEnabled = true;
            return specificationBuilder;
        }

        public static ISpecificationBuilder<T> Skip<T>(
            this ISpecificationBuilder<T> specificationBuilder,
            int skip)
        {
            if (specificationBuilder.Specification.Skip != null)
            {
                throw new DuplicateSkipException();
            }

            specificationBuilder.Specification.Skip = skip;
            specificationBuilder.Specification.IsPagingEnabled = true;
            return specificationBuilder;
        }
    }
}