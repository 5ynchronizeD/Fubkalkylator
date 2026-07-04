using System.Text.Json;
using Fubkalkylator.Core;

namespace Fubkalkylator.App;

/// <summary>
/// Loggbokslagring som JSON-fil i appens privata datamapp. Beständig över
/// omstarter; tas bort först när appen avinstalleras. Bakom <see cref="ISawJobStore"/>
/// så den enkelt kan bytas mot SQLite senare.
/// </summary>
public sealed class JsonFileSawJobStore : ISawJobStore
{
    private readonly string _path = Path.Combine(FileSystem.AppDataDirectory, "loggbok.json");
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private List<SawJob>? _cache;

    private async Task<List<SawJob>> LoadAsync()
    {
        if (_cache is not null) return _cache;
        if (File.Exists(_path))
        {
            await using var s = File.OpenRead(_path);
            _cache = await JsonSerializer.DeserializeAsync<List<SawJob>>(s) ?? new();
        }
        else
        {
            _cache = new();
        }
        return _cache;
    }

    private async Task PersistAsync()
    {
        await using var s = File.Create(_path);
        await JsonSerializer.SerializeAsync(s, _cache!, Json);
    }

    public async Task<IReadOnlyList<SawJob>> GetAllAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var list = await LoadAsync();
            return list.OrderByDescending(j => j.SavedAt).ToList();
        }
        finally { _gate.Release(); }
    }

    public async Task<SawJob> SaveAsync(SawJob job)
    {
        await _gate.WaitAsync();
        try
        {
            var list = await LoadAsync();
            if (job.Id == 0)
            {
                job.Id = list.Count == 0 ? 1 : list.Max(j => j.Id) + 1;
                list.Add(job);
            }
            else
            {
                int i = list.FindIndex(j => j.Id == job.Id);
                if (i >= 0) list[i] = job; else list.Add(job);
            }
            await PersistAsync();
            return job;
        }
        finally { _gate.Release(); }
    }

    public async Task DeleteAsync(int id)
    {
        await _gate.WaitAsync();
        try
        {
            var list = await LoadAsync();
            list.RemoveAll(j => j.Id == id);
            await PersistAsync();
        }
        finally { _gate.Release(); }
    }
}
