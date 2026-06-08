using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Repositories;

public class PolicyRepository : IPolicyRepository
{
    private readonly BookStoreDbContext _context;

    public PolicyRepository(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PolicyCategory>> GetAllCategoriesAsync()
    {
        return await _context.PolicyCategories
            .Include(c => c.Policies)
            .ToListAsync();
    }

    public async Task<PolicyCategory?> GetCategoryByIdAsync(int id)
    {
        return await _context.PolicyCategories
            .Include(c => c.Policies)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<PolicyCategory?> GetCategoryBySlugAsync(string slug)
    {
        return await _context.PolicyCategories
            .Include(c => c.Policies)
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive);
    }

    public async Task AddCategoryAsync(PolicyCategory category)
    {
        await _context.PolicyCategories.AddAsync(category);
    }

    public async Task UpdateCategoryAsync(PolicyCategory category)
    {
        _context.PolicyCategories.Update(category);
        await Task.CompletedTask;
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _context.PolicyCategories.FindAsync(id);
        if (category != null)
        {
            _context.PolicyCategories.Remove(category);
        }
    }

    public async Task<IEnumerable<Policy>> GetAllPoliciesAsync(bool onlyPublished = false)
    {
        var query = _context.Policies.Include(p => p.Category).AsQueryable();
        if (onlyPublished)
        {
            query = query.Where(p => p.IsPublished && p.Category.IsActive);
        }
        return await query.ToListAsync();
    }

    public async Task<Policy?> GetPolicyByIdAsync(int id)
    {
        return await _context.Policies
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Policy?> GetPolicyByCategoryIdAsync(int categoryId)
    {
        return await _context.Policies
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.CategoryId == categoryId);
    }

    public async Task AddPolicyAsync(Policy policy)
    {
        await _context.Policies.AddAsync(policy);
    }

    public async Task UpdatePolicyAsync(Policy policy)
    {
        policy.UpdatedAt = DateTime.Now;
        _context.Policies.Update(policy);
        await Task.CompletedTask;
    }

    public async Task DeletePolicyAsync(int id)
    {
        var policy = await _context.Policies.FindAsync(id);
        if (policy != null)
        {
            _context.Policies.Remove(policy);
        }
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
