using BookStore.DataAccess.Data;
using BookStore.DataAccess.Repository.IRepository;
using BookStore.Models;
using BookStore.Models.ViewModels;
using BookStore.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BooksStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _repo;
        private readonly UserManager<IdentityUser> _userManager;
        public UserController(ApplicationDbContext repo, UserManager<IdentityUser> userManager)
        {
            _repo = repo;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RoleManagment(string userId)
        {
            string roleId = _repo.UserRoles.FirstOrDefault(u => u.UserId == userId).RoleId;

            RoleManagementVM roleManagement = new RoleManagementVM()
            {
                User = _repo.ApplicationUsers.Include(u => u.Company).FirstOrDefault(u => u.Id == userId),
                RoleList = _repo.Roles.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Name
                }),
                CompanyList = _repo.Companies.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
            };

            roleManagement.User.Role = _repo.Roles.FirstOrDefault(u => u.Id == roleId).Name;

            return View(roleManagement);
        }

        [HttpPost]
        public IActionResult UpdateRole(RoleManagementVM roleManagement)
        {
            string roleId = _repo.UserRoles.FirstOrDefault(u => u.UserId == roleManagement.User.Id).RoleId;
            string oldRole = _repo.Roles.FirstOrDefault(u => u.Id == roleId).Name;

            if (!(roleManagement.User.Role == oldRole))
            {
                ApplicationUser applicationUser = _repo.ApplicationUsers.FirstOrDefault(u => u.Id == roleManagement.User.Id);

                if (roleManagement.User.Role == SD.Role_Company)
                {
                    applicationUser.CompanyId = roleManagement.User.CompanyId;
                }
                if (oldRole == SD.Role_Company)
                {
                    applicationUser.CompanyId = null;
                }

                _repo.SaveChanges();

                _userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(applicationUser, roleManagement.User.Role).GetAwaiter().GetResult();
            }

            return RedirectToAction("Index");
        }


        #region API Calls

        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> objUserList = _repo.ApplicationUsers.Include(u => u.Company).ToList();

            var userRole = _repo.UserRoles.ToList();
            var roles = _repo.Roles.ToList();

            foreach(var user in objUserList)
            {
                var roleId = userRole.FirstOrDefault(u => u.UserId == user.Id).RoleId;
                user.Role = roles.FirstOrDefault(r => r.Id == roleId).Name;

                if (user.Company == null) user.Company = new() { Name = "" };
            }

            return Json(new { data = objUserList });
        }

        [HttpPost]
        public IActionResult LockUnLock([FromBody] string id)
        {
            var users = _repo.ApplicationUsers.FirstOrDefault(u => u.Id == id);

            if(users == null) 
                return Json(new { success = false, message = "Error while Locking/Unlocking" });

            if (users.LockoutEnd != null && users.LockoutEnd > DateTime.Now)
                users.LockoutEnd = DateTime.Now;
            else if (users.LockoutEnd == null || users.LockoutEnd < DateTime.Now)
                users.LockoutEnd = DateTime.Now.AddYears(100);

            _repo.SaveChanges();

            return Json(new { success = true, message = "Operation Successful" });
        }

        #endregion

    }
}
