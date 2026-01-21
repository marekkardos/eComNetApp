using Core.Entities.OrderAggregate;
using Core.Specifications.Base;
using Core.Specifications.Builder;

namespace Core.Specifications
{
    public class OrderByPaymentIntentIdSpecification : BaseSpecification<Order>
    {
        public OrderByPaymentIntentIdSpecification(string paymentIntentId) 
        {
            Query.Where(o => o.PaymentIntentId == paymentIntentId);
        }
    }
}