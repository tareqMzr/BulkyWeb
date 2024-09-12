using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly ApplicationDbContext _db;
        public OrderHeaderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(OrderHeader obj)
        {
            _db.OrderHeader.Update(obj);
        }

        public void UpdateStatus(int OrderHeaderId, string OrderStatus, string? PaymentStatus = null)
        {
            var orderfromdb = _db.OrderHeader.FirstOrDefault(u=>u.OrderHeader_Id==OrderHeaderId);
            if (orderfromdb != null) {
                orderfromdb.OrderStatus = OrderStatus;
                if (!string.IsNullOrEmpty(PaymentStatus))
                {
                    orderfromdb.PaymetStatus= PaymentStatus;
                }
            }
        }

        public void UpdateStripePaymentId(int OrderHeaderId, string SessionId, string PaymentIntentId)
        {
            var orderfromdb = _db.OrderHeader.FirstOrDefault(u => u.OrderHeader_Id == OrderHeaderId);
            if (!string.IsNullOrEmpty(SessionId))
            {
                orderfromdb.SessionId= SessionId;
            }
            if (!string.IsNullOrEmpty(PaymentIntentId))
            {
                orderfromdb.SessionId = PaymentIntentId;
                orderfromdb.PaymetDate= DateTime.Now;
            }
        }
    }
}
