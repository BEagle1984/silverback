using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SilverbackShop.Common.Infrastructure
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