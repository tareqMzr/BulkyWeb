using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOFWork _unitOFWork;
        public CompanyController(IUnitOFWork unitOFWork)
        {
            _unitOFWork = unitOFWork;
        }
        public IActionResult Index()
        {
            List<Company> CompanyList = _unitOFWork.Company.GetAll().ToList();
            return View(CompanyList);
        }
        public IActionResult Upsert(int? id)
        {
            if (id == null)
            {
                return View(new Company());
            }
            else
            {
                Company company = _unitOFWork.Company.Get(u => u.Company_id == id);
                return View(company);
            }
        }
        [HttpPost]
        public IActionResult Upsert(Company obj)
        {
            if (ModelState.IsValid)
            {
                if (obj.Company_id != 0)
                {
                    _unitOFWork.Company.Update(obj);
                }
                else
                {
                    _unitOFWork.Company.Add(obj);
                }
                _unitOFWork.Save();
                TempData["Success"] = "Added Successfully";
                return RedirectToAction("Index");
            }
            else
            {
                return View(obj);
            }
        }
        public IActionResult Delete(int? id)
        {
            Company company = _unitOFWork.Company.Get(u => u.Company_id == id);
            _unitOFWork.Company.Remove(company);
            _unitOFWork.Save();
            TempData["Success"] = "Removed Successfully";
            return RedirectToAction("Index");
        }
        #region API Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> obj = _unitOFWork.Company.GetAll().ToList();
            return Json(new { data = obj });
        }
        #endregion
    }
}


