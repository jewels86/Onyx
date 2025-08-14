using Microsoft.EntityFrameworkCore;
using ProjectDashboard.Web.Data;
using ProjectDashboard.Web.Models;

namespace ProjectDashboard.Web.Services;

public class SensitiveDataService
{
    private readonly ApplicationDbContext _db;
    private readonly EncryptionService _enc;
    public SensitiveDataService(ApplicationDbContext db, EncryptionService enc)
    { _db = db; _enc = enc; }

    public async Task<int> StoreAsync(string ownerUserId, string kind, string name, string value, int? projectId = null, string? metadata = null)
    {
        var (cipher, iv) = _enc.Encrypt(value);
        var item = new SensitiveItem
        {
            OwnerUserId = ownerUserId,
            Kind = kind,
            Name = name,
            EncryptedValueBase64 = cipher,
            IvBase64 = iv,
            ProjectId = projectId,
            Metadata = metadata
        };
        _db.SensitiveItems.Add(item);
        await _db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<string?> RetrieveAsync(int id, string requesterUserId)
    {
        var i = await _db.SensitiveItems.FirstOrDefaultAsync(x => x.Id == id);
        if (i is null || i.OwnerUserId != requesterUserId) return null;
        return _enc.Decrypt(i.EncryptedValueBase64, i.IvBase64);
    }

    public async Task<List<SensitiveItem>> ListAsync(string ownerUserId, int? projectId = null)
    {
        var q = _db.SensitiveItems.Where(x => x.OwnerUserId == ownerUserId);
        if (projectId.HasValue) q = q.Where(x => x.ProjectId == projectId.Value);
        return await q.OrderByDescending(x => x.CreatedAt).ToListAsync();
    }
}
