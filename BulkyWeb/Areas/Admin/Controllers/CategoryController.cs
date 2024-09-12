using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOFWork _unitOFWork;
        public CategoryController(IUnitOFWork unitOFWork)
        {
            _unitOFWork = unitOFWork;
        }
        public IActionResult Index()
        {
            List<Category> objCategoryList = _unitOFWork.Category.GetAll().ToList();
            return View(objCategoryList);
        }
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category obj)
        {
            if (ModelState.IsValid)
            {
                _unitOFWork.Category.Add(obj);
                _unitOFWork.Save();
                TempData["Success"] = "Added Successfully";
                return RedirectToAction("Index");
            }
            return View();
        }
        public IActionResult Edit(int? id)
        {
            if (id == 0 || id == null)
            {
                return NotFound();
            }
            Category? category = _unitOFWork.Category.Get(u => u.Category_id == id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }
        [HttpPost]
        public IActionResult Edit(Category obj)
        {
            if (ModelState.IsValid)
            {
                _unitOFWork.Category.Update(obj);
                _unitOFWork.Save();
                TempData["Success"] = "Updated Successfully";
                return RedirectToAction("Index");
            }
            return View();
        }
        public IActionResult Delete(int? id)
        {
            Category? category = _unitOFWork.Category.Get(u => u.Category_id == id);
            if (category == null)
            {
                TempData["Error"] = "Error While finding category";
                return RedirectToAction("Index");
            }
            _unitOFWork.Category.Remove(category);
            _unitOFWork.Save();
            TempData["Success"] = "Removed Successfully";
            return RedirectToAction("Index");
        }
    }
}

