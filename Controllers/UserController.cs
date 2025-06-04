using System.Security.Claims;
using Blogging.Application.Interfaces;
using Blogging.Domain.ViewModel;
using Blogging.Infrastructure.Roles;
using BloggingWeb.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloggingWeb.Controllers
{
    
    public class UserController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private IWebHostEnvironment _webHostEnvironment;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        public UserController(IUnitOfWork unitOfWork,IWebHostEnvironment webHostEnvironment,RoleManager<IdentityRole> roleManager,UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _roleManager = roleManager;
            _userManager = userManager;
            
                
        }
        [Authorize(Roles = SD.Role_User_Admin)]
        public async Task< IActionResult> Index(string searchString)
        {
            var user= await _unitOfWork.User.GetAllAsync();
            if (!string.IsNullOrEmpty(searchString))
            {
                user = user.Where(s => s.FirstName!.ToUpper().Contains(searchString.ToUpper()));
                    
            }
            
            return View(user);
        }
        [Authorize(Roles = SD.Role_User_Admin)]
        public async Task<IActionResult> Details(string id)
        {
            var user=await _unitOfWork.User.GetByIdAsync(id);
            return View(user);
        }
        [Authorize]
        public async Task<IActionResult> Edit(string? Id)
        {
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) != null)
            {
                Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            }
            var user = await _unitOfWork.User.GetByIdAsync(Id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }
        [HttpPost]
        public async Task<IActionResult>Edit(ApplicationUser applicationUser, IFormFile? file)
        {
            if(ModelState.IsValid)
            {
                var user= await _unitOfWork.User.GetByIdAsync(applicationUser.Id);
                if(user!=null)
                {
                    user.FirstName=applicationUser.FirstName;
                    user.LastName=applicationUser.LastName;
                    user.UserName=applicationUser.UserName;
                    user.Country=applicationUser.Country;
                    user.State= applicationUser.State;
                    user.City =applicationUser.City;
                    user.Bio=applicationUser.Bio;
                    if (applicationUser.file != null)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(applicationUser.file.FileName);
                        string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, @"ProfilePictures");
                        if (!string.IsNullOrEmpty(applicationUser.ProfileUrl))
                        {
                            var OldImagePath = Path.Combine(_webHostEnvironment.WebRootPath,applicationUser.ProfileUrl.TrimStart('\\'));
                            if (System.IO.File.Exists(OldImagePath))
                            {
                                System.IO.File.Delete(OldImagePath);
                            }

                        }
                        using (var fileStream = new FileStream(Path.Combine(imagePath, fileName), FileMode.Create))
                        {
                            applicationUser.file.CopyTo(fileStream);
                        }
                        applicationUser.ProfileUrl = @"\ProfilePictures\" + fileName;
                    }
                    user.ProfileUrl = applicationUser.ProfileUrl;
                    await _unitOfWork.User.UpdateAsync(user);
                    TempData["success"] = "Details Updated";
                    return RedirectToAction("index","Home");
                }
            }
            return View(applicationUser);

        }
        [Authorize(Roles = SD.Role_User_Admin)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var like = await _unitOfWork.Likes.GetAllAsync();
            var Like = like.Where(l => l.UserId == id);
            foreach (var likeid in Like)
            {
                await _unitOfWork.Likes.DeleteAsync(likeid);
            }
            var comm = await _unitOfWork.Comment.GetAllAsync();
            var Coment = comm.Where(l => l.UserId == id);
            foreach (var comid in Coment)
            {
                await _unitOfWork.Comment.DeleteAsync(comid);
            }
            var Post= await _unitOfWork.post.GetAllAsync();
            var po=Post.Where(l => l.UserId == id);
            foreach (var poid in po)
            {
                var Pos = await _unitOfWork.PostCategories.GetAllAsync();
                var PostId = Pos.Where(p => p.PostId == poid.Id).ToList();

                foreach (var proId in PostId)
                {
                    await _unitOfWork.PostCategories.DeleteAsync(proId);
                }
                if (!string.IsNullOrEmpty(poid.ImageUrl))
                {
                    var OldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, poid.ImageUrl.TrimStart('\\'));
                    if (System.IO.File.Exists(OldImagePath))
                    {
                        System.IO.File.Delete(OldImagePath);
                    }

                }
                await _unitOfWork.post.DeleteAsync(poid);
            }
            var user = await _unitOfWork.User.GetByIdAsync(id);
            if (user != null)
            {
                if (!string.IsNullOrEmpty(user.ProfileUrl))
                {
                    var OldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfileUrl.TrimStart('\\'));
                    if (System.IO.File.Exists(OldImagePath))
                    {
                        System.IO.File.Delete(OldImagePath);
                    }

                }
                await _unitOfWork.User.DeleteAsync(user);
                TempData["success"]="User Removed";
            }
            return RedirectToAction("Index");
        }
        [Authorize(Roles = SD.Role_User_Admin)]
        public async Task<IActionResult> AssignRole(string id)
        {
            var user = await _unitOfWork.User.GetByIdAsync(id);
            var roles = await _roleManager.Roles.ToListAsync();
            var viewModel = new RolesVM
            {
                Users = user,
                Roles = roles
            };
            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(RolesVM model)
        {
            if (ModelState.IsValid)
            {
                var user = await _unitOfWork.User.GetByIdAsync(model.Users.Id);
                var role = await _roleManager.FindByNameAsync(model.RoleName);
                if (user != null && role != null)
                {
                    
                    var result = await _userManager.AddToRoleAsync(user, role.Name);
                    if (result.Succeeded)
                    {
                        TempData["success"] = "Role Assigned";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Role Addition failed");
                    }
                }
            }
            var users = await _unitOfWork.User.GetByIdAsync(model.Users.Id);
            var roles = await _roleManager.Roles.ToListAsync();
            model.Roles = roles;
            
            return View(model);
            
        }

    }
}

    

