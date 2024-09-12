using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModel;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private IUnitOFWork _unitOFWork;
        public OrderController(IUnitOFWork unitOFWork)
        {
            _unitOFWork = unitOFWork;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Detail(int orderid)
        {
            OrderVM orderVM = new()
            {
                orderHeader = _unitOFWork.OrderHeader.Get(u=>u.OrderHeader_Id== orderid, includeProperties:"ApplicationUser"),
                orderDetails=_unitOFWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderid, includeProperties:"Product")
            };
            return View(orderVM);
        }

        [ActionName("Detail")]
        [HttpPost]
        public IActionResult Detail_PAY_NOW(OrderVM orderVM)
        {
            orderVM.orderHeader = _unitOFWork.OrderHeader.Get(u=>u.OrderHeader_Id==orderVM.orderHeader.OrderHeader_Id,includeProperties:"ApplicationUser");
            orderVM.orderDetails = _unitOFWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderVM.orderHeader.OrderHeader_Id, includeProperties: "Product");

            var domain = "http://localhost:5295/";
            var options = new Stripe.Checkout.SessionCreateOptions
            {
                SuccessUrl = domain + $"Customer/Cart/PaymentCofirmation?orderHeaderId={orderVM.orderHeader.OrderHeader_Id}",
                CancelUrl = domain + $"Admin/Order/Detail?orderid={orderVM.orderHeader.OrderHeader_Id}",
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>(),
                Mode = "payment",
            };
            foreach (var item in orderVM.orderDetails)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100),//20.50 =>2050
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                };
                options.LineItems.Add(sessionLineItem);
            }
            var service = new Stripe.Checkout.SessionService();
            Session session = service.Create(options);
            _unitOFWork.OrderHeader.UpdateStripePaymentId(orderVM.orderHeader.OrderHeader_Id, session.Id, session.PaymentIntentId);
            _unitOFWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }


        public IActionResult PaymentCofirmation(int orderHeaderId)
        {
            OrderHeader orderHeader = _unitOFWork.OrderHeader.Get(u => u.OrderHeader_Id == orderHeaderId);
            if (orderHeader.PaymetStatus != SD.PaymentStatusDelayedPayment)
            {
                //this is an order from Compay
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOFWork.OrderHeader.UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
                    _unitOFWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _unitOFWork.Save();
                }
            }
            return View(orderHeaderId);
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing(OrderVM orderVM)
        {
            _unitOFWork.OrderHeader.UpdateStatus(orderVM.orderHeader.OrderHeader_Id, SD.StatusInProcess);
            _unitOFWork.Save();
            TempData["Success"] = "Order Details Updated Seccessfully";
            return RedirectToAction("Detail", new {orderid=orderVM.orderHeader.OrderHeader_Id});
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder(OrderVM orderVM)
        {
            var orderHeader = _unitOFWork.OrderHeader.Get(u => u.OrderHeader_Id == orderVM.orderHeader.OrderHeader_Id);
            orderHeader.TrackingNumber = orderVM.orderHeader.TrackingNumber;
            orderHeader.Carrier=orderVM.orderHeader.Carrier;
            orderHeader.OrderStatus=orderVM.orderHeader.OrderStatus;
            orderHeader.ShippingDate=DateTime.Now;
            if (orderHeader.PaymetStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymetDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }

            _unitOFWork.OrderHeader.UpdateStatus(orderVM.orderHeader.OrderHeader_Id, SD.StatusShipped);
            _unitOFWork.Save();
            TempData["Success"] = "Order Shipped Seccessfully";
            return RedirectToAction("Detail", new { orderid = orderVM.orderHeader.OrderHeader_Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder(OrderVM orderVM)
        {
            var orderHeader = _unitOFWork.OrderHeader.Get(u => u.OrderHeader_Id == orderVM.orderHeader.OrderHeader_Id);
            
            if (orderHeader.PaymetStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };
                var service=new RefundService();
                Refund refund = service.Create(options);

                _unitOFWork.OrderHeader.UpdateStatus(orderVM.orderHeader.OrderHeader_Id, SD.StatusCancelled,SD.StatusRefunded);
            }
            else
            {
                _unitOFWork.OrderHeader.UpdateStatus(orderVM.orderHeader.OrderHeader_Id, SD.StatusCancelled, SD.StatusCancelled);
            }
            _unitOFWork.Save();
            TempData["Success"] = "Order Cancelled Seccessfully";
            return RedirectToAction("Detail", new { orderid = orderVM.orderHeader.OrderHeader_Id });
        }



        [HttpPost]
        [Authorize(Roles =SD.Role_Admin+","+SD.Role_Employee)]
        public IActionResult UpdateOrderDetail(OrderVM orderVM)
        {
            var orderHeaderFromDb = _unitOFWork.OrderHeader.Get(u=>u.OrderHeader_Id==orderVM.orderHeader.OrderHeader_Id);
            orderHeaderFromDb.Name=orderVM.orderHeader.Name;
            orderHeaderFromDb.PhoneNumber=orderVM.orderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress=orderVM.orderHeader.StreetAddress;
            orderHeaderFromDb.State=orderVM.orderHeader.State;
            orderHeaderFromDb.City=orderVM.orderHeader.City;
            orderHeaderFromDb.PostalCode=orderVM.orderHeader.PostalCode;
            if (!string.IsNullOrEmpty(orderVM.orderHeader.Carrier)){
                orderHeaderFromDb.Carrier=orderVM.orderHeader.Carrier;
            }
            if(!string.IsNullOrEmpty(orderVM.orderHeader.TrackingNumber)){
                orderHeaderFromDb.TrackingNumber=orderVM.orderHeader.TrackingNumber;
            }
            _unitOFWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOFWork.Save();
            TempData["Success"] = "Order Detail Updated Successfully";
            return RedirectToAction("Detail",new {orderid=orderHeaderFromDb.OrderHeader_Id});
        }

        #region API Calls
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> objOrderHeader;
            if(User.IsInRole(SD.Role_Admin )||User.IsInRole(SD.Role_Employee))
            {
                objOrderHeader = _unitOFWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();

            }
            else
            {
                var claimsIdentity=(ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                objOrderHeader= _unitOFWork.OrderHeader.GetAll(u=>u.ApplicationUserId==userId,includeProperties: "ApplicationUser").ToList();
            }
            switch (status)
            {
                case "paymentpending":
                    objOrderHeader = objOrderHeader.Where(u=>u.PaymetStatus==SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    objOrderHeader = objOrderHeader.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    objOrderHeader = objOrderHeader.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objOrderHeader = objOrderHeader.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;
            }
            return Json(new { data = objOrderHeader });
        }
        #endregion
    }
}
