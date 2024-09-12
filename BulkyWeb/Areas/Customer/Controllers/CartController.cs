using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModel;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{

    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOFWork _unitOFWork;
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(IUnitOFWork unitOfWork)
        {
            _unitOFWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var claimidentity = (ClaimsIdentity)User.Identity;
            var user = claimidentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOFWork.ShoppingCart.GetAll(filter: u => u.ApplicationUserId == user, includeProperties: "Product"),
                OrderHeader = new()
            };
            IEnumerable<ProductImage> productImages=_unitOFWork.ProductImage.GetAll();
            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Product.ProductImages = productImages.Where(u => u.ProductId == cart.Product.Product_id).ToList();
                cart.Price = CalculatePrice(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            return View(ShoppingCartVM);
        }

        public IActionResult Summary()
        {
            var claimidentity = (ClaimsIdentity)User.Identity;
            var user = claimidentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOFWork.ShoppingCart.GetAll(filter: u => u.ApplicationUserId == user, includeProperties: "Product"),
                OrderHeader = new()
            };
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOFWork.ApplicationUser.Get(u => u.Id == user);
            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;


            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = CalculatePrice(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPost(ShoppingCartVM shoppingCartVM)
        {
            var claimidentity = (ClaimsIdentity)User.Identity;
            var user = claimidentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCartVM.ShoppingCartList = _unitOFWork.ShoppingCart.GetAll(u => u.ApplicationUserId == user, includeProperties: "Product,ApplicationUser");
            shoppingCartVM.OrderHeader.ApplicationUserId = user;

            ApplicationUser applicationUser = _unitOFWork.ApplicationUser.Get(u => u.Id == user);
            foreach (var cart in shoppingCartVM.ShoppingCartList)
            {
                cart.Price = CalculatePrice(cart);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                //Normal Customer Account
                shoppingCartVM.OrderHeader.PaymetStatus=SD.PaymentStatusPending;
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                //its a Company Account
                shoppingCartVM.OrderHeader.PaymetStatus = SD.PaymentStatusDelayedPayment;
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }

            _unitOFWork.OrderHeader.Add(shoppingCartVM.OrderHeader);
            _unitOFWork.Save();

            foreach(var cart in shoppingCartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = shoppingCartVM.OrderHeader.OrderHeader_Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                _unitOFWork.OrderDetail.Add(orderDetail);
                _unitOFWork.Save();
            }
            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                //Normal Customer Account
                //Stripe Logic
                var domain = "http://localhost:5295/";
                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    SuccessUrl = domain+$"Customer/Cart/OrderConfirmation?id={shoppingCartVM.OrderHeader.OrderHeader_Id}",
                    CancelUrl= domain+$"Customer/Cart/Index",
                    LineItems = new List<Stripe.Checkout.SessionLineItemOptions>(),
                    Mode = "payment",
                };
                foreach (var item in shoppingCartVM.ShoppingCartList)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData=new SessionLineItemPriceDataOptions
                        {
                            UnitAmount=(long)(item.Price*100),//20.50 =>2050
                            Currency="usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name=item.Product.Title
                            }
                        },
                        Quantity=item.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                } 
                var service = new Stripe.Checkout.SessionService();
                Session session=service.Create(options);
                _unitOFWork.OrderHeader.UpdateStripePaymentId(shoppingCartVM.OrderHeader.OrderHeader_Id,session.Id,session.PaymentIntentId);
                _unitOFWork.Save();
                Response.Headers.Add("Location",session.Url);
                return new StatusCodeResult(303);
            }
            return RedirectToAction("OrderConfirmation",new {id= shoppingCartVM .OrderHeader.OrderHeader_Id});
        }

        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unitOFWork.OrderHeader.Get(u=>u.OrderHeader_Id==id);
            if (orderHeader.PaymetStatus != SD.PaymentStatusDelayedPayment)
            {
                var service=new SessionService();
                Session session = service.Get(orderHeader.SessionId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOFWork.OrderHeader.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                    _unitOFWork.OrderHeader.UpdateStatus(id,SD.StatusApproved,SD.PaymentStatusApproved);
                    _unitOFWork.Save();
                }
                HttpContext.Session.Clear();
            }
            List<ShoppingCart> shoppingCarts = _unitOFWork.ShoppingCart.GetAll(u=>u.ApplicationUserId==orderHeader.ApplicationUserId).ToList();
            _unitOFWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOFWork.Save();
            return View(id);
        }
        public IActionResult Remove(int? id)
        {
            ShoppingCart shoppingCart = _unitOFWork.ShoppingCart.Get(u => u.Shop_Id == id,tracked:true);
            _unitOFWork.ShoppingCart.Remove(shoppingCart);
            HttpContext.Session.SetInt32(SD.SessionCart, _unitOFWork.ShoppingCart.GetAll(u => u.ApplicationUserId == shoppingCart.ApplicationUserId).Count()-1);
            _unitOFWork.Save();
            return RedirectToAction("Index");
        }
        public IActionResult Plus(int? id)
        {
            ShoppingCart shoppingCart = _unitOFWork.ShoppingCart.Get(u => u.Shop_Id == id);
            shoppingCart.Count++;
            _unitOFWork.ShoppingCart.Update(shoppingCart);
            _unitOFWork.Save();
            return RedirectToAction("Index");
        }
        public IActionResult Minus(int? id)
        {
            ShoppingCart shoppingCart = _unitOFWork.ShoppingCart.Get(u => u.Shop_Id == id, tracked: true);
            if (shoppingCart.Count <= 1)
            {
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOFWork.ShoppingCart.GetAll(u => u.ApplicationUserId == shoppingCart.ApplicationUserId).Count() - 1);
                _unitOFWork.ShoppingCart.Remove(shoppingCart);
            }
            else
            {
                shoppingCart.Count--;
                _unitOFWork.ShoppingCart.Update(shoppingCart);
            }
            _unitOFWork.Save();
            return RedirectToAction("Index");
        }

        private double CalculatePrice(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }
            else
            {
                if (shoppingCart.Count <= 100)
                {
                    return shoppingCart.Product.Price50;
                }
                else
                {
                    return shoppingCart.Product.Price100;
                }
            }
        }
        //[HttpPost]
        //public IActionResult Index()
        //{
        //    return View();
        //}
    }
}
