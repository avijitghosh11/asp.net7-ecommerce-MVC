﻿using BulkyStore_DataAccess.Repository.IRepository;
using BulkyStore_Models.Models;
using BulkyStore_Models.ViewModels;
using BulkyStore_Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Stripe;

namespace BulkyStore_UI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            //List<Product> data = _unitOfWork.Product.GetAll(includeProperties:"Category").ToList();
            return View();
        }


        public IActionResult Upsert(int? id)
        {
            if (id == null || id == 0)
            {
                ProductVM data = new();
                data.CategoryList = GetCategoryList(_unitOfWork);
                data.Product = new();
                return View(data);
            }
            else
            {
                if (id == null || id == 0)
                {
                    return NotFound();
                }
                var product = _unitOfWork.Product.Get(c => c.Id == id, includeProperties: "Category");
                if (product == null)
                {
                    return NotFound();
                }

                ProductVM data = new()
                {
                    Product = product,
                    CategoryList = GetCategoryList(_unitOfWork, product.CategoryId)
                };

                data.Product = _unitOfWork.Product.Get(u => u.Id == id, includeProperties: "ProductImages");
                return View(data);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(int? id, ProductVM productVM, List<IFormFile> files)
        {
            productVM.CategoryList = GetCategoryList(_unitOfWork, categoryId: productVM.Product.CategoryId);
            if (ModelState.IsValid)
            {
                
                if (productVM.Product.Id == 0)
                {
                    var isExists = _unitOfWork.Product.Get(x => x.Title.ToLower() == productVM.Product.Title.ToLower());
                    if (isExists != null)
                    {
                        ModelState.AddModelError("Product.Title", "Title must be unique.");
                        return View(productVM);
                    }
                    _unitOfWork.Product.Add(productVM.Product);
                    TempData["success"] = "Product create successfully";                    
                }
                else
                {
                    var isExists = _unitOfWork.Product
                            .Get(x => x.Title.ToLower() == productVM.Product.Title.ToLower() && x.Id != productVM.Product.Id);
                    if (isExists != null)
                    {
                        ModelState.AddModelError("Product.Title", "Title must be unique.");
                        return View(productVM);
                    }
                    _unitOfWork.Product.Update(productVM.Product);                    
                    TempData["success"] = "Product update successfully";

                    
                }

                _unitOfWork.Save();

                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (files != null)
                {

                    foreach (IFormFile file in files)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath = @"images\products\product-" + productVM.Product.Id;
                        string finalPath = Path.Combine(wwwRootPath, productPath);

                        if (!Directory.Exists(finalPath))
                            Directory.CreateDirectory(finalPath);

                        using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }

                        ProductImage productImage = new()
                        {
                            ImageUrl = @"\" + productPath + @"\" + fileName,
                            ProductId = productVM.Product.Id,
                        };

                        if (productVM.Product.ProductImages == null)
                            productVM.Product.ProductImages = new List<ProductImage>();

                        productVM.Product.ProductImages.Add(productImage);

                    }

                    _unitOfWork.Product.Update(productVM.Product);
                    _unitOfWork.Save();
                }
                return RedirectToAction("Index");
            }
            return View(productVM);

        }

        public IActionResult DeleteImage(int imageId)
        {
            var imageToBeDeleted = _unitOfWork.ProductImage.Get(u => u.Id == imageId);
            int productId = imageToBeDeleted.ProductId;
            if (imageToBeDeleted != null)
            {
                if (!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
                {
                    var oldImagePath =
                                   Path.Combine(_webHostEnvironment.WebRootPath,
                                   imageToBeDeleted.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                _unitOfWork.ProductImage.Remove(imageToBeDeleted);
                _unitOfWork.Save();

                TempData["success"] = "Deleted successfully";
            }

            return RedirectToAction(nameof(Upsert), new { id = productId });
        }

        public IActionResult Create()
        {
            ProductVM data = new();
            data.CategoryList = GetCategoryList(_unitOfWork);
            //ViewBag.Categories = categories;
            //ViewData["Categories"] = categories;

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ProductVM productVM)
        {
            productVM.CategoryList = GetCategoryList(_unitOfWork,categoryId:productVM.Product.CategoryId);
            if (ModelState.IsValid)
            {
                var isExists = _unitOfWork.Product.Get(x => x.Title.ToLower() == productVM.Product.Title.ToLower());
                
                if (isExists != null)
                {
                    ModelState.AddModelError("Product.Title", "Title must be unique.");                    
                    return View(productVM);
                }
                _unitOfWork.Product.Add(productVM.Product);
                _unitOfWork.Save();
                TempData["success"] = "Product create successfully";
                return RedirectToAction("Index");
            }
            return View(productVM);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var product = _unitOfWork.Product.Get(c => c.Id == id, includeProperties: "Category");
            if (product == null)
            {
                return NotFound();
            }
            
            ProductVM data = new()
            {
                Product = product,
                CategoryList = GetCategoryList(_unitOfWork,product.CategoryId)
            };

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProductVM productVM)
        {
            productVM.CategoryList = GetCategoryList(_unitOfWork, productVM.Product.CategoryId);
            if (ModelState.IsValid)
            {
                var isExists = _unitOfWork.Product
                    .Get(x => x.Title.ToLower() == productVM.Product.Title.ToLower() && x.Id != productVM.Product.Id);
                if (isExists != null)
                {
                    ModelState.AddModelError("Product.Title", "Title must be unique.");
                    return View(productVM);
                }
                _unitOfWork.Product.Update(productVM.Product);
                _unitOfWork.Save();
                TempData["success"] = "Product update successfully";
                return RedirectToAction("Index");
            }
            return View(productVM);
        }

        //public IActionResult Delete(int? id)
        //{
        //    if (id == null || id == 0)
        //    {
        //        return NotFound();
        //    }
        //    var product = _unitOfWork.Product.Get(c => c.Id == id, includeProperties: "Category");
        //    if (product == null)
        //    {
        //        return NotFound();
        //    }

        //    ProductVM data = new()
        //    {
        //        Product = product,
        //        CategoryList = CategoryList.GetCategoryList(_unitOfWork, product.CategoryId)
        //    };
        //    return View(data);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Delete(int id)
        //{
        //    string wwwRootPath = _webHostEnvironment.WebRootPath;
        //    var obj = _unitOfWork.Product.Get(c => c.Id == id);
        //    if (!string.IsNullOrEmpty(obj.ImageUrl))
        //    {
        //        var oldImagePath = Path.Combine(wwwRootPath, obj.ImageUrl.TrimStart('\\'));
        //        if (System.IO.File.Exists(oldImagePath))
        //        {
        //            System.IO.File.Delete(oldImagePath);
        //        }
        //    }
        //    _unitOfWork.Product.Remove(obj);
        //    _unitOfWork.Save();
        //    TempData["success"] = "Product delete successfully";
        //    return RedirectToAction("Index");
        //}


        #region API CALL
        [HttpGet]
        public IActionResult GetAll()
        {
            List<BulkyStore_Models.Models.Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = objProductList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productToBeDeleted = _unitOfWork.Product.Get(u => u.Id == id);
            if (productToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            string productPath = @"images\products\product-" + id;
            string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);

            if (Directory.Exists(finalPath))
            {
                string[] filePaths = Directory.GetFiles(finalPath);
                foreach (string filePath in filePaths)
                {
                    System.IO.File.Delete(filePath);
                }

                Directory.Delete(finalPath);
            }


            _unitOfWork.Product.Remove(productToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion

        [NonAction]
        public static IEnumerable<SelectListItem> GetCategoryList(IUnitOfWork unitOfWork, int? categoryId = 0)
        {
            IEnumerable<SelectListItem> data = null;
            if (categoryId == 0)
            {
                data = unitOfWork.Category.GetAll().Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                });
            }
            else
            {
                data = unitOfWork.Category.GetAll().Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString(),
                    Selected = x.Id == categoryId ? true : false
                });
            }

            return data;
        }
    }
}
