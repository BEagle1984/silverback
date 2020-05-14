// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Domain.Util
{
    internal static class EntityActivator
    {
        public static TEntity CreateInstance<TEntity>(IEnumerable<IEntityEvent> events, object eventStoreEntity)
        {
            try
            {
                var entity = (TEntity) Activator.CreateInstance(typeof(TEntity), events);

                PropertiesMapper.Map(eventStoreEntity, entity);

                return entity;
            }
            catch (MissingMethodException ex)
            {
                throw new EventSourcingException(
                    $"The type {typeof(TEntity).Name} doesn't have a public constructor " +
                    "with a single parameter of type IEnumerable<IEntityEvent>.", ex);
            }
        }
    }
}