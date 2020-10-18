// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using System;
// using System.Collections.Generic;
// using System.IO;
//
// namespace Silverback.Diagnostics
// {
//     public class DebugLog
//     {
//         private readonly List<string> _logs = new List<string>();
//
//         public List<string> Logs => _logs;
//
//         public void Write(string message)
//         {
//             lock (_logs)
//             {
//                 _logs.Add($"{DateTime.Now:HH:mm.fff} - {message}");
//             }
//         }
//
//         public void Dump()
//         {
//             if (!Directory.Exists("logs"))
//                 Directory.CreateDirectory("logs");
//
//             File.WriteAllText($"logs/{Guid.NewGuid()}.log", string.Join("\r\n", Logs));
//         }
//     }
// }
