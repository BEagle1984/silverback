using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers
{
    internal static class SubscribedMethodInvoker
    {
        public static async Task<object> InvokeAndGetResult(SubscribedMethod method, IMessage message, bool executeAsync) =>
            executeAsync
                ? await InvokeAndGetResultAsync(method, message)
                : InvokeAndGetResultSync(method, message);

        private static async Task<object> InvokeAndGetResultAsync(SubscribedMethod method, IMessage message)
        {
            var result = Invoke(method, message);

            return method.MethodInfo.IsAsync()
                ? await GetTaskResult((Task)result)
                : result;
        }

        private static object InvokeAndGetResultSync(SubscribedMethod method, IMessage message) =>
            method.MethodInfo.IsAsync()
                ? AsyncHelper.RunSynchronously<object>(() =>
                {
                    var result = (Task) Invoke(method, message);
                    return GetTaskResult((Task) result);
                })
                : Invoke(method, message);

        private static object Invoke(SubscribedMethod method, IMessage message) =>
            method.MethodInfo.Invoke(method.Instance, new object[] { message });

        private static async Task<object> GetTaskResult(Task resultTask)
        {
            if (resultTask is Task<object> taskWithReturn)
                return await taskWithReturn;

            await resultTask;
            return null;
        }
    }
}