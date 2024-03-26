using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using Threads.Data;
using Threads.Models;

namespace Threads.Controllers
{

    [Authorize]
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext db;
        private IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public GroupsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IWebHostEnvironment env)
        {
            this.db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _env = env;
        }


        public IActionResult Index()
        {
            SetAccessRights();

            var groups = db.Groups
                           .Include("Posts")
                           .Include("User")
                           .Include("GroupUsers")
                           .Include("Posts.User");

            if (TempData.ContainsKey("message"))
            {
                ViewBag.message = TempData["message"].ToString();
                ViewBag.Alert = TempData["messageType"].ToString();
            }

            if(groups.Count()==0)
            {
                ViewBag.info = "Momentan nu a fost creat niciun grup.";
            }
            ViewBag.Groups = groups;
            return View();
        }


        [Authorize(Roles = "User,Admin")]
        public IActionResult Show(int id)
        {
            if(TempData.ContainsKey("message"))
            {
                ViewBag.message = TempData["message"].ToString();
                ViewBag.Alert = TempData["messageType"].ToString();
            }
            SetAccessRights();

            var group = db.Groups
                           .Include("Posts")
                           .Include("User")
                           .Include("GroupUsers")
                           .Include("Posts.User")
                           .Where(b => b.Id == id)
                           .FirstOrDefault();

            //put the posts from the group in reversed chronological order

           

            if (group == null)
            {
                return NotFound(); // Handle case when the group is not found
            }

            group.Posts = group.Posts.OrderByDescending(p => p.Date).ToList();
            bool ok; //Permitem adminului aplicatiei sa acceseze orice grup
            if (User.IsInRole("Admin"))
            {
                 ok = true;
            }
            else
            {
                 ok = group.GroupUsers.Any(gu => gu.UserId.ToString() == _userManager.GetUserId(User));
            }

            // Depending on what you want to do with 'ok', you might want to pass it to the view or use it for some logic here
            if (ok) return View(group);
            else
            {
                TempData["message"] = "Nu aveti dreptul sa vedeti acest grup";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }
        }


        [Authorize(Roles = "User,Admin")]
        public IActionResult New()
        {
            return View();
        }

        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        public async Task<IActionResult> New(Group grp, IFormFile GroupImage)
        {
            ModelState.Remove("GroupImage");
            if (ModelState.IsValid == true)
            {   if(GroupImage!=null&&GroupImage.Length > 0)
                {
                    var storagePath = Path.Combine(_env.WebRootPath, "images", GroupImage.FileName);
                    var databaseFileName = "/images/" + GroupImage.FileName;
                    using (var fileStream = new FileStream(storagePath, FileMode.Create))
                    {
                        await GroupImage.CopyToAsync(fileStream);
                    }

                    grp.GroupImage = databaseFileName;
                }
                
                grp.UserId = _userManager.GetUserId(User);
                grp.Date = DateTime.Now;
                db.Groups.Add(grp);
                db.SaveChanges();


                GroupUser groupUser = new GroupUser();
                groupUser.UserId = _userManager.GetUserId(User);
                groupUser.GroupId = grp.Id;
                db.GroupUsers.Add(groupUser);
                
                db.SaveChanges();
                TempData["message"] = "Grupul a fost creat!";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else return View(grp);
        }


        [Authorize(Roles = "User,Admin")]
        public IActionResult Edit(int id)
        {
            Group grp = db.Groups.Where( art =>  art.Id == id ).FirstOrDefault();

            if (grp.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                return View(grp);
            }
            else
            {
                TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui grup care nu va apartine";
                TempData["messageType"] = "alert-danger";

                return RedirectToAction("Index");
            }
        }


        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Group grp, IFormFile GroupImage)
        {
            Group group = db.Groups.Find(id);

            if (group == null)
            {
                return NotFound(); // Handle the case where the group with the given ID is not found
            }

            // Check if the user has permission to edit the group
            if (group.UserId != _userManager.GetUserId(User))
            {
                TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unei postari care nu va apartine";
                TempData["messageType"] = "alert-danger";

                return RedirectToAction("Index");
            }
            ModelState.Remove("GroupImage");

            if (ModelState.IsValid)
            {
                if (GroupImage != null && GroupImage.Length > 0)
                {
                    var storagePath = Path.Combine(_env.WebRootPath, "images", GroupImage.FileName);
                    var databaseFileName = "/images/" + GroupImage.FileName;
                    using (var fileStream = new FileStream(storagePath, FileMode.Create))
                    {
                        await GroupImage.CopyToAsync(fileStream);
                    }

                    group.GroupImage = databaseFileName;
                }

                // Update other group properties as needed
                group.GroupName = grp.GroupName;
                group.Description = grp.Description;

                db.SaveChanges();

                TempData["message"] = "Grupul a fost modificat!";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }

            ViewBag.Group = group;
            SetAccessRights();
            return View(group);
        }




        /*        
         *        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public ActionResult Edit(int id, Group requestGroup)
        {
            SetAccessRights();
            Group group = db.Groups.Find(id);
            if (group.UserId == _userManager.GetUserId(User))
            {
                if (ModelState.IsValid)
                {
                    
                    group.GroupName = requestGroup.GroupName;
                    group.GroupImage = requestGroup.GroupImage;
                    group.Description = requestGroup.Description;
                    db.SaveChanges();
                    TempData["message"] = "Grupul a fost modificat!";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui grup care nu va apartine";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index");
                }
            }

            else
            {
                return View(requestGroup);
            }
        } */

       

        [HttpPost]
        public ActionResult Delete(int id)
        {
            SetAccessRights();
            Group group = db.Groups.Find(id);

            if (group.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {

                var relatedPosts = db.Posts.Where(p => p.GroupId == group.Id).ToList();

                foreach (var post in relatedPosts)
                {
                    db.Posts.Remove(post);
                }

                db.Groups.Remove(group);

                db.SaveChanges();

                TempData["message"] = "Grupul a fost șters";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");

            }

            else
            {
                TempData["message"] = "Nu aveți dreptul să ștergeți un grup care nu vă aparține";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Posts");
            }
        }

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public ActionResult AddUSer(int id)
        {
            Group group = db.Groups.Find(id);
            
            GroupUser groupUser = new GroupUser();
            groupUser.GroupId = id;
            groupUser.UserId = _userManager.GetUserId(User);
            db.GroupUsers.Add(groupUser);
            db.SaveChanges();
            TempData["message"] = "Ai fost adaugat in grup!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Index", "Groups");
        }


        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public ActionResult RemoveUser(int id)
        {
            Group group = db.Groups.Find(id);
            string userId = _userManager.GetUserId(User);

            // Check if the user is a member of the group before removing them
            GroupUser groupUser = db.GroupUsers.FirstOrDefault(gu => gu.GroupId == id && gu.UserId == userId);

            if (groupUser != null)
            {
                db.GroupUsers.Remove(groupUser);
                db.SaveChanges();
                TempData["message"] = "Ai fost eliminat din grup!";
                TempData["messageType"] = "alert-success";
            }
            else
            {
                TempData["message"] = "Nu faci parte din acest grup!";
                TempData["messageType"] = "alert-danger";
            }

            return RedirectToAction("Index", "Groups");
        }



        private void SetAccessRights()
        {


            ViewBag.EsteAdmin = User.IsInRole("Admin");
            ViewBag.UserCurent = _userManager.GetUserId(User);
        }


        [HttpPost]
        public ActionResult Show([FromForm] Post post)
        {
            post.Date = DateTime.Now;
            post.UserId = _userManager.GetUserId(User);

            if (post.Content == null)
            {
                TempData["message"] = "Conținutul postării nu poate fi gol!";
                TempData["messageType"] = "alert-danger";
                return Redirect("/Groups/Show/"+ post.GroupId);
            }

            if (ModelState.IsValid)
            {
                db.Posts.Add(post);
                db.SaveChanges();
                TempData["message"] = "Postarea a fost adaugată în grup.";
                TempData["messageType"] = "alert-success";
                return Redirect("/Groups/Show/" + post.GroupId);


            }

            else
            {
                var groups = db.Groups
                           .Include("Posts")
                           .Include("User")
                           .Include("GroupUsers")
                           .Include("Posts.User")
                           .Where(b => b.Id == post.GroupId)
                           .FirstOrDefault();

                SetAccessRights();
                return View(groups);
            }
        }


        public ActionResult NewPost(int groupId)
        {
            var group = db.Groups.Find(groupId);

            if (group == null)
            {
                // Handle the case where the group isn't found
                // For example, redirect to an error page or return a not found result
                return View("Index"); // Or an appropriate action
            }

            return View("NewPost", group);

        }


    }
}
