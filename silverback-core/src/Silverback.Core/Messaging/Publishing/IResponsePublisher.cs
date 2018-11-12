using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public interface IResponsePublisher<in TResponse>
        where TResponse : IResponse
    {
        void Reply(TResponse responseMessage);

        Task ReplyAsync(TResponse responseMessage);
    }
}
