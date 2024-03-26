using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Hosting;
using NuGet.Protocol.Plugins;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Threads.Data;
using Threads.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;


namespace Threads.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext db;
        private IWebHostEnvironment _env;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public UsersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IWebHostEnvironment env, SignInManager<ApplicationUser> signInManager)
        {
            this.db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _env = env;
            _signInManager = signInManager;
        }
        //oricine poate sa vada lista utilizatorilor din platforma
        public IActionResult Index()
        {
            /*mesaj de validare*/
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"].ToString();
                ViewBag.Alert = TempData["messageType"].ToString();
            }

            IOrderedQueryable<ApplicationUser> users = db.Users.Where(user => user.Id != _userManager.GetUserId(User)).OrderBy(user => user.UserName);
            var search = "";
            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {

                // eliminam spatiile libere  
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim();
                
                // Cautare in UserName 
                List<string> userIds = db.Users.Where(at => at.UserName.Contains(search) 
                                                            || at.FirstName.Contains(search)
                                                            || at.LastName.Contains(search)
                                                     ).Select(a => a.Id).ToList();

                users = (IOrderedQueryable<ApplicationUser>)db.Users.Where(user => userIds.Contains(user.Id));
                
            }
            SetAccessRights();
            ViewBag.Users = users;
            return View();
        }

        //oricine poate sa acceseze profilul unui user
        public IActionResult ShowPosts(string id)
        {

            SetAccessRights();
            /*mesaj de validare*/
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"].ToString();
                ViewBag.Alert = TempData["messageType"].ToString();
            }
            
            ApplicationUser user = db.Users
                                    .Include("Posts")
                                    .Include("Posts.Group")
                                    .Include("Posts.Comments")
                                    .Where(u => u.Id == id)
                                    .First();
            //am nevoie sa nu consider si postarile din grupuri
            user.Posts = user.Posts.Where(p => p.GroupId == null).ToList();
            user.Posts = user.Posts.OrderByDescending(p => p.Date).ToList();
            ViewBag.posts = user.Posts;
            if(user.Visibility == false && user.Id != _userManager.GetUserId(User))
            {
                //verific daca utilizatorul curent a trimis cerere de urmarire deja 
                var followRequest = db.FollowRequests
                                        .Where(fr => fr.SenderId == _userManager.GetUserId(User) && fr.ReceiverId == user.Id)
                                        .FirstOrDefault();

                //daca nu a trimis cerere de urmarire, ii afisez utilizatorului butonul de cerere de urmarire
                if(followRequest == null)
                {
                    ViewBag.Status = "none";
                    ViewBag.Button = "Trimite cerere de urmărire";
                    ViewBag.Action = "SendRequest";
                }
                //daca a trimis cerere de urmarire, ii afisez utilizatorului butonul de anulare a cererii de urmarire
                else
                {
                    if(followRequest.Status == "pending")
                    {
                        ViewBag.Status = "pending";
                        ViewBag.Button = "Anulează cererea de urmarire";
                        ViewBag.Action = "DeleteRequest";
                    }
                    else
                    {
                        ViewBag.Status = "accepted";
                        ViewBag.Button = "none";
                        ViewBag.Action = "none";
                    }
                }
            }

            user.Posts = user.Posts.OrderByDescending(p => p.Date).ToList();
            ViewBag.ActiveTab = "Posts";
            ViewBag.User = user;
            return View(user);
        }

        //oricine poate sa acceseze profilul unui user
        public IActionResult ShowGroups(string id)
        {

            /*Cautam utilizatorul si toate grupurile din care acesta face parte */

            ApplicationUser user =db.Users
                                    .Include("GroupUsers")
                                    .Include("GroupUsers.Group")
                                    .Include("GroupUsers.Group.User")
                                    .Include("GroupUsers.Group.GroupUsers")
                                    .Where(u => u.Id == id)
                                    .First();
            /*Salvam intr-o lista toate grupurile din care face parte utilizatorul si trimitem in View */
            var groups = user.GroupUsers.Select(g => g.Group).ToList();
            /*Salvam intr-o lista vector de utilizatori, toti userii fiecarui grup*/
            foreach (var group in groups)
            {
                group.GroupUsers = db.GroupUsers
                                    .Include("User")
                                    .Where(gu => gu.GroupId == group.Id)
                                    .ToList();
            }
            ViewBag.Groups = groups;
            if(groups.Count == 0)
            {
                ViewBag.Message = "Utilizatorul nu face parte din niciun grup.";
                ViewBag.Alert = "alert-warning";
            }
            ViewBag.User = user;
            ViewBag.ActiveTab = "Groups";
            if (user.Visibility == false && user.Id != _userManager.GetUserId(User))
            {
                //verific daca utilizatorul curent a trimis cerere de urmarire deja 
                var followRequest = db.FollowRequests
                                        .Where(fr => fr.SenderId == _userManager.GetUserId(User) && fr.ReceiverId == user.Id)
                                        .FirstOrDefault();

                //daca nu a trimis cerere de urmarire, ii afisez utilizatorului butonul de cerere de urmarire
                if (followRequest == null)
                {
                    ViewBag.Status = "none";
                    ViewBag.Button = "Trimite cerere de urmărire";
                    ViewBag.Action = "SendRequest";
                }
                //daca a trimis cerere de urmarire, ii afisez utilizatorului butonul de anulare a cererii de urmarire
                else
                {
                    if (followRequest.Status == "pending")
                    {
                        ViewBag.Status = "pending";
                        ViewBag.Button = "Anulează cererea de urmarire";
                        ViewBag.Action = "DeleteRequest";
                    }
                    else
                    {
                        ViewBag.Status = "accepted";
                        ViewBag.Button = "none";
                        ViewBag.Action = "none";
                    }
                }
            }
            SetAccessRights();
            return View(user);
        }

        [Authorize(Roles = "User,Admin")]
        public IActionResult Edit(string id)
        {  SetAccessRights();
            ApplicationUser user = db.Users
                                    .Include("Posts")
                                    .Include("Posts.Group")
                                    .Include("Posts.Comments")
                                    .Where(u => u.Id == id)
                                    .First();
            ViewBag.ActiveTab = "Edit";
            if (user.Id == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                ViewBag.User = user;
                SetAccessRights();
                return View(user);
            }

            else
            {
                TempData["message"] = "Nu aveti dreptul sa modificati datele unui alt utilizator!";
                TempData["messageType"] = "alert-danger";
                /*++ id ul utilizatorului curent*/

                return Redirect("/Users/ShowPosts/" + id);
            }
        }

        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        public async Task<IActionResult> Edit(string id, ApplicationUser newData, IFormFile ProfilePicture, IFormFile CoverPicture)
        {
            SetAccessRights();
            ApplicationUser user = db.Users.Find(id);
            ViewBag.ActiveTab = "Edit";
            //mic truc ca sa scap de mesajul de validare al framework-ului pentru imagine
            ModelState.Remove("ProfilePicture"); 
            ModelState.Remove("CoverPicture");
            var regex = "^[\\w-\\.]+@([\\w-]+\\.)+[\\w-]{2,4}$";
            var match = Regex.Match(newData.Email, regex);

            if (!match.Success)
            {
                ModelState.AddModelError("Email", "Email-ul nu este valid!");
            }

            if (ModelState.IsValid == true)
            {
                /*Prima data verific daca am facut vreo modificare, pentru a afisa mesajul de validare dupa cazul corect*/
                if (user.UserName == newData.UserName &&
                user.Email == newData.Email &&
                user.FirstName == newData.FirstName &&
                user.LastName == newData.LastName &&
                user.Biografie == newData.Biografie &&
                user.Visibility == newData.Visibility &&
                CoverPicture == null &&
                ProfilePicture == null)
                {
                    // Nicio modificare nu a fost făcută
                    TempData["message"] = "Nu ai făcut nicio modificare.";
                    TempData["messageType"] = "alert-warning";

                    return Redirect("/Users/ShowPosts/" + id);
                }


                if (user.Id == _userManager.GetUserId(User))
                {
                    if (!IsUsernameUnique(newData.UserName, newData.Id))
                    {
                        ModelState.AddModelError("UserName", "Username-ul exista deja!");
                        return View(newData);
                    }

                    //Adaugarea fotografiei de profil in baza de date 
                    if (ProfilePicture != null && ProfilePicture.Length > 0)
                    {
                        var storagePath = Path.Combine(_env.WebRootPath, "images", ProfilePicture.FileName);
                        var databaseFileName = "/images/" + ProfilePicture.FileName;
                        using (var fileStream = new FileStream(storagePath, FileMode.Create))
                        {
                            await ProfilePicture.CopyToAsync(fileStream);
                        }
                        user.ProfilePicture = databaseFileName;
                    }
                   

                    //Adaugarea imaginii de coperta in baza de date
                    
                    if (CoverPicture != null && CoverPicture.Length > 0)
                    {
                        var storagePath = Path.Combine(_env.WebRootPath, "images", CoverPicture.FileName);
                        var databaseFileName = "/images/" + CoverPicture.FileName;
                        using (var fileStream = new FileStream(storagePath, FileMode.Create))
                        {
                            await CoverPicture.CopyToAsync(fileStream);
                        }
                        user.CoverPicture = databaseFileName;
                    }
                    
                    user.UserName = newData.UserName;
                    user.Email = newData.Email;
                    user.FirstName = newData.FirstName;
                    user.LastName = newData.LastName;
                    user.Biografie = newData.Biografie;
                    user.Visibility = newData.Visibility ?? user.Visibility;

                    db.SaveChanges();
                    TempData["message"] = "Datele au fost modificate.";
                    TempData["messageType"] = "alert-success";
                    return Redirect("/Users/ShowPosts/" + id);
                }
                else
                {
                    TempData["message"] = "Nu aveti dreptul sa modificati datele unui alt utilizator!";
                    TempData["messageType"] = "alert-danger";
                    return Redirect("/Users/ShowPosts/"+ id);
                }

            }

            else
            {
                return View(newData);
            }
        }

        [HttpPost]
        [Authorize(Roles="User,Admin")]
        public async Task<IActionResult> SendRequest(string id)
        {
            var senderId = _userManager.GetUserId(User);
            // Verificare dacă există deja o cerere de urmărire între expeditor și destinatar
            var existingRequest = db.FollowRequests
                .FirstOrDefault(fr => fr.SenderId == senderId && fr.ReceiverId == id && fr.Status=="pending");

            if (existingRequest != null)
            {
                TempData["message"] = "Cererea de urmărire a fost deja trimisă!";
                TempData["messageType"] = "alert-warning";
                return Redirect("/Users/ShowPosts/" +   id );
            }
            var followRequest = new FollowRequest
            {
                SenderId = senderId,
                ReceiverId = id,
                Status = "pending",
                Sender = await _userManager.FindByIdAsync(senderId),
                Receiver = await _userManager.FindByIdAsync(id)
            };

            db.FollowRequests.Add(followRequest);
            await db.SaveChangesAsync();
            TempData["message"] = "Cererea de urmărire a fost trimisă cu succes!";
            TempData["messageType"] = "alert-success";

            return Redirect("/Users/ShowPosts/" + id);
        }

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> DeleteRequest(string id)
        {
            var senderId = _userManager.GetUserId(User);
            // Verificare dacă există deja o cerere de urmărire între expeditor și destinatar
            var existingRequest = db.FollowRequests
                .FirstOrDefault(fr => fr.SenderId == senderId && fr.ReceiverId == id && fr.Status == "pending");

            if (existingRequest == null)
            {
                TempData["message"] = "Cererea de urmărire nu există!";
                TempData["messageType"] = "alert-warning";
                return Redirect("/Users/ShowPosts/" + id);
            }

            db.FollowRequests.Remove(existingRequest);
            await db.SaveChangesAsync();
            TempData["message"] = "Cererea de urmărire a fost anulată cu succes!";
            TempData["messageType"] = "alert-success";

            return Redirect("/Users/ShowPosts/" + id);
        }

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> AcceptRequest(string id)
        {
            //usesrul curent este receiverul

            var receiverId = _userManager.GetUserId(User);

            // Verificare dacă există intr-adevar o cerere de urmărire între expeditor și destinatar
            var existingRequest = db.FollowRequests
                .FirstOrDefault(fr => fr.SenderId == id && fr.ReceiverId == receiverId && fr.Status == "pending");

            if (existingRequest == null)
            {
                TempData["message"] = "Cererea de urmărire nu există!";
                TempData["messageType"] = "alert-warning";
                return Redirect("/Users/ShowPosts/" + id);
            }

            existingRequest.Status = "accepted";
            db.FollowRequests.Update(existingRequest);
            await db.SaveChangesAsync();
            TempData["message"] = "Cererea de urmărire a fost acceptată cu succes!";
            TempData["messageType"] = "alert-success";

            return Redirect("/FollowRequests/PendingRequestsIndex");
        }

        
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> DeclineRequest(string id)
        {
            //usesrul curent este receiverul

            var receiverId = _userManager.GetUserId(User);

            // Verificare dacă există intr-adevar o cerere de urmărire între expeditor și destinatar
            var existingRequest = db.FollowRequests
                .FirstOrDefault(fr => fr.SenderId == id && fr.ReceiverId == receiverId && fr.Status == "pending");

            if (existingRequest == null)
            {
                TempData["message"] = "Cererea de urmărire nu există!";
                TempData["messageType"] = "alert-warning";
                return Redirect("/Users/ShowPosts/" + id);
            }

            db.FollowRequests.Remove(existingRequest);
            await db.SaveChangesAsync();
            TempData["message"] = "Cererea de urmărire a fost respinsă.";
            TempData["messageType"] = "alert-success";

            return Redirect("/FollowRequests/PendingRequestsIndex");
        }



        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            ApplicationUser user = db.Users
                                   .Include("Comments")
                                   .Include("Posts")
                                   .Include("Posts.Comments")
                                   .Include("GroupUsers")
                                   .Include("GroupUsers.Group")
                                   .Include("GroupUsers.Group.Posts")
                                   .Include("GroupUsers.Group.Posts.Comments")
                                   .Where(u => u.Id == id)
                                   .First();

            if (user == null)
            {
                TempData["message"] = "Userul nu a fost găsit.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            if (!User.IsInRole("Admin") && _userManager.GetUserId(User) != user.Id)
            {
                TempData["message"] = "Nu aveți dreptul să ștergeți un alt cont";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            // Explicitly load related entities
            await db.Entry(user)
                .Collection(u => u.Comments)
                .LoadAsync();

            await db.Entry(user)
                .Collection(u => u.Posts)
                .LoadAsync();

            await db.Entry(user)
                .Collection(u => u.GroupUsers)
                    .Query()
                        .Include(gu => gu.Group)
                            .ThenInclude(g => g.Posts)
                                .ThenInclude(p => p.Comments)
                    .LoadAsync();

            // Remove related FollowRequests
            var followRequests = db.FollowRequests
                .Where(fr => fr.SenderId == user.Id || fr.ReceiverId == user.Id)
                .ToList();

            db.FollowRequests.RemoveRange(followRequests);
            // Remove user
            db.ApplicationUsers.Remove(user);
            await db.SaveChangesAsync();

            // Redirect or sign out based on the user's role
            if (User.IsInRole("Admin") || _userManager.GetUserId(User) == user.Id)
            {
                TempData["message"] = "Contul a fost șters";
                TempData["messageType"] = "alert-success";

                if (_userManager.GetUserId(User) == user.Id)
                {
                    await _signInManager.SignOutAsync();
                    return Redirect("/Home/Index");
                }

                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }


        private void SetAccessRights()
        {
            ViewBag.EsteAdmin = User.IsInRole("Admin");
            ViewBag.UserCurent = _userManager.GetUserId(User);
        }

        private bool IsUsernameUnique(string username, string id)
        {
            var users = from user in db.Users.OfType<ApplicationUser>()
                        where user.UserName == username
                        select user;
            if (users.Count() == 0 || (users.Count() == 1 && users.First().Id == id))
            {
                return true;
            }

            return false;
        }

    }
}