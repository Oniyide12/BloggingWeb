using Blogging.Application.Interfaces;
using Blogging.Domain.ViewModel;
using Blogging.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Blogging.Domain.JoinTables;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using BloggingWeb.Areas.Identity.Data;
using Microsoft.AspNetCore.Authentication;

namespace BloggingWeb.Controllers
{
    [Authorize]
    public class PostController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        public PostController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment,UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }
        public async Task<ActionResult<IEnumerable<PostIndexVM>>> Index(string SearchString)
        {
            var Post= await _unitOfWork.post.GetPostAsync();
            var Po = Post.Where(p => (p.Status).ToString() == "Publish");
            var postCat= await _unitOfWork.PostCategories.GetAllAsync();
            
            var user = await _unitOfWork.User.GetAllAsync();
            
            
            var postIndex = Po.Select(a => new PostIndexVM
            {
                PostId=a.Id,
                Tittle=a.Title,
                Content=a.Content,
                CreatedDate=a.CreatedDate,
                UserId=a.UserId,
                ImageUrl=a.ImageUrl,
                Likecount=a.Likes.Count,
                Commentcount=a.Comment.Count,
                UserName=user.FirstOrDefault(w => w.Id == a.UserId).UserName,
                CategoryName =a.PostCategories?.Select(p=>p.Category?.CategoryName)??Enumerable.Empty<string>()
                
            });
            if (!string.IsNullOrEmpty(SearchString))
            {
                postIndex=postIndex.Where(p=>p.Tittle!.ToUpper().Contains(SearchString.ToUpper()));

            }
          
            
            return View(postIndex);
            
        }
        public async Task<ActionResult<IEnumerable<PostIndexVM>>> Draft()
        {
            var Post = await _unitOfWork.post.GetPostAsync();
            var Po = Post.Where(p => (p.Status).ToString() == "Draft" && p.UserId == _userManager.GetUserId(User));
            var postCat = await _unitOfWork.PostCategories.GetAllAsync();

            var user = await _unitOfWork.User.GetAllAsync();


            var postIndex = Po.Select(a => new PostIndexVM
            {
                PostId = a.Id,
                Tittle = a.Title,
                CreatedDate = a.CreatedDate,
                UserId = a.UserId,
                ImageUrl = a.ImageUrl,
                Likecount = a.Likes.Count,
                Commentcount = a.Comment.Count,
                UserName = user.FirstOrDefault(w => w.Id == a.UserId).UserName,
                CategoryName = a.PostCategories?.Select(p => p.Category?.CategoryName) ?? Enumerable.Empty<string>()

            });
            return View(postIndex);

        }


        [Authorize]
        public async Task<IActionResult> Create()
        {
            var category= await _unitOfWork.Category.GetAllAsync();
            var SelectListItem= category.Select(a=>new SelectListItem
            {
                Value = a.CategoryId.ToString(),
                Text = a.CategoryName.ToString(),

            });
            var postcreatevm = new PostCreateVM
            {
                Post= new Post(),
                Categories= SelectListItem

            };
            
            return View(postcreatevm);
        }
        public string GenerateSlug(string title)
        {
            return title.ToLower().Replace(" ", "_").Replace(",", "");
        }
        [HttpPost]
        public async Task<IActionResult> Create(PostCreateVM model)
        {
            if (model.SelectedCategoryIds==null||model.SelectedCategoryIds.Length==0)
            {
                ModelState.AddModelError("", "Select at least a Category");

            }
            
            if(ModelState.IsValid)
            {
                if (model.Post.file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.Post.file.FileName);
                    string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, @"images\posts");
                    using var fileStream = new FileStream(Path.Combine(imagePath, fileName), FileMode.Create);
                    model.Post.file.CopyTo(fileStream);
                    model.Post.ImageUrl = @"\images\posts\" + fileName;
                }
                else
                {
                    model.Post.ImageUrl = "https://placehold.co/600x400";

                }
                model.Post.CreatedDate = DateTime.Now;
                if(User.FindFirstValue(ClaimTypes.NameIdentifier) != null)
                {
                    model.Post.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                }
               
                model.Post.Slug =GenerateSlug(model.Post.Title);
                await _unitOfWork.post.AddAsync(model.Post);
                foreach (var categoryId in model.SelectedCategoryIds)
                {
                    var postCategories = new PostCategories
                    {
                        PostId = model.Post.Id,
                        CategoryId = int.Parse(categoryId.ToString())
                    };
                    await _unitOfWork.PostCategories.AddAsync(postCategories);
                    TempData["success"] = "Post Successfully Created";
                }

                
                return RedirectToAction("Index");

            }
            var category = await _unitOfWork.Category.GetAllAsync();
            var SelectListItem = category.Select(a => new SelectListItem
            {
                Value = a.CategoryId.ToString(),
                Text = a.CategoryName.ToString(),

            });
            var postcreatevm = new PostCreateVM
            {
                Post = new Post(),
                Categories = SelectListItem

            };

            return View(postcreatevm);

        }
        public async Task<IActionResult>Edit(int id)
        {
            
            var Post= await _unitOfWork.post.GetByIdAsync(id);
            var user = _userManager.GetUserId(User);
            if (Post.UserId != user)
            {
                return Forbid();
            }
            if (Post == null)
            {
                return NotFound();
            }
            var category = await _unitOfWork.Category.GetAllAsync();
            var SelectListItem = category.Select(a => new SelectListItem
            {
                Value = a.CategoryId.ToString(),
                Text = a.CategoryName.ToString(),

            });
            var postcreatevm = new PostCreateVM
            {
                Post =  Post,
                Categories = SelectListItem

            };
           
            

            return View(postcreatevm);

        }
        [HttpPost]
        public async Task<IActionResult> Edit(PostCreateVM postCreateVM, IFormFile? file)
        {
           
            if (ModelState.IsValid)

            {
                if (postCreateVM.SelectedCategoryIds == null || postCreateVM.SelectedCategoryIds.Length == 0)
                {
                    ModelState.AddModelError("SelectedCategoryIds", "Select at least a Category");
                    return RedirectToAction("Edit");

                }
                else
                {

                    var post = await _unitOfWork.post.GetByIdAsync(postCreateVM.Post.Id);
                   if (post != null)
                   {
                   

                        var Pos = await _unitOfWork.PostCategories.GetAllAsync();
                        var PostId = Pos.Where(p => p.PostId == post.Id).ToList();


                        foreach (var proId in PostId)
                        {
                            await _unitOfWork.PostCategories.DeleteAsync(proId);
                        }

                        post.Title = postCreateVM.Post.Title;
                        post.Content = postCreateVM.Post.Content;
                        post.Status = postCreateVM.Post.Status;
                        post.CreatedDate = postCreateVM.Post.CreatedDate;
                        post.Slug = GenerateSlug(postCreateVM.Post.Title);
                        post.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (postCreateVM.Post.file != null)
                        {
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(postCreateVM.Post.file.FileName);
                            string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, @"images\posts");
                            if (!string.IsNullOrEmpty(postCreateVM.Post.ImageUrl))
                            {
                                var OldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, postCreateVM.Post.ImageUrl.TrimStart('\\'));
                                if (System.IO.File.Exists(OldImagePath))
                                {
                                    System.IO.File.Delete(OldImagePath);
                                }

                            }
                            using (var fileStream = new FileStream(Path.Combine(imagePath, fileName), FileMode.Create))
                            {
                                postCreateVM.Post.file.CopyTo(fileStream);
                            }
                            postCreateVM.Post.ImageUrl = @"\images\posts\" + fileName;
                        }
                        post.ImageUrl = postCreateVM.Post.ImageUrl;
                        await _unitOfWork.post.UpdateAsync(post);
                        foreach (var categoryId in postCreateVM.SelectedCategoryIds)
                        {
                            var postCategories = new PostCategories
                            {
                                PostId = postCreateVM.Post.Id,
                                CategoryId = int.Parse(categoryId.ToString())
                            };
                            await _unitOfWork.PostCategories.AddAsync(postCategories);

                        }
                        TempData["success"] = "Post Successfully Updated";

                        if (post.Status.ToString() == "Publish")
                        {
                            return RedirectToAction("index");
                        }
                        else
                        {
                            return RedirectToAction("Draft");
                        }




                   }
                }
            }

            

                return View(postCreateVM);
            
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var like = await _unitOfWork.Likes.GetAllAsync();
            var Like = like.Where(l => l.PostId == id);
            foreach(var likeid  in Like)
            {
                await _unitOfWork.Likes.DeleteAsync(likeid);
            }
            var comm = await _unitOfWork.Comment.GetAllAsync();
            var Coment = comm.Where(l => l.PostId == id);
            foreach (var comid in Coment)
            {
                await _unitOfWork.Comment.DeleteAsync(comid);
            }
            var Pos = await _unitOfWork.PostCategories.GetAllAsync();
            var PostId = Pos.Where(p => p.PostId == id).ToList();

            foreach (var proId in PostId)
            {
                await _unitOfWork.PostCategories.DeleteAsync(proId);
            }
            var Post = await _unitOfWork.post.GetByIdAsync(id);
            if (Post != null)
            {
                if (!string.IsNullOrEmpty(Post.ImageUrl))
                {
                    var OldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, Post.ImageUrl.TrimStart('\\'));
                    if (System.IO.File.Exists(OldImagePath))
                    {
                        System.IO.File.Delete(OldImagePath);
                    }

                }
                await _unitOfWork.post.DeleteAsync(Post);
                TempData["success"] = "Post Removed";
            }
            
            return RedirectToAction("Index");


        }

       
        public async Task<IActionResult> Details(int id)
        {

            var Pot = await _unitOfWork.post.GetPostAsync();
           var po= Pot.Select(b => new Post
            {
              
               Id = b.Id,
                Title=b.Title,
                Content = b.Content,
                CreatedDate = b.CreatedDate,
                UserId = b.UserId,
                ImageUrl = b.ImageUrl,
                Comment=b.Comment
              
                



            });
            var poSt = po.FirstOrDefault(b=>b.Id==id);

            return View(poSt);
        }
        [HttpPost]
        public async Task<IActionResult> Likepost(int id)
        {
            var Post= await _unitOfWork.post.GetByIdAsync(id);
            var userid= User.FindFirstValue(ClaimTypes.NameIdentifier);
            var likes = await _unitOfWork.Likes.GetAsync(i => i.PostId == id && i.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            if (likes == null)
            {
                likes = new Likes
                {
                    PostId = id,
                    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                };
                await _unitOfWork.Likes.AddAsync(likes);
            }
            else
            {
                await _unitOfWork.Likes.DeleteAsync(likes);
            }
            return RedirectToAction("Details");

        }
        [HttpPost]
        public async Task<IActionResult>  AddComment(int postId, string commentText)
        {
            
            var comment = new Comment
            {
                PostId = postId,
                Content = commentText,
                UserId= User.FindFirstValue(ClaimTypes.NameIdentifier)

            };
            await _unitOfWork.Comment.AddAsync(comment);
            return Json(new {commentText=comment.Content});

        }
    }
}
