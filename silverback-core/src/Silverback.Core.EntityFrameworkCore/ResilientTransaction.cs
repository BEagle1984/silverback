﻿// TODO: DELETE?

//using System;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;

//namespace Silverback.Core.EntityFrameworkCore
//{
//    // TODO: Needed?
//    public class ResilientTransaction
//    {
//        private DbContext _context;

//        private ResilientTransaction(DbContext context) =>
//            _context = context ?? throw new ArgumentNullException(nameof(context));

//        public static ResilientTransaction New(DbContext context) =>
//            new ResilientTransaction(context);

//        public async Task ExecuteAsync(Func<Task> action)
//        {
//            // Use of an EF Core resiliency strategy when using multiple DbContexts within an explicit BeginTransaction():
//            // See: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency
//            var strategy = _context.Database.CreateExecutionStrategy();
//            await strategy.ExecuteAsync(async () =>
//            {
//                using (var transaction = _context.Database.BeginTransaction())
//                {
//                    await action();
//                    transaction.Commit();
//                }
//            });
//        }

//        public void Execute(Action action)
//        {
//            // Use of an EF Core resiliency strategy when using multiple DbContexts within an explicit BeginTransaction():
//            // See: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency
//            var strategy = _context.Database.CreateExecutionStrategy();
//            strategy.Execute(() =>
//            {
//                using (var transaction = _context.Database.BeginTransaction())
//                {
//                    action();
//                    transaction.Commit();
//                }
//            });
//        }
//    }
//}
