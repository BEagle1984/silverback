using System.Threading;
using System.Threading.Tasks;

namespace Common.Domain
{
    /// <summary>
    /// The unit of work.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Asynchronously saves all pending changes and published all pending events
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken());
    }
}