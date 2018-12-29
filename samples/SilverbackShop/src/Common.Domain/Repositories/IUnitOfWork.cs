using System.Threading;
using System.Threading.Tasks;

namespace Common.Domain.Repositories
{
    public interface IUnitOfWork
    {
        Task SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken());
    }
}