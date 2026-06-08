using WedBanSach.Models;

namespace WedBanSach.Services;

public interface IPolicyService
{
    Task<IEnumerable<PolicyCategory>> GetAllCategoriesAsync();
    Task<PolicyCategory?> GetCategoryByIdAsync(int id);
    Task<PolicyCategory?> GetCategoryBySlugAsync(string slug);
    Task<bool> CreateCategoryAsync(PolicyCategory category);
    Task<bool> UpdateCategoryAsync(PolicyCategory category);
    Task<bool> DeleteCategoryAsync(int id);

    Task<IEnumerable<Policy>> GetAllPoliciesAsync(bool onlyPublished = false);
    Task<Policy?> GetPolicyByIdAsync(int id);
    Task<Policy?> GetPolicyByCategoryIdAsync(int categoryId);
    Task<bool> CreatePolicyAsync(Policy policy);
    Task<bool> UpdatePolicyAsync(Policy policy);
    Task<bool> DeletePolicyAsync(int id);
}
