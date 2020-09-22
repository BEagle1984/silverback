// TODO: DELETE

// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using System.Collections.Generic;
// using System.Diagnostics.CodeAnalysis;
// using System.Threading.Tasks;
// using Silverback.Messaging.Messages;
// using Silverback.Messaging.Subscribers;
// using Silverback.Util;
//
// namespace Silverback.Messaging.Inbound.Transaction
// {
//     /// <summary>
//     ///     Manages the consumer transaction propagating the commit or rollback to all enlisted services.
//     /// </summary>
//     public class ConsumerTransactionManager
//     {
//         private readonly List<ITransactional> _transactionalServices = new List<ITransactional>();
//
//         /// <summary>
//         ///     Adds the specified service to the transaction participants to be called upon commit or rollback.
//         /// </summary>
//         /// <param name="transactionalService">
//         ///     The service to be enlisted.
//         /// </param>
//         public void Enlist(ITransactional transactionalService)
//         {
//             Check.NotNull(transactionalService, nameof(transactionalService));
//
//             if (_transactionalServices.Contains(transactionalService))
//                 return;
//
//             lock (_transactionalServices)
//             {
//                 if (_transactionalServices.Contains(transactionalService))
//                     return;
//
//                 _transactionalServices.Add(transactionalService);
//             }
//         }
//
//         [Subscribe]
//         [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
//         [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
//         [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
//         private Task OnConsumingCompleted(ConsumingCompletedEvent completedEvent) =>
//             _transactionalServices.ForEachAsync(transactional => transactional.Commit());
//
//         [Subscribe]
//         [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
//         [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
//         [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
//         private Task OnConsumingAborted(ConsumingAbortedEvent abortedEvent) =>
//             _transactionalServices.ForEachAsync(transactional => transactional.Rollback());
//     }
// }
