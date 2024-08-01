// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Integration.E2E.TestTypes.Messages;

public partial class ProtobufMessage
{
    public const string Schema =
        """
        syntax = "proto3";

        package Silverback.Tests.Integration.E2E.TestTypes.Messages;

        message ProtobufMessage {
          string number = 1;
        }
        """;
}
