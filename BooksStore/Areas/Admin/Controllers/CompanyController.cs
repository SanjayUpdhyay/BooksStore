using BookStore.DataAccess.Data;
using BookStore.DataAccess.Repository.IRepository;
using BookStore.Models;
using BookStore.Models.ViewModels;
using BookStore.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BooksStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _repo;
        public CompanyController(IUnitOfWork repo)
        {
            _repo = repo;
        }
        public IActionResult Index()
        {
            List<Company> CompanyList = _repo.Company.GetAll().ToList();
            return View(CompanyList);
        }

        #region Upsert
        public IActionResult Upsert(int? id)
        {

            if (id == null || id == 0) return View(new Company());   // Create

            Company company = _repo.Company.Get(Company => Company.Id == id);

            return View(company);   // Update
        }

        [HttpPost]
        public IActionResult Upsert(Company company)
        {
            if (ModelState.IsValid)
            {
                if (company.Id == 0)
                {
                    _repo.Company.Add(company);    // Adding to DB.
                    TempData["success"] = "Company created Successfully";
                } 
                else
                {
                    _repo.Company.Update(company);  // Update to DB.
                    TempData["success"] = "Company updated Successfully";
                } 

                _repo.Save();
                return RedirectToAction("Index");
            }
            else
            {
                return View(company);
            }
        }

        #endregion

        #region API Calls

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> objProjectList = _repo.Company.GetAll().ToList();

            return Json(new { data = objProjectList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var CompanyToBeDeleted= _repo.Company.Get(x => x.Id == id);

            if(CompanyToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            else
            {
                _repo.Company.Remove(CompanyToBeDeleted);
                _repo.Save();
                return Json(new { success = true, message = "delete Successful" });
            }
        }
        #endregion

    }
}
