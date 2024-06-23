using BookStore.DataAccess.Repository.IRepository;
using BookStore.Models;
using BookStore.Models.ViewModels;
using BookStore.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BooksStore.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _repo;

        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork repo)
        {
            _repo = repo;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList = _repo.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Quantity;
            }

            return View(ShoppingCartVM);
        }

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList = _repo.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };

            ShoppingCartVM.OrderHeader.ApplicationUser = _repo.ApplicationUser.Get(a => a.Id == userId);

            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Quantity;
            }

            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPost()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM.ShoppingCartList = _repo.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");

            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = userId;

            ApplicationUser applicationUser = _repo.ApplicationUser.Get(a => a.Id == userId);

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                // it is a regular customer account
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                // it is a regular Company account
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Quantity;
            }

            _repo.OrderHeader.Add(ShoppingCartVM.OrderHeader);  // Saving Data to OrderHeaders Table
            _repo.Save();

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    ProductId = cart.ProductId,
                    Price = cart.Price,
                    Quantity = cart.Quantity,
                };

                _repo.OrderDetail.Add(orderDetail);  // Saving Data to Orderdetails Table
                _repo.Save();
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                // it is a regular customer account we need to capture payment
                return PaymentAndSave();
            }

            return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });

        }

        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _repo.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");

            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                // this order by customer

                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _repo.OrderHeader.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                    _repo.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);

                    List<ShoppingCart> shoppingCarts = _repo.ShoppingCart.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
                    _repo.ShoppingCart.RemoveRange(shoppingCarts);

                    _repo.Save();
                }

                HttpContext.Session.Clear();   // Clear Session
            }
            return View();
        }

        public IActionResult Plus(int cardId)
        {
            var cartToDb = _repo.ShoppingCart.Get(s => s.Id == cardId);
            cartToDb.Quantity += 1;
            _repo.ShoppingCart.Update(cartToDb);
            _repo.Save();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cardId)
        {
            var cartToDb = _repo.ShoppingCart.Get(s => s.Id == cardId, tracked: true);

            if (cartToDb.Quantity == 1)
            {
                HttpContext.Session.SetInt32(SD.SessionCart, _repo.ShoppingCart.GetAll(s => s.ApplicationUserId == cartToDb.ApplicationUserId).Count() - 1);  // set session
                _repo.ShoppingCart.Remove(cartToDb);
            }
            else
            {
                cartToDb.Quantity -= 1;
                _repo.ShoppingCart.Update(cartToDb);
            }
            _repo.Save();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cardId)
        {
            var cartToDb = _repo.ShoppingCart.Get(s => s.Id == cardId, tracked: true);
            HttpContext.Session.SetInt32(SD.SessionCart, _repo.ShoppingCart.GetAll(s => s.ApplicationUserId == cartToDb.ApplicationUserId).Count() - 1);  // set session
            _repo.ShoppingCart.Remove(cartToDb);
            _repo.Save();

            return RedirectToAction(nameof(Index));
        }

        private double GetPriceBasedQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Quantity <= 50) return shoppingCart.Product.Price;
            else
            {
                if (shoppingCart.Quantity <= 100) return shoppingCart.Product.Price50;
                else return shoppingCart.Product.Price100;
            }

        }

        private StatusCodeResult PaymentAndSave()
        {
            var domin = Request.Scheme + "://" + Request.Host.Value + "/";
            var options = new Stripe.Checkout.SessionCreateOptions
            {
                SuccessUrl = domin + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
                CancelUrl = domin + $"customer/cart/index",
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>(),
                ShippingAddressCollection = new SessionShippingAddressCollectionOptions
                {
                    AllowedCountries = new List<string> { "IN"},
                },
                Mode = "payment",
            };

            foreach (var item in ShoppingCartVM.ShoppingCartList)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100),
                        Currency = "inr",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Quantity,
                };

                options.LineItems.Add(sessionLineItem);
            }

            var service = new Stripe.Checkout.SessionService();
            Session session = service.Create(options);

            _repo.OrderHeader.UpdateStripePaymentId(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _repo.Save();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }
    }
}
