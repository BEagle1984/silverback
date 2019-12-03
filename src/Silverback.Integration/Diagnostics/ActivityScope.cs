using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Silverback.Diagnostics
{
    internal class ActivityScope
    {
        public static void ExecuteSend(string activityName, Action<Activity> populate, Action execute)
        {
            var activity = new Activity(activityName);
            try
            {
                activity.Start();
                populate(activity);
                execute();
            }
            finally
            {
                activity.Stop();
            }
        }

        public static async Task ExecuteSendAsync(string activityName, Action<Activity> populate, Func<Task> execute)
        {
            var activity = new Activity(activityName);
            try
            {
                activity.Start();
                populate(activity);
                await execute();
            }
            finally
            {
                activity.Stop();
            }
        }


        public static async Task ExecuteReceiveAsync(string activityName, Action<Activity> populate, Func<Task> execute)
        {
            var activity = new Activity(activityName);
            try
            {
                populate(activity);
                activity.Start();
                await execute();
            }
            finally
            {
                activity.Stop();
            }
        }
    }
}
