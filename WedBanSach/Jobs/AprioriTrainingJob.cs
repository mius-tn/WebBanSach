using Hangfire;
using WedBanSach.Services.Apriori;
using WedBanSach.Repositories;

namespace WedBanSach.Jobs;

public class AprioriTrainingJob
{
    private readonly IAprioriService _aprioriService;
    private readonly IAprioriRepository _repository;

    public AprioriTrainingJob(IAprioriService aprioriService, IAprioriRepository repository)
    {
        _aprioriService = aprioriService;
        _repository = repository;
    }

    public async Task ExecuteAsync()
    {
        var config = await _repository.GetConfigAsync();
        if (config == null || !config.AutoRetrain)
        {
            return;
        }

        var lastTraining = await _repository.GetLatestTrainingHistoryAsync();
        
        // If never trained or trained longer ago than TrainingIntervalHours
        if (lastTraining == null || 
            lastTraining.EndTime.HasValue && (DateTime.Now - lastTraining.EndTime.Value).TotalHours >= config.TrainingIntervalHours)
        {
            await _aprioriService.TrainModelAsync();
        }
    }
}
