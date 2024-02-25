using BookStore.DataAccess.Data;
using BookStore.DataAccess.Repository.IRepository;
using BookStore.Models;
using Microsoft.AspNetCore.Mvc;

namespace BooksStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _repo;
        public CategoryController(IUnitOfWork repo)
        {
            _repo = repo;
        }
        public IActionResult Index()
        {
            List<Category> objCategoryList = _repo.Category.GetAll().ToList();
            return View(objCategoryList);
        }

        #region Upsert

        public IActionResult Upsert(int? id)
        {
            Category category = new Category();
            if (id == null || id == 0) return View(category);   // Create

            category = _repo.Category.Get(category => category.Id == id);

            return View(category);   // Update
        }

        [HttpPost]
        public IActionResult Upsert(Category category)
        {
            if (ModelState.IsValid)
            {
                if(category.Id == 0)
                {
                    _repo.Category.Add(category);
                    TempData["success"] = "Category created Successfully";
                } 
                else
                {
                    _repo.Category.Update(category);
                    TempData["success"] = "Category Updated Successfully";
                } 
                _repo.Save();
                return RedirectToAction("Index");
            }
            return View();
        }

        #endregion

        #region  Get & Delete API

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Category> objProjectList = _repo.Category.GetAll().ToList();

            return Json(new { data = objProjectList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var categoryToBeDeleted = _repo.Category.Get(x => x.Id == id);

            if (categoryToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            else
            {
                _repo.Category.Remove(categoryToBeDeleted);
                _repo.Save();
                return Json(new { success = true, message = "delete Successful" });
            }
        }

        #endregion

    }
}
