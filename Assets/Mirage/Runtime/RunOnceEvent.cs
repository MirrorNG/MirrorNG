using System;
using UnityEngine;
using UnityEngine.Events;

namespace Mirage
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
            // todo this will remove inspector events, do we want to do that?
            baseEvent.RemoveAllListeners();
        }
    }

    /// <summary>
    /// Event that can only run once, adding handler late will invoke right away
    /// </summary>
    /// <remarks>
    /// Interface only contains AddHandler method because Invoke should only be called from the owner of the event
    /// </remarks>
    public interface IRunOnceEvent
    {
        void AddListener(UnityAction handler);
    }

    [Serializable]
    public sealed class RunOnceEvent : RunOnceEventBase, IRunOnceEvent
    {
        [SerializeField] UnityEvent _event;

        protected override UnityEventBase baseEvent => _event;

        public void AddListener(UnityAction handler)
        {
            if (hasInvoked)
            {
                handler.Invoke();
            }
            else
            {
                _event.AddListener(handler);
            }
        }

        public void Invoke()
        {
            MarkInvoked();

            _event.Invoke();
        }
    }

    /// <summary>
    /// Event that can only run once, adding handler late will invoke right away
    /// <para>
    /// See <see cref="IRunOnceEvent"/> for more details
    /// </para>
    /// </summary>
    public interface IRunOnceEvent<T0>
    {
        void AddHandler(UnityAction<T0> handler);
    }

    [Serializable]
    public abstract class RunOnceEvent<T0> : RunOnceEventBase, IRunOnceEvent<T0>
    {
        [SerializeField] UnityEvent<T0> innerEvent;
        protected override UnityEventBase baseEvent => innerEvent;

        T0 arg0;

        public void AddHandler(UnityAction<T0> handler)
        {
            if (hasInvoked)
            {
                handler.Invoke(arg0);
            }
            else
            {
                innerEvent.AddListener(handler);
            }
        }

        public void Invoke(T0 arg0)
        {
            MarkInvoked();

            this.arg0 = arg0;
            innerEvent.Invoke(arg0);
        }
    }

    /// <summary>
    /// Event that can only run once, adding handler late will invoke right away
    /// <para>
    /// See <see cref="IRunOnceEvent"/> for more details
    /// </para>
    /// </summary>
    public interface IRunOnceEvent<T0, T1>
    {
        void AddHandler(UnityAction<T0, T1> handler);
    }
    [Serializable]
    public abstract class RunOnceEvent<T0, T1> : RunOnceEventBase, IRunOnceEvent<T0, T1>
    {
        [SerializeField] UnityEvent<T0, T1> innerEvent;
        protected override UnityEventBase baseEvent => innerEvent;

        T0 arg0;
        T1 arg1;

        public void AddHandler(UnityAction<T0, T1> handler)
        {
            if (hasInvoked)
            {
                handler.Invoke(arg0, arg1);
            }
            else
            {
                innerEvent.AddListener(handler);
            }
        }

        public void Invoke(T0 arg0, T1 arg1)
        {
            MarkInvoked();

            this.arg0 = arg0;
            this.arg1 = arg1;
            innerEvent.Invoke(arg0, arg1);
        }
    }
}
