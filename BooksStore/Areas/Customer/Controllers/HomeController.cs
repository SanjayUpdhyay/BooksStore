using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using BookStore.Models;
using BookStore.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BookStore.Utility;

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
            ShoppingCart shoppingCart = new()
            {
                Product = _repo.Product.Get(product => product.Id == productId, includeProperties: "Category"),
                Quantity = 1,
                ProductId = productId.GetValueOrDefault()
            };
            
            return View(shoppingCart);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            shoppingCart.ApplicationUserId = userId;

            var shoppingCarts = _repo.ShoppingCart.Get(s => s.ProductId == shoppingCart.ProductId && s.ApplicationUserId == userId);

            if(shoppingCarts == null)
            {
                _repo.ShoppingCart.Add(shoppingCart);
            }
            else
            {
                shoppingCarts.Quantity += shoppingCart.Quantity;
                _repo.ShoppingCart.Update(shoppingCarts);
            }
            
            _repo.Save();
            
            HttpContext.Session.SetInt32(SD.SessionCart, _repo.ShoppingCart.GetAll(s => s.ApplicationUserId == userId).Count());   // Set Session

            TempData["success"] = "Cart Updated Successfully";

            return RedirectToAction(nameof(Index));
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