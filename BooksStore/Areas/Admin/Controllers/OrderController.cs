using BookStore.DataAccess.Repository.IRepository;
using BookStore.Models;
using BookStore.Models.ViewModels;
using BookStore.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BooksStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _repo;

        [BindProperty]
        public OrderVM OrderVM { get; set; }
        public OrderController(IUnitOfWork repo)
        {
            _repo = repo;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int orderId)
        {
            OrderVM = new()
            {
                orderHeader = _repo.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                orderDetails = _repo.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties:"Product").ToList(),
            };

            return View(OrderVM);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetails()
        {
            var orderHeaderFromDb = _repo.OrderHeader.Get(u => u.Id == OrderVM.orderHeader.Id);

            orderHeaderFromDb.Name = OrderVM.orderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.orderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.orderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.orderHeader.City;
            orderHeaderFromDb.State = OrderVM.orderHeader.State;
            orderHeaderFromDb.PostalCode = OrderVM.orderHeader.PostalCode;

            if(!string.IsNullOrEmpty(OrderVM.orderHeader.Carrier))
                orderHeaderFromDb.Carrier = OrderVM.orderHeader.Carrier;
            if (!string.IsNullOrEmpty(OrderVM.orderHeader.TrackingNumber))
                orderHeaderFromDb.Carrier = OrderVM.orderHeader.TrackingNumber;

            _repo.OrderHeader.Update(orderHeaderFromDb);
            _repo.Save();

            TempData["Success"] = "Order Details Update Successfully";

            return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
        }

        [HttpPost]
        [ActionName("Details")]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult Details_Pay_Now()
        {
            OrderVM.orderHeader = _repo.OrderHeader.Get(u => u.Id == OrderVM.orderHeader.Id, includeProperties: "ApplicationUser");
            OrderVM.orderDetails = _repo.OrderDetail.GetAll(u => u.OrderHeaderId == OrderVM.orderHeader.Id, includeProperties: "Product").ToList();

            PaymentAndSave();

            TempData["Success"] = "Order Details Update Successfully";

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.orderHeader.Id });
        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader orderHeader = _repo.OrderHeader.Get(u => u.Id == orderHeaderId);

            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                // this order by Company

                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _repo.OrderHeader.UpdateStripePaymentId(orderHeader.Id, session.Id, session.PaymentIntentId);
                    _repo.OrderHeader.UpdateStatus(orderHeader.Id, orderHeader.OrderStatus, SD.PaymentStatusApproved);

                    _repo.Save();
                }
            }
            return View(orderHeaderId);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _repo.OrderHeader.UpdateStatus(OrderVM.orderHeader.Id, SD.StatusInProcess);
            _repo.Save();

            TempData["Success"] = "Order Details Update Successfully";

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.orderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            var orderHeader = _repo.OrderHeader.Get(u => u.Id == OrderVM.orderHeader.Id);

            orderHeader.TrackingNumber = OrderVM.orderHeader.TrackingNumber;
            orderHeader.Carrier = OrderVM.orderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;

            if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
            }

            _repo.OrderHeader.Update(orderHeader);
            _repo.Save();

            TempData["Success"] = "Order shipped Successfully";

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.orderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            var orderHeader = _repo.OrderHeader.Get(u => u.Id == OrderVM.orderHeader.Id);

            if(orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                PaymentRefund();
                _repo.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
                _repo.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);

            _repo.Save();

            TempData["Success"] = "Order cancelled Successfully";

            return RedirectToAction(nameof(Details), new { orderId = OrderVM.orderHeader.Id });
        }

        private StatusCodeResult PaymentAndSave()
        {
            var domin = "https://localhost:7113/";
            var options = new Stripe.Checkout.SessionCreateOptions
            {
                SuccessUrl = domin + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVM.orderHeader.Id}",
                CancelUrl = domin + $"admin/order/details?orderId={OrderVM.orderHeader.Id}",
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>(),
                ShippingAddressCollection = new SessionShippingAddressCollectionOptions
                {
                    AllowedCountries = new List<string> { "IN" },
                },
                Mode = "payment",
            };

            foreach (var item in OrderVM.orderDetails)
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

            _repo.OrderHeader.UpdateStripePaymentId(OrderVM.orderHeader.Id, session.Id, session.PaymentIntentId);
            _repo.Save();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        private void PaymentRefund()
        {
            var options = new RefundCreateOptions
            {
                Reason = RefundReasons.RequestedByCustomer,
                PaymentIntent = OrderVM.orderHeader.PaymentIntentId
            };

            var service = new RefundService();
            Refund refund = service.Create(options);
        }

        #region API Calls

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> objOrderHeaderList;

            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                objOrderHeaderList = _repo.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                objOrderHeaderList = _repo.OrderHeader.GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser").ToList();
            }
            

            switch (status)
            {
                case "inprocess":
                    objOrderHeaderList = objOrderHeaderList.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "pending":
                    objOrderHeaderList = objOrderHeaderList.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "completed":
                    objOrderHeaderList = objOrderHeaderList.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objOrderHeaderList = objOrderHeaderList.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;

            }

            return Json(new { data = objOrderHeaderList });
        }

        #endregion
    }
}
