﻿using BookStore.DataAccess.Repository.IRepository;
using BookStore.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BooksStore.ViewComponents
{
    public class ShoppingCartViewComponent : ViewComponent
    {
        private readonly IUnitOfWork _repo;

        public ShoppingCartViewComponent(IUnitOfWork repo)
        {
            _repo = repo;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null)
            {
                if(HttpContext.Session.GetInt32(SD.SessionCart) == null)
                    HttpContext.Session.SetInt32(SD.SessionCart, _repo.ShoppingCart.GetAll(s => s.ApplicationUserId == claim.Value).Count());   // Set Session
            }
            else HttpContext.Session.Clear();

            return View(HttpContext.Session.GetInt32(SD.SessionCart));
        }
    }
}
