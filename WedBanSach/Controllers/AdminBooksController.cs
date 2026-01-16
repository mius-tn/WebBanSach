using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
public class AdminBooksController : Controller
{
    private readonly BookStoreDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public AdminBooksController(BookStoreDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<IActionResult> Index(string searchTerm = "", int page = 1, int pageSize = 20)
    {
        var query = _context.Books
            .Include(b => b.Publisher)
            .Include(b => b.BookAuthors)
            .ThenInclude(ba => ba.Author)
            .Include(b => b.BookCategories)
            .ThenInclude(bc => bc.Category)
            .Include(b => b.BookImages)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(b => b.Title.Contains(searchTerm) || 
                                   (b.ISBN != null && b.ISBN.Contains(searchTerm)) ||
                                   (b.Publisher != null && b.Publisher.PublisherName.Contains(searchTerm)));
        }

        var totalRecords = await query.CountAsync();
        var books = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.SearchTerm = searchTerm;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        ViewBag.PageSize = pageSize;

        return View(books);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadViewDataForBookForm();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Book book, List<int> authorIds, List<int> categoryIds, List<IFormFile> images)
    {
        if (ModelState.IsValid)
        {
            // Check duplicate ISBN
            if (!string.IsNullOrEmpty(book.ISBN) && await _context.Books.AnyAsync(b => b.ISBN == book.ISBN))
            {
                ModelState.AddModelError("ISBN", "Mã ISBN này đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    book.CreatedAt = DateTime.Now;
                    _context.Books.Add(book);
                    await _context.SaveChangesAsync();

                    // Add authors
                    if (authorIds != null && authorIds.Any())
                    {
                        foreach (var authorId in authorIds)
                        {
                            _context.BookAuthors.Add(new BookAuthor
                            {
                                BookID = book.BookID,
                                AuthorID = authorId
                            });
                        }
                    }

                    // Add categories
                    if (categoryIds != null && categoryIds.Any())
                    {
                        foreach (var categoryId in categoryIds)
                        {
                            _context.BookCategories.Add(new BookCategory
                            {
                                BookID = book.BookID,
                                CategoryID = categoryId
                            });
                        }
                    }

                    // Upload images
                    if (images != null && images.Any())
                    {
                        var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "books");
                        if (!Directory.Exists(uploadPath))
                            Directory.CreateDirectory(uploadPath);

                        bool isFirst = true;
                        foreach (var image in images)
                        {
                            if (image.Length > 0)
                            {
                                var fileName = $"{book.BookID}_{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                                var filePath = Path.Combine(uploadPath, fileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await image.CopyToAsync(stream);
                                }

                                _context.BookImages.Add(new BookImage
                                {
                                    BookID = book.BookID,
                                    ImageUrl = $"/uploads/books/{fileName}",
                                    IsMain = isFirst
                                });
                                isFirst = false;
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Thêm sách thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Lỗi lưu dữ liệu: Có thể dữ liệu bị trùng lặp (vd: ISBN). Vui lòng kiểm tra lại.");
                }
            }
        }

        await LoadViewDataForBookForm();
        return View(book);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var book = await _context.Books
            .Include(b => b.BookAuthors)
            .Include(b => b.BookCategories)
            .Include(b => b.BookImages)
            .FirstOrDefaultAsync(b => b.BookID == id);

        if (book == null)
            return NotFound();

        ViewBag.SelectedAuthorIds = book.BookAuthors.Select(ba => ba.AuthorID).ToList();
        ViewBag.SelectedCategoryIds = book.BookCategories.Select(bc => bc.CategoryID).ToList();

        await LoadViewDataForBookForm();
        return View(book);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Book book, List<int> authorIds, List<int> categoryIds, List<IFormFile> images)
    {
        if (id != book.BookID)
            return NotFound();

        if (ModelState.IsValid)
        {
            if (!string.IsNullOrEmpty(book.ISBN) && await _context.Books.AnyAsync(b => b.ISBN == book.ISBN && b.BookID != id))
            {
                ModelState.AddModelError("ISBN", "Mã ISBN này đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update book
                    _context.Update(book);

                    // Update authors
                    var existingAuthors = await _context.BookAuthors
                        .Where(ba => ba.BookID == id)
                        .ToListAsync();
                    _context.BookAuthors.RemoveRange(existingAuthors);

                    if (authorIds != null && authorIds.Any())
                    {
                        foreach (var authorId in authorIds)
                        {
                            _context.BookAuthors.Add(new BookAuthor
                            {
                                BookID = id,
                                AuthorID = authorId
                            });
                        }
                    }

                    // Update categories
                    var existingCategories = await _context.BookCategories
                        .Where(bc => bc.BookID == id)
                        .ToListAsync();
                    _context.BookCategories.RemoveRange(existingCategories);

                    if (categoryIds != null && categoryIds.Any())
                    {
                        foreach (var categoryId in categoryIds)
                        {
                            _context.BookCategories.Add(new BookCategory
                            {
                                BookID = id,
                                CategoryID = categoryId
                            });
                        }
                    }

                    // Add new images
                    if (images != null && images.Any())
                    {
                        var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "books");
                        if (!Directory.Exists(uploadPath))
                            Directory.CreateDirectory(uploadPath);

                        var hasMainImage = await _context.BookImages
                            .AnyAsync(bi => bi.BookID == id && bi.IsMain);

                        foreach (var image in images)
                        {
                            if (image.Length > 0)
                            {
                                var fileName = $"{id}_{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                                var filePath = Path.Combine(uploadPath, fileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await image.CopyToAsync(stream);
                                }

                                _context.BookImages.Add(new BookImage
                                {
                                    BookID = id,
                                    ImageUrl = $"/uploads/books/{fileName}",
                                    IsMain = !hasMainImage
                                });
                                hasMainImage = true;
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật sách thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.BookID))
                        return NotFound();
                    throw;
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Lỗi cập nhật: Dữ liệu bị trùng lặp (vd: ISBN).");
                }
            }
        }

        await LoadViewDataForBookForm();
        return View(book);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book != null)
        {
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa sách thành công!";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteImage(int imageId)
    {
        var image = await _context.BookImages.FindAsync(imageId);
        if (image != null)
        {
            // Delete file
            if (!string.IsNullOrEmpty(image.ImageUrl))
            {
                var filePath = Path.Combine(_environment.WebRootPath, image.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            _context.BookImages.Remove(image);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        return Json(new { success = false });
    }

    [HttpPost]
    public async Task<IActionResult> SetMainImage(int imageId)
    {
        var image = await _context.BookImages.FindAsync(imageId);
        if (image != null)
        {
            // Unset other main images
            var otherMainImages = await _context.BookImages
                .Where(bi => bi.BookID == image.BookID && bi.ImageID != imageId)
                .ToListAsync();
            foreach (var img in otherMainImages)
                img.IsMain = false;

            image.IsMain = true;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        return Json(new { success = false });
    }

    private async Task LoadViewDataForBookForm()
    {
        ViewBag.Publishers = new SelectList(await _context.Publishers.OrderBy(p => p.PublisherName).ToListAsync(), 
            "PublisherID", "PublisherName");
        ViewBag.Authors = await _context.Authors.OrderBy(a => a.AuthorName).ToListAsync();
        ViewBag.Categories = await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync();
    }

    private bool BookExists(int id)
    {
        return _context.Books.Any(e => e.BookID == id);
    }
}
