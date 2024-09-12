using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface IOrderHeaderRepository : IRepository<OrderHeader>
    {
        void Update(OrderHeader obj);

        void UpdateStatus(int OrderHeaderId, string OrderStatus, string? PaymentStatus = null);
        void UpdateStripePaymentId(int OrderHeaderId, string SessionId, string PaymentIntentId);
    }
}
