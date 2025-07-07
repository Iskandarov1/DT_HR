using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Domain.Core.Primitives.Maybe;
using DT_HR.Domain.Entities;
using DT_HR.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DT_HR.Persistence.Repositories;

internal sealed class CompanyRepository(IDbContext dbContext) : GenericRepository<Company>(dbContext), ICompanyRepository
{
    public async Task<Maybe<Company>> GetAsync(CancellationToken cancellationToken) =>
        await DbContext.Set<Company>().FirstOrDefaultAsync(cancellationToken);
}