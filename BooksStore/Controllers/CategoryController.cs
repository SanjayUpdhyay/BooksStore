using BookStore.DataAccess.Data;
using BookStore.DataAccess.Repository.IRepository;
using BookStore.Models;
using Microsoft.AspNetCore.Mvc;

namespace BooksStore.Controllers
{
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

        #region Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _repo.Category.Add(category);
                _repo.Save();
                TempData["success"] = "Category created Successfully";
                return RedirectToAction("Index");
            }
            return View();
        }

        #endregion

        #region Edit
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();

            Category? category = _repo.Category.Get(category => category.Id == id);
            return View(category);
        }

        [HttpPost]
        public IActionResult Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                _repo.Category.Update(category);
                _repo.Save();
                TempData["success"] = "Category Edit Successfully";
                return RedirectToAction("Index");
            }
            return View();
        }

        #endregion

        #region Delete
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();

            Category? category = _repo.Category.Get(category => category.Id == id);
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult Delete(Category category)
        {
            if (category.Equals == null) return NotFound();

            _repo.Category.Remove(category);
            _repo.Save();
            TempData["success"] = "Category Deleted Successfully";
            return RedirectToAction("Index");
        }

        #endregion

    }
}
