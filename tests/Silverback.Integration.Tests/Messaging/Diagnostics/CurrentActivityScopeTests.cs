// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Diagnostics;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics;

public class CurrentActivityScopeTests
{
    [Fact]
    public async Task Enter_ShouldSetActivityAsCurrentUntilDisposed()
    {
        using Activity callerActivity = new Activity("Caller").Start();
        using Activity activity = await StartActivityInOtherFlowAsync();

        using (CurrentActivityScope.Enter(activity))
        {
            Activity.Current.ShouldBe(activity);
        }

        Activity.Current.ShouldBe(callerActivity);
    }

    [Fact]
    public void Enter_ShouldDoNothing_WhenActivityIsNull()
    {
        using Activity callerActivity = new Activity("Caller").Start();

        using (CurrentActivityScope.Enter(null))
        {
            Activity.Current.ShouldBe(callerActivity);
        }

        Activity.Current.ShouldBe(callerActivity);
    }

    [Fact]
    public async Task Dispose_ShouldClearCurrentActivity_WhenPreviousActivityIsStopped()
    {
        // The current activity of a thread whose execution context captured an activity that was stopped in another flow
        Activity staleActivity = new Activity("Stale").Start();
        await Task.Run(staleActivity.Stop);
        Activity.Current.ShouldBe(staleActivity);

        using Activity activity = await StartActivityInOtherFlowAsync();

        using (CurrentActivityScope.Enter(activity))
        {
            Activity.Current.ShouldBe(activity);
        }

        Activity.Current.ShouldBeNull();
    }

    // Starts the activity without making it the current one in this flow
    private static Task<Activity> StartActivityInOtherFlowAsync() => Task.Run(() => new Activity("Produce").Start());
}
