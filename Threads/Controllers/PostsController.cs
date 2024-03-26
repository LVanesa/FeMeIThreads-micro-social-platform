using Ganss.Xss;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;
using Threads.Data;
using Threads.Models;

namespace SocialApplication.Controllers
{
  
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public PostsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }


        public IActionResult Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }
            var currentUserId = _userManager.GetUserId(User);
            //daca este admin
            
            if (User.IsInRole("Admin"))
            {
                var posts = db.Posts
                    .Include(p => p.User)
                    .Include(p => p.Comments)
                    .Include(p => p.Group)
                    .Where(p => p.GroupId == null);

                // Ordonare
                posts = posts.OrderByDescending(p => p.Date);

                foreach (var post in posts)
                {
                    if (post.User.ProfilePicture == null)
                    {
                        post.User.ProfilePicture = "/images/ghost.png";
                    }
                }
                ViewBag.Posts = posts;
                SetAccessRights();
                return View();
            }
            else
            {
                var posts = db.Posts
                .Include(p => p.User)
                .Include(p => p.Comments)
                .Include(p => p.Group)
                .Where(p =>
                    p.GroupId == null &&
                    p.User.Visibility == true ||
                    (p.User.Visibility == false &&
                    db.FollowRequests.Any(f =>
                        f.SenderId == currentUserId &&
                        f.ReceiverId == p.User.Id &&
                        f.Status == "accepted")) || p.User.Id == currentUserId);

                // Ordonare
                posts = posts.OrderByDescending(p => p.Date);

                foreach (var post in posts)
                {
                    if (post.User.ProfilePicture == null)
                    {
                        post.User.ProfilePicture = "/images/ghost.png";
                    }
                }
                ViewBag.Posts = posts;
                SetAccessRights();
                return View();
            }
            
        }


        public IActionResult Show(int id)
        {
            Post p = db.Posts.Include("Group")
                             .Include("User")
                             .Include("Comments")
                             .Include("Comments.User")
                             .Where(p => p.Id == id)
                             .First();

            SetAccessRights();

            //verific daca userul care a facut postarea are profilul vizibil, daca nu, verific daca userul curent a trimis o cerere care i-a fost acceptata userului care a facut postarea
            if (p.User.Visibility == false && User.IsInRole("Admin")==false)
            {
                var currentUserId = _userManager.GetUserId(User);
                if (db.FollowRequests.Any(f =>
                                       f.SenderId == currentUserId &&
                                                              f.ReceiverId == p.User.Id &&
                                                                                     f.Status == "accepted") || p.User.Id == currentUserId)
                {
                    return View(p);
                }

                else
                {
                    TempData["message"] = "Nu aveți dreptul să vizualizați această postare.";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                if (TempData.ContainsKey("message"))
                {
                    ViewBag.Message = TempData["message"];
                    ViewBag.Alert = TempData["messageType"];
                }
                return View(p);
            }

        }

        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        public IActionResult Show([FromForm] Comment comment)
        {
            comment.Date = DateTime.Now;
            comment.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Comments.Add(comment);
                db.SaveChanges();
                TempData["message"] = "Comentariul a fost adăugat.";
                TempData["messageType"] = "alert-success";
                return Redirect("/Posts/Show/" + comment.PostId);
            }

            else
            {
                Post art = db.Posts.Include("Group")
                            .Include("User")
                            .Include("Comments")
                            .Include("Comments.User")
                            .Where(art => art.Id == comment.PostId)
                             .First();

                SetAccessRights();
                return View(art);
            }
        }

        [Authorize(Roles = "User,Admin")]
        public IActionResult New()
        {
            Post post = new Post();
            post.Groups = GetAllGroups();
            return PartialView("_NewPostFormPartial");
        }

        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        public IActionResult New(Post post)
        {
            //var sanitizer = new HtmlSanitizer();
            post.Date = DateTime.Now;
            post.UserId = _userManager.GetUserId(User);
            if(post.Content==null)
            {
                TempData["message"] = "Conținutul postării nu poate fi gol!";
                TempData["messageType"] = "alert-danger";
                return Redirect("/Posts/Index");
            }

            if (ModelState.IsValid && post.Content!=null)
            { 
                //post.Content = sanitizer.Sanitize(post.Content);
                db.Posts.Add(post);
                db.SaveChanges();
                TempData["message"] = "Postarea s-a realizat cu succes.";
                TempData["messageType"] = "alert-success";

                return RedirectToAction("Index");
            }

            else
            {
                post.Groups = GetAllGroups();
                return PartialView("_NewPostFormPartial",post);
            }
        }

        [Authorize(Roles = "User,Admin")]
        public IActionResult Edit(int id)
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }
            Post post = db.Posts.Include("Group")
                                         .Where(art => art.Id == id)
                                         .First();
            post.Groups = GetAllGroups();

            if (post.UserId == _userManager.GetUserId(User))
            {
                return View(post);
            }
            else
            {
                TempData["message"] = "Nu aveți dreptul să faceți modificări asupra unei postări care nu vă aparține.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Edit(int id, Post requestPost)
        {
            //var sanitizer = new HtmlSanitizer();
            if(TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }
            //add model state for the content: it should not be empty
            if (string.IsNullOrEmpty(requestPost.Content))
            {
                ModelState.AddModelError("Content", "Conținutul postării nu poate fi gol!");
            }
            Post post = db.Posts.Find(id);
            if (ModelState.IsValid && post.Content!=requestPost.Content)
            {
                if (post.UserId == _userManager.GetUserId(User))
                {
                    //post.Content = sanitizer.Sanitize(requestPost.Content);
                    post.Content = requestPost.Content;
                    TempData["message"] = "Postarea a fost modificată.";
                    TempData["messageType"] = "alert-success";
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["message"] = "Nu aveți dreptul să faceți modificări asupra unei postări care nu vă aparține.";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index");
                }
            }
            else if(post.Content == requestPost.Content)
            {
                TempData["message"] = "Nu ați făcut nicio modificare.";
                TempData["messageType"] = "alert-warning";
                return RedirectToAction("Index");
            }
            else
            {
                requestPost.Groups = GetAllGroups();
                return View(requestPost);
            }
            
        }

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public ActionResult Delete(int id)
        {

            Post post = db.Posts.Include("Comments")
                                         .Where(p => p.Id == id)
                                         .First();
            //Implementam stergerea in cascada, intai toate comentariile, apoi postarea
            if (post.Comments.Count > 0)
            {
                foreach (Comment comm in post.Comments)
                {
                    db.Comments.Remove(comm);
                }
            }

            if (post.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                if(post.GroupId != null)
                {
                    Group group = db.Groups.Find(post.GroupId);
                    db.Posts.Remove(post);
                    db.SaveChanges();
                    TempData["message"] = "Postarea a fost ștearsă.";
                    TempData["messageType"] = "alert-success";
                    return Redirect("/Groups/Show/"+group.Id);
                }
                else
                {
                    db.Posts.Remove(post);
                    db.SaveChanges();
                    TempData["message"] = "Postarea a fost ștearsă.";
                    TempData["messageType"] = "alert-success";
                    return RedirectToAction("Index");
                }
                
            }

            else
            {
                if(post.GroupId != null)
                {
                    Group group = db.Groups.Find(post.GroupId);
                    TempData["message"] = "Nu aveți dreptul să faceți modificări asupra unei postări care nu vă aparține.";
                    TempData["messageType"] = "alert-danger";
                    return Redirect("/Groups/Show/" + group.Id);
                }
                else
                {
                    TempData["message"] = "Nu aveți dreptul să faceți modificări asupra unei postări care nu vă aparține.";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index");
                }
            }
        }

        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            ViewBag.EsteAdmin = User.IsInRole("Admin");

            ViewBag.UserCurent = _userManager.GetUserId(User);
        }


        [NonAction]
        public IEnumerable<SelectListItem> GetAllGroups()
        {
            var selectList = new List<SelectListItem>();
            var groups = from grp in db.Groups select grp;

            foreach (var group in groups)
            {
                selectList.Add(new SelectListItem
                {
                    Value = group.Id.ToString(),
                    Text = group.GroupName.ToString()
                });
            }
            return selectList;
        }

    }
}
