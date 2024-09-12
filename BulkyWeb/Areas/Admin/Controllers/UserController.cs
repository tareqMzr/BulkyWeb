using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModel;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly IUnitOFWork _unitOFWork;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        public UserController( UserManager<IdentityUser> userManager, IUnitOFWork unitOFWork, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _unitOFWork = unitOFWork;
            _roleManager = roleManager;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RoleManagment(string id)
        {
            var user = _userManager.FindByIdAsync(id).GetAwaiter().GetResult();
            var Rolename= _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();
            //string RoleId = _db.UserRoles.FirstOrDefault(u => u.UserId == id).RoleId;

            RoleManagmentVM roleManagmentVM = new RoleManagmentVM()
            {
                ApplicationUser = _unitOFWork.ApplicationUser.Get(u=>u.Id==id,includeProperties:"Company"),
                RoleList= _roleManager.Roles.Select(i=>new SelectListItem { Text=i.Name,Value=i.Name}),
                CompanyList= _unitOFWork.Company.GetAll().Select(i => new SelectListItem { Text = i.Name, Value = i.Company_id.ToString() }),
            };
            roleManagmentVM.ApplicationUser.Role = Rolename;
            //roleManagmentVM.ApplicationUser.Role = _db.Roles.FirstOrDefault(u=>u.Id==RoleId).Name;
            return View(roleManagmentVM);
        }
        [HttpPost]
        public IActionResult RoleManagment(RoleManagmentVM roleManagmentVM)
        {
            var user = _userManager.FindByIdAsync(roleManagmentVM.ApplicationUser.Id).GetAwaiter().GetResult();
            var Rolename = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();
            ApplicationUser applicationUser = _unitOFWork.ApplicationUser.Get(u => u.Id == roleManagmentVM.ApplicationUser.Id);

            if (!(roleManagmentVM.ApplicationUser.Role == Rolename))
            {
                if (roleManagmentVM.ApplicationUser.Role != SD.Role_Company)
                {
                    applicationUser.CompanyId = roleManagmentVM.ApplicationUser.CompanyId;
                }
                if (Rolename != SD.Role_Company)
                {
                    applicationUser.CompanyId = null;
                }
                _unitOFWork.ApplicationUser.Update(applicationUser);
                _unitOFWork.Save();

                _userManager.RemoveFromRoleAsync(applicationUser, Rolename).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(applicationUser, roleManagmentVM.ApplicationUser.Role).GetAwaiter().GetResult();
            }
            else
            {
                if(Rolename==SD.Role_Company&& applicationUser.CompanyId!= roleManagmentVM.ApplicationUser.CompanyId)
                {
                    applicationUser.CompanyId = roleManagmentVM.ApplicationUser.CompanyId;
                    _unitOFWork.ApplicationUser.Update(applicationUser);
                    _unitOFWork.Save();
                }
            }
            return RedirectToAction("Index");
        }
        #region API Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> objUserList = _unitOFWork.ApplicationUser.GetAll(includeProperties: "Company").ToList();
            
            foreach (var user in objUserList)
            {
                user.Role = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();
                if (user.Company == null)
                {
                    user.Company=new() { Name = "" };
                }
            }
            return Json(new { data = objUserList });
        }

        public IActionResult LockUnlock([FromBody]string id)
        {
            var objFromDb = _unitOFWork.ApplicationUser.Get(u=>u.Id==id);
            if(objFromDb == null)
            {
                return Json(new{success=false,message="Error While Lockig/Ulocking"});
            }
            if(objFromDb.LockoutEnd != null&& objFromDb.LockoutEnd > DateTime.Now)
            {
                //user is currently locked and we need to unlock them
                objFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                objFromDb.LockoutEnd= DateTime.Now.AddYears(1000);
            }
            _unitOFWork.ApplicationUser.Update(objFromDb);
            _unitOFWork.Save();
            return Json(new { success = true, message = "Done Successfully" });
        }
        #endregion
    }
}


