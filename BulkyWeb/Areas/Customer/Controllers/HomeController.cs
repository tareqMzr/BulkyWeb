using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOFWork _unitOFWork;
        public HomeController(ILogger<HomeController> logger, IUnitOFWork unitOFWork)
        {
            _logger = logger;
            _unitOFWork = unitOFWork;
        }

        public IActionResult Index()
        {
            
            IEnumerable<Product> productList = _unitOFWork.Product.GetAll(includeProperties: "Category,ProductImages");
            return View(productList);
        }
        public IActionResult Details(int id)
        {
            Product product = _unitOFWork.Product.Get(u => u.Product_id == id, "Category,ProductImages");
            ShoppingCart shoppingCart = new()
            {
                Product = product,
                Count = 1,
                ProductId = id
            };
            return View(shoppingCart);
        }
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var claimidentity = (ClaimsIdentity)User.Identity;
            var user = claimidentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCart.ApplicationUserId = user;
            if (ModelState.IsValid)
            {
                ShoppingCart cart = _unitOFWork.ShoppingCart.Get(u => u.ApplicationUserId == user && u.ProductId == shoppingCart.ProductId);
                if (cart != null)
                {
                    cart.Count += shoppingCart.Count;
                    _unitOFWork.ShoppingCart.Update(cart);
                    _unitOFWork.Save();
                }
                else
                {
                    _unitOFWork.ShoppingCart.Add(shoppingCart);
                    _unitOFWork.Save();
                    HttpContext.Session.SetInt32(SD.SessionCart,
                        _unitOFWork.ShoppingCart.GetAll(u=>u.ApplicationUserId==user).Count());
                }
                TempData["Success"] = "Cart Updated Successfully!";
            }

            return RedirectToAction("Index");
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
