using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Core.Specifications.Builder;

namespace Core.Specifications.Base
{
    public class BaseSpecification<T> : ISpecification<T>
    {
        protected ISpecificationBuilder<T> Query { get; }

        public BaseSpecification()
        {
            this.Query = new SpecificationBuilder<T>(this);
        }

        public IList<Expression<Func<T, bool>>> Criteria { get; } = 
            new List<Expression<Func<T, bool>>>();

        public IList<Expression<Func<T, object>>> Includes { get; } =
            new List<Expression<Func<T, object>>>();

        public Expression<Func<T, object>> OrderBy { get; internal set; }

        public Expression<Func<T, object>> OrderByDescending { get; internal set; }

        public int? Take { get; internal set; }

        public int? Skip { get; internal set; }

        public bool IsPagingEnabled { get; internal set; }

        protected void AddWhere(Expression<Func<T, bool>> whereExpression)
        {
            Criteria.Add(whereExpression);
        }

        protected void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }

        protected void AddOrderBy(Expression<Func<T, object>> orderByExpression)
        {
            OrderBy = orderByExpression;
        }

        protected void AddOrderByDescending(Expression<Func<T, object>> orderByDescExpression)
        {
            OrderByDescending = orderByDescExpression;
        }
    }
}