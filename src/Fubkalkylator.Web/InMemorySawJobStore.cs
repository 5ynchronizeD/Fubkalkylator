using Fubkalkylator.Core;

namespace Fubkalkylator.Web;

/// <summary>
/// Loggbokslagring i minnet för webb-appen (ej beständig). Android-appen
/// använder en JSON-filslagring i stället.
/// </summary>
public sealed class InMemorySawJobStore : ISawJobStore
{
    private readonly List<SawJob> _jobs = new();
    private int _next = 1;

    public Task<IReadOnlyList<SawJob>> GetAllAsync()
        => Task.FromResult<IReadOnlyList<SawJob>>(_jobs.OrderByDescending(j => j.SavedAt).ToList());

    public Task<SawJob> SaveAsync(SawJob job)
    {
        if (job.Id == 0) { job.Id = _next++; _jobs.Add(job); }
        else
        {
            int i = _jobs.FindIndex(j => j.Id == job.Id);
            if (i >= 0) _jobs[i] = job; else _jobs.Add(job);
        }
        return Task.FromResult(job);
    }

    public Task DeleteAsync(int id)
    {
        _jobs.RemoveAll(j => j.Id == id);
        return Task.CompletedTask;
    }
}
