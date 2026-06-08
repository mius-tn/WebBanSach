using WedBanSach.Models;

namespace WedBanSach.Repositories;

public interface IPolicyRepository
{
    Task<IEnumerable<PolicyCategory>> GetAllCategoriesAsync();
    Task<PolicyCategory?> GetCategoryByIdAsync(int id);
    Task<PolicyCategory?> GetCategoryBySlugAsync(string slug);
    Task AddCategoryAsync(PolicyCategory category);
    Task UpdateCategoryAsync(PolicyCategory category);
    Task DeleteCategoryAsync(int id);

    Task<IEnumerable<Policy>> GetAllPoliciesAsync(bool onlyPublished = false);
    Task<Policy?> GetPolicyByIdAsync(int id);
    Task<Policy?> GetPolicyByCategoryIdAsync(int categoryId);
    Task AddPolicyAsync(Policy policy);
    Task UpdatePolicyAsync(Policy policy);
    Task DeletePolicyAsync(int id);
    Task<bool> SaveChangesAsync();
}
