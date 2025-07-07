using DT_HR.Domain.Core.Primitives.Maybe;
using DT_HR.Domain.Entities;

namespace DT_HR.Domain.Repositories;

public interface ICompanyRepository
{
    Task<Maybe<Company>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Maybe<Company>> GetAsync(CancellationToken cancellationToken);

    void Insert(Company company);
    void Update(Company company);
}