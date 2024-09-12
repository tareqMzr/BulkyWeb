using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModel;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOFWork _unitOFWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOFWork unitOFWork,IWebHostEnvironment webHostEnvironment)
        {
            _unitOFWork = unitOFWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            List<Product> objProducts = _unitOFWork.Product.GetAll(includeProperties:"Category").ToList();
            return View(objProducts);
        }
        public IActionResult Upsert(int? id) {
            IEnumerable<SelectListItem> CategoryList = _unitOFWork.Category.GetAll().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Category_id.ToString(),
            });
            if (id == null || id == 0)
            {
                ProductVM productVM = new()
                {
                    CategoryList = CategoryList,
                    Product = new Product()
                };
                return View(productVM);
            }
            else
            {
                ProductVM productVM =new()
                {
                    CategoryList=CategoryList,
                    Product=_unitOFWork.Product.Get(u=>u.Product_id==id,includeProperties: "ProductImages")
                };
                return View(productVM);
            }
        }
        [HttpPost]
        public IActionResult Upsert(ProductVM obj, List<IFormFile>? files)
        {
            if (ModelState.IsValid)
            {
                if (obj.Product.Product_id != 0)
                {
                    _unitOFWork.Product.Update(obj.Product);
                }
                else
                {
                    _unitOFWork.Product.Add(obj.Product);
                }
                _unitOFWork.Save();
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (files != null)
                {


                    foreach(IFormFile file in files)
                    {
                        //Guid.NewGuid().ToString() Generate random 128 bit // Path.GetExtension(file.FileName) get file type example(image.jfij)=.jfif
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath=@"images\products\product-"+obj.Product.Product_id;
                        string finalPath = Path.Combine(wwwRootPath, productPath);

                        if (!Directory.Exists(finalPath))
                        {
                            Directory.CreateDirectory(finalPath);
                        }
                        using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }
                        ProductImage productImage = new()
                        {
                            ImageUrl = @"\" + productPath + @"\" + fileName,
                            ProductId= obj.Product.Product_id
                        };
                        if (obj.Product.ProductImages == null)
                        {
                            obj.Product.ProductImages =new List<ProductImage>();
                        }
                        obj.Product.ProductImages.Add(productImage);
                    }
                    _unitOFWork.Product.Update(obj.Product);
                    _unitOFWork.Save();
                }
               
                TempData["Success"] = "Prouct Created/Add Successfully";
                return RedirectToAction("Index");
            }
            else{
                obj.CategoryList = _unitOFWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text=u.Name,
                    Value=u.Category_id.ToString(),
                });
                return View(obj);
            }
        }
        public IActionResult Delete(int? id)
        {
            Product? product = _unitOFWork.Product.Get(u => u.Product_id == id);
            if (product == null)
            {
                TempData["Error"] = "Error While finding product";
                return RedirectToAction("Index");
            }

            string productPath = @"images\products\product-" + id;
            string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);

            if (!Directory.Exists(finalPath))
            {
                string[] filePaths=Directory.GetFiles(finalPath);
                foreach (var filePath in filePaths)
                {
                    System.IO.File.Delete(filePath);
                }
                Directory.Delete(finalPath);
            }
            _unitOFWork.Product.Remove(product);
            _unitOFWork.Save();
            TempData["Success"] = "Removed Successfully";
            return Json(new{ date= true});
        }
        #region API Calls
        [HttpGet]
        public IActionResult GetAll() {
            List<Product> objProducts = _unitOFWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new {data=  objProducts }); 
        }

        public IActionResult DeleteImage(int imageId)
        {
            var imageToBeDeleted = _unitOFWork.ProductImage.Get(u=>u.id==imageId);
            var productId=imageToBeDeleted.ProductId;
            if(imageToBeDeleted != null)
            {
                if (!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageToBeDeleted.ImageUrl.Trim('\\'));
                    if (System.IO.File.Exists(oldImagePath)) {
                        System.IO.File.Delete(oldImagePath);
                    }
                }
                _unitOFWork.ProductImage.Remove(imageToBeDeleted);
                _unitOFWork.Save();
                TempData["success"] = "Deleted successfully";
            }
            return RedirectToAction("Upsert", new { id = productId });
        }
        #endregion
    }
}
