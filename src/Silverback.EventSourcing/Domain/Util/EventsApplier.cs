// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Reflection;

namespace Silverback.Domain.Util
{
    // TODO: Cache something?
    internal static class EventsApplier
    {
        private const string ApplyMethodPrefix = "apply";

        public static void Apply(IEntityEvent @event, object entity, bool isReplaying = false)
        {
            var methods = entity.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(
                    m =>
                        m.Name.StartsWith(ApplyMethodPrefix, StringComparison.InvariantCultureIgnoreCase) &&
                        m.GetParameters().Any() &&
                        m.GetParameters().First().ParameterType.IsInstanceOfType(@event))
                .ToList();

            if (!methods.Any())
            {
                throw new EventSourcingException(
                    $"No method found to apply event of type {@event.GetType().Name} " +
                    $"in entity {entity.GetType().Name}.");
            }

            methods.ForEach(m => InvokeApplyMethod(m, @event, entity, isReplaying));
        }

        private static void InvokeApplyMethod(
            MethodInfo methodInfo,
            IEntityEvent @event,
            object entity,
            bool isReplaying)
        {
            try
            {
                var parametersCount = methodInfo.GetParameters().Length;

                switch (parametersCount)
                {
                    case 1:
                        methodInfo.Invoke(entity, new object[] { @event });
                        break;
                    case 2:
                        methodInfo.Invoke(entity, new object[] { @event, isReplaying });
                        break;
                    default:
                        throw new ArgumentException("Invalid parameters count.");
                }
            }
            catch (ArgumentException ex)
            {
                throw new EventSourcingException(
                    $"The apply method for the event of type {@event.GetType().Name} " +
                    $"in entity {entity.GetType().Name} has an invalid signature.",
                    ex);
            }
        }
    }
}
