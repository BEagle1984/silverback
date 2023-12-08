﻿// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Silverback.Storage.PostgreSql")]
[assembly: InternalsVisibleTo("Silverback.Storage.Sqlite")]

[assembly: InternalsVisibleTo("Silverback.Storage.PostgreSql.Tests")]
[assembly: InternalsVisibleTo("Silverback.Storage.Sqlite.Tests")]