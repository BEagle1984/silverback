// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Examples.Common
{
    public static class Constants
    {
        public const ConsoleColor PrimaryColor = ConsoleColor.Green;
        public const ConsoleColor AccentColor = ConsoleColor.Green;

        public const ConsoleColor SecondaryColor = ConsoleColor.DarkGray;

        public static readonly byte[] AesEncryptionKey =
        {
            0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e,
            0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c
        };
    }
}