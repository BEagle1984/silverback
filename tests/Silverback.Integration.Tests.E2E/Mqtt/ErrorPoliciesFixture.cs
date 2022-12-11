// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Tests.Integration.E2E.TestHost;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ErrorPoliciesFixture : MqttFixture
{
    private static readonly byte[] AesEncryptionKey = BytesUtil.GetRandomBytes(32);

    public ErrorPoliciesFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
        // TODO: Test rollback always called with all kind of policies
    }
}
