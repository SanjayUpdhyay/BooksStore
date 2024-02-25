using BookStore.DataAccess.Data;
using BookStore.DataAccess.Repository.IRepository;
using BookStore.Models;
using BookStore.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BooksStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _repo;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork repo, IWebHostEnvironment webHostEnvironment)
        {
            _repo = repo;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            List<Product> objProjectList = _repo.Product.GetAll(includeProperties: "Category").ToList();
            return View(objProjectList);
        }

        #region Upsert
        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new()
            {
                CategoryList = _repo.Category.GetAll().Select(u => new SelectListItem      // Convert by help of Projection
                {
                    Text = u.Name,
                    Value = u.Id.ToString(),
                }),

                Product = new Product(),
            };

            if (id == null || id == 0) return View(productVM);   // Create

            productVM.Product = _repo.Product.Get(product => product.Id == id);

            return View(productVM);   // Update
        }

        [HttpPost]
        public IActionResult Upsert(ProductVM productVM, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;

                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\product");

                    if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, productVM.Product.ImageUrl.TrimStart('\\'));  // Delete the Image

                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                    using (var filestream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(filestream);       // Added the Image
                    }

                    productVM.Product.ImageUrl = @"\images\product\" + fileName;
                }

                if (productVM.Product.Id == 0)
                {
                    _repo.Product.Add(productVM.Product);    // Adding to DB.
                    TempData["success"] = "Product created Successfully";
                } 
                else
                {
                    _repo.Product.Update(productVM.Product);  // Update to DB.
                    TempData["success"] = "Product updated Successfully";
                } 

                _repo.Save();
                return RedirectToAction("Index");
            }
            else
            {
                productVM.CategoryList = _repo.Category.GetAll().Select(u => new SelectListItem      // Convert by help of Projection
                {
                    Text = u.Name,
                    Value = u.Id.ToString(),
                });

                return View(productVM);
            }
        }

        #endregion

        #region API Calls

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objProjectList = _repo.Product.GetAll(includeProperties: "Category").ToList();

            return Json(new { data = objProjectList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productToBeDeleted= _repo.Product.Get(x => x.Id == id);

            if(productToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            else
            {
                if (!string.IsNullOrEmpty(productToBeDeleted.ImageUrl))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, productToBeDeleted.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                _repo.Product.Remove(productToBeDeleted);
                _repo.Save();
                return Json(new { success = true, message = "delete Successful" });
            }
        }
        #endregion

    }
}
