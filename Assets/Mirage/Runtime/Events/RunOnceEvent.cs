using System;
using UnityEngine;
using UnityEngine.Events;

namespace Mirage.Events
{
    /// <summary>
    /// Event that can only run once, adding handler late will it invoke right away
    /// </summary>
    [Serializable]
    public sealed class RunOnceEvent : RunOnceEventBase, IRunOnceEvent
    {
        [SerializeField] UnityEvent _event = new UnityEvent();

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
    /// Version of <see cref="RunOnceEvent"/> with 1 argument
    /// <para>Create a non-generic class inheirting from this to use in inspector. Same rules as <see cref="UnityEvent"/></para>
    /// </summary>
    /// <typeparam name="T0">argument 0</typeparam>
    /// <typeparam name="TEvent">UnityEvent</typeparam>
    [Serializable]
    public abstract class RunOnceEvent<T0, TEvent> : RunOnceEventBase, IRunOnceEvent<T0>
        where TEvent : UnityEvent<T0>, new()
    {
        [SerializeField] TEvent _event = new TEvent();
        protected override UnityEventBase baseEvent => _event;

        T0 arg0;

        public void AddListener(UnityAction<T0> handler)
        {
            if (hasInvoked)
            {
                handler.Invoke(arg0);
            }
            else
            {
                _event.AddListener(handler);
            }
        }

        public void Invoke(T0 arg0)
        {
            MarkInvoked();

            this.arg0 = arg0;
            _event.Invoke(arg0);
        }
    }

    /// <summary>
    /// Version of <see cref="RunOnceEvent"/> with 2 arguments
    /// <para>Create a non-generic class inheirting from this to use in inspector. Same rules as <see cref="UnityEvent"/></para>
    /// </summary>
    /// <typeparam name="T0"></typeparam>
    /// <typeparam name="T1"></typeparam>
    [Serializable]
    public abstract class RunOnceEvent<T0, T1, TEvent> : RunOnceEventBase, IRunOnceEvent<T0, T1>
        where TEvent : UnityEvent<T0, T1>, new()
    {
        [SerializeField] TEvent _event = new TEvent();
        protected override UnityEventBase baseEvent => _event;

        T0 arg0;
        T1 arg1;

        public void AddListener(UnityAction<T0, T1> handler)
        {
            if (hasInvoked)
            {
                handler.Invoke(arg0, arg1);
            }
            else
            {
                _event.AddListener(handler);
            }
        }

        public void Invoke(T0 arg0, T1 arg1)
        {
            MarkInvoked();

            this.arg0 = arg0;
            this.arg1 = arg1;
            _event.Invoke(arg0, arg1);
        }
    }
}
