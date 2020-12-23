using Core.Entities.OrderAggregate;
using Core.Specifications.Base;
using Core.Specifications.Builder;

namespace Core.Specifications
{
    public class OrdersWithItemsAndOrderingSpecification : BaseSpecification<Order>
    {
        public OrdersWithItemsAndOrderingSpecification(string email)
        {
            Query
                .Where(o => o.BuyerEmail == email)
                .Include(o => o.OrderItems)
                .Include(o => o.DeliveryMethod)
                .OrderByDescending(o => o.OrderDate);
        }

        public OrdersWithItemsAndOrderingSpecification(int id, string email)
        {
            Query
                .Where(o => o.Id == id && o.BuyerEmail == email)
                .Include(o => o.OrderItems)
                .Include(o => o.DeliveryMethod);
        }
    }
}