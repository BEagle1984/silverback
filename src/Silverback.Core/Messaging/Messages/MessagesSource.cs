// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Messages
{
    public abstract class MessagesSource : MessagesSource<object>
    {
    }

    public abstract class MessagesSource<TBaseEvent> : IMessagesSource
    {
        private List<TBaseEvent> _events;

        #region IMessagesSource

        public IEnumerable<object> GetMessages() => _events?.Cast<object>();

        public void ClearMessages() => _events?.Clear();

        #endregion

        protected virtual void AddEvent(TBaseEvent @event)
        {
            _events = _events ?? new List<TBaseEvent>();

            if (@event is IMessageWithSource messageWithSource)
                messageWithSource.Source = this;

            _events.Add(@event);
        }

        /// <summary>
        /// Adds an event of the specified type to this entity. The event will be fired when the entity
        /// is saved to the underlying database.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="allowMultiple">if set to <c>false</c> only one instance of the specified type <c>TEvent</c> will be added.</param>
        /// <returns></returns>
        protected TEvent AddEvent<TEvent>(bool allowMultiple = true)
            where TEvent : TBaseEvent, new()
        {
            if (!allowMultiple && _events != null && _events.OfType<TEvent>().Any())
                return _events.OfType<TEvent>().First();

            var @event = new TEvent();
            AddEvent(@event);
            return @event;
        }

        protected void RemoveEvent(TBaseEvent @event) => _events?.Remove(@event);
    }
}