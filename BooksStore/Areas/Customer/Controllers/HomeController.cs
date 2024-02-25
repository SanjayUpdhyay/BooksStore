using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using BookStore.Models;
using BookStore.DataAccess.Repository.IRepository;

namespace BooksStore.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _repo;
        public HomeController(IUnitOfWork repo)
        {
            _repo = repo;
        }

        public IActionResult Index()
        {
            List<Product> objProjectList = _repo.Product.GetAll(includeProperties: "Category").ToList();
            return View(objProjectList);
        }

        public IActionResult Details(int? productId)
        {
            Product objProject = _repo.Product.Get(product => product.Id == productId,includeProperties: "Category");
            return View(objProject);
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