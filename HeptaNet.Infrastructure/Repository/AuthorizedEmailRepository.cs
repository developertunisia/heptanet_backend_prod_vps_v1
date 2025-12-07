using Microsoft.EntityFrameworkCore;
using HeptaNet.Domain.Models;
using HeptaNet.Domain.Base;
using HeptaNet.Infrastructure.Database;

namespace HeptaNet.Infrastructure.Repository;

public class AuthorizedEmailRepository : Repository<AuthorizedEmail>, IAuthorizedEmailRepository
{
    public AuthorizedEmailRepository(MyAppContext context) : base(context)
    {
    }

    public async Task<AuthorizedEmail?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(w => w.Email == email);
    }

    public async Task<IEnumerable<AuthorizedEmail>> GetImportedAsync(bool isImported)
    {
        return await _dbSet.Where(w => w.IsImported == isImported).ToListAsync();
    }

    public async Task<IEnumerable<AuthorizedEmail>> SearchAsync(string? email, bool? isImported)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(email))
        {
            query = query.Where(w => w.Email.Contains(email));
        }

        if (isImported.HasValue)
        {
            query = query.Where(w => w.IsImported == isImported.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<bool> UpdateImportStatusAsync(string email, bool isImported)
    {
        var emailRecord = await GetByEmailAsync(email);
        if (emailRecord == null)
            return false;

        emailRecord.IsImported = isImported;
        if (isImported)
        {
            emailRecord.SyncDate = DateTime.UtcNow;
        }

        _dbSet.Update(emailRecord); // Remove 'await' here, Update is synchronous
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteByEmailAsync(string email)
    {
        var emailRecord = await GetByEmailAsync(email);
        if (emailRecord == null)
            return false;

        _dbSet.Remove(emailRecord);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<AuthorizedEmail> CreateAuthorizedEmailAsync(AuthorizedEmail authorizedEmail)
    {
        // Vérifier si l'email existe déjà
        var existing = await GetByEmailAsync(authorizedEmail.Email);
        if (existing != null)
        {
            throw new InvalidOperationException($"L'email {authorizedEmail.Email} existe déjà");
        }

        authorizedEmail.SyncDate = DateTime.UtcNow;
        return await CreateAsync(authorizedEmail);
    }
}