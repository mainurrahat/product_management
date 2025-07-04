﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using productManagement.Data;
using productManagement.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace productManagement.Controllers
{
    
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderController> _logger;

        public OrderController(ApplicationDbContext context, ILogger<OrderController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int productId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                _logger.LogWarning($"Product not found: {productId}");
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index", "Product");
            }

            var order = new Order
            {
                ProductId = product.Id,
                Product = product
            };

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order)
        {
            if (!ModelState.IsValid)
            {
                order.Product = await _context.Products.FindAsync(order.ProductId);
                return View(order);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "You must be logged in to place an order.";
                return RedirectToAction("Login", "Account");
            }

            order.UserId = userId;
            order.OrderDate = DateTime.UtcNow; // Use UTC for consistency

            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Order placed successfully!";
                return RedirectToAction("Details", new { id = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order");
                ModelState.AddModelError("", "An error occurred while placing your order.");
                order.Product = await _context.Products.FindAsync(order.ProductId);
                return View(order);
            }
        }

        // Add other actions like Index, Details, etc.
    }
}