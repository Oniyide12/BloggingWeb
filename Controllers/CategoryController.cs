using System.Threading.Tasks;
using Blogging.Application.Interfaces;
using Blogging.Domain.Entities;
using Blogging.Infrastructure.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BloggingWeb.Controllers
{
    [Authorize(Roles = SD.Role_User_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
                
        }
        public async Task<IActionResult> Index(string? searchString)
        {
            var category= await _unitOfWork.Category.GetAllAsync();
            if (!string.IsNullOrEmpty(searchString))
            {
                category = category.Where(s => s.CategoryName.Contains(searchString, StringComparison.OrdinalIgnoreCase));
            }
            return View(category);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task <IActionResult> Create(Categories categories)
        {
            if(ModelState.IsValid)
            {
               await _unitOfWork.Category.AddAsync(categories);
                TempData["success"] = "New Category Added";
                return RedirectToAction("Index");
            }
            return View();

        }
        public async Task<IActionResult> Edit(int id)
        {
            var cat= await _unitOfWork.Category.GetByIdAsync(id, new QueryOptions<Categories>() { Includes = "PostCategories.Post" });
            
            return View(cat);
        }
        [HttpPost]
        public async Task<IActionResult>Edit(Categories categories)
        {
            if(ModelState.IsValid)
            {
                await _unitOfWork.Category.UpdateAsync(categories);
                TempData["success"] = "Category Name Updated";
                return RedirectToAction("Index");
            }
            return View(categories);
          
        }
        [HttpPost]
        public async Task<IActionResult>Delete(int id)
        {
            var Post = await _unitOfWork.PostCategories.GetAllAsync();
            var PostId= Post.Where(p=>p.CategoryId==id).ToList();
            
            foreach (var proId in PostId)
            {
                await _unitOfWork.PostCategories.DeleteAsync(proId);
            }
            var category = await _unitOfWork.Category.GetByIdAsync(id);
            if (category != null)
            {

                await _unitOfWork.Category.DeleteAsync(category);
                TempData["success"] = "Category Removed";
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index");

        }
    }
}
