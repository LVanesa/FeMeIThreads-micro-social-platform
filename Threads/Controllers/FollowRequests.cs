using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Threads.Data;
using Threads.Models;

namespace Threads.Controllers
{
    public class FollowRequests : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public FollowRequests(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [Authorize (Roles = "Admin,User")]
        public IActionResult PendingRequestsIndex()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"].ToString();
                ViewBag.Alert = TempData["messageType"].ToString();
            }
            // Obține ID-ul utilizatorului curent
            var currentUserId = _userManager.GetUserId(User);

            // Obține toate cererile de urmărire primite de utilizatorul curent
            var pendingRequests = db.FollowRequests
                .Include(fr => fr.Sender)  // Include detalii despre expeditor
                .Where(fr => fr.ReceiverId == currentUserId && fr.Status == "pending")
                .ToList();

            if (pendingRequests.Count == 0)
            {
                ViewBag.Info = "Momentan nu ai nicio cerere de urmărire.";
            }

            return View(pendingRequests);
        }
    }
}
