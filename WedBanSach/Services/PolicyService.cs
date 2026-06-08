using WedBanSach.Models;
using WedBanSach.Repositories;

namespace WedBanSach.Services;

public class PolicyService : IPolicyService
{
    private readonly IPolicyRepository _repository;

    public PolicyService(IPolicyRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<PolicyCategory>> GetAllCategoriesAsync()
    {
        return await _repository.GetAllCategoriesAsync();
    }

    public async Task<PolicyCategory?> GetCategoryByIdAsync(int id)
    {
        return await _repository.GetCategoryByIdAsync(id);
    }

    public async Task<PolicyCategory?> GetCategoryBySlugAsync(string slug)
    {
        return await _repository.GetCategoryBySlugAsync(slug);
    }

    public async Task<bool> CreateCategoryAsync(PolicyCategory category)
    {
        await _repository.AddCategoryAsync(category);
        return await _repository.SaveChangesAsync();
    }

    public async Task<bool> UpdateCategoryAsync(PolicyCategory category)
    {
        await _repository.UpdateCategoryAsync(category);
        return await _repository.SaveChangesAsync();
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        await _repository.DeleteCategoryAsync(id);
        return await _repository.SaveChangesAsync();
    }

    public async Task<IEnumerable<Policy>> GetAllPoliciesAsync(bool onlyPublished = false)
    {
        return await _repository.GetAllPoliciesAsync(onlyPublished);
    }

    public async Task<Policy?> GetPolicyByIdAsync(int id)
    {
        return await _repository.GetPolicyByIdAsync(id);
    }

    public async Task<Policy?> GetPolicyByCategoryIdAsync(int categoryId)
    {
        return await _repository.GetPolicyByCategoryIdAsync(categoryId);
    }

    public async Task<bool> CreatePolicyAsync(Policy policy)
    {
        await _repository.AddPolicyAsync(policy);
        return await _repository.SaveChangesAsync();
    }

    public async Task<bool> UpdatePolicyAsync(Policy policy)
    {
        await _repository.UpdatePolicyAsync(policy);
        return await _repository.SaveChangesAsync();
    }

    public async Task<bool> DeletePolicyAsync(int id)
    {
        await _repository.DeletePolicyAsync(id);
        return await _repository.SaveChangesAsync();
    }
}
