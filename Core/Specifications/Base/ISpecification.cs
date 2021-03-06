using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Core.Specifications.Base
{
    public interface ISpecification<T>
    {
        IList<Expression<Func<T, bool>>> Criteria { get; }

        IList<Expression<Func<T, object>>> Includes {get; }

         Expression<Func<T, object>> OrderBy {get; }

         Expression<Func<T, object>> OrderByDescending {get; }

         int? Take {get;}

         int? Skip {get;}

         bool QueryIsTracked { get; }

         //bool IsPagingEnabled {get;}
        }
}