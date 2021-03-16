using System;
using UnityEngine.Events;

namespace Mirage.Events
{
    public abstract class RunOnceEventBase
    {
        protected abstract UnityEventBase baseEvent { get; }
        protected bool hasInvoked { get; private set; }

        protected void MarkInvoked()
        {
            if (hasInvoked) throw new InvalidOperationException("Event can only be invoked once Invoke");

            hasInvoked = true;
        }

        /// <summary>
        /// Resets event, removing all listens and allowing it to be invoked again
        /// </summary>
        public void Reset()
        {
            hasInvoked = false;
            baseEvent.RemoveAllListeners();
        }
    }
}
