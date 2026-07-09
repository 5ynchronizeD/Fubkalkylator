using Fubkalkylator.Core;
using Supabase.Postgrest;

namespace Fubkalkylator.Supabase;

/// <summary>
/// Loggbokslagring mot Supabase (Postgres). Radnivå-säkerhet (RLS) i databasen ser
/// till att varje konto bara ser sina egna rader — ingen user_id-filtrering i koden.
/// </summary>
public sealed class SupabaseSawJobStore : ISawJobStore
{
    private readonly global::Supabase.Client _client;

    public SupabaseSawJobStore(global::Supabase.Client client) => _client = client;

    public async Task<IReadOnlyList<SawJob>> GetAllAsync()
    {
        var res = await _client.From<SawJobRow>()
            .Order("saved_at", Constants.Ordering.Descending)
            .Get();
        return res.Models.Select(r => r.ToJob()).ToList();
    }

    public async Task<SawJob> SaveAsync(SawJob job)
    {
        var row = SawJobRow.FromJob(job);
        if (job.Id == 0)
        {
            var res = await _client.From<SawJobRow>().Insert(row);
            var saved = res.Models.FirstOrDefault();
            return saved?.ToJob() ?? job;
        }
        else
        {
            var res = await _client.From<SawJobRow>().Update(row);
            var saved = res.Models.FirstOrDefault();
            return saved?.ToJob() ?? job;
        }
    }

    public async Task DeleteAsync(int id)
    {
        await _client.From<SawJobRow>().Where(r => r.Id == id).Delete();
    }
}
