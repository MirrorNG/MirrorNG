using System;
using NUnit.Framework;
using NUnit.Framework.Internal;
using UnityEngine.Events;

namespace Mirage.Tests
{
    public abstract class RunOnceEventTestsBase
    {
        int listenerCallCount;
        protected void TestListener() => listenerCallCount++;

        protected abstract void Init();
        protected abstract void Invoke();
        protected abstract void AddListener();
        protected abstract void Reset();


        [SetUp]
        public void Setup()
        {
            listenerCallCount = 0;
            Init();
        }

        [Test]
        public void EventCanBeInvokedOnce()
        {
            AddListener();
            Invoke();
            Assert.That(listenerCallCount, Is.EqualTo(1));
        }


        [Test]
        public void EventCantBeInvokedTwice()
        {
            AddListener();
            Invoke();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            {
                Invoke();
            });
            Assert.That(exception, Has.Message.EqualTo("Event can only be invoked once Invoke"));
        }

        [Test]
        public void EventCantBeInvokedEmpty()
        {
            Assert.DoesNotThrow(() =>
            {
                Invoke();
            });
        }

        [Test]
        public void AddingListenerLateRunsListener()
        {
            Invoke();
            Assert.That(listenerCallCount, Is.EqualTo(0));
            AddListener();
            Assert.That(listenerCallCount, Is.EqualTo(1));
        }

        [Test]
        public void ResetEventAllowsEventToBeInvokedAgain()
        {
            AddListener();

            Invoke();
            Assert.That(listenerCallCount, Is.EqualTo(1));

            Reset();

            AddListener();

            Invoke();
            Assert.That(listenerCallCount, Is.EqualTo(2));
        }

        [Test]
        public void ResetEventRemovesOldListners()
        {
            AddListener();

            Invoke();
            Assert.That(listenerCallCount, Is.EqualTo(1));

            Reset();

            Assert.DoesNotThrow(() =>
            {
                Invoke();
            });
            // listener removed so no increase to count
            Assert.That(listenerCallCount, Is.EqualTo(1));
        }
    }


    public class RunOnceEvent0ArgTest : RunOnceEventTestsBase
    {
        RunOnceEvent onceEvent;
        protected override void Init()
        {
            onceEvent = new RunOnceEvent();
        }

        protected override void Invoke()
        {
            onceEvent.Invoke();
        }

        protected override void AddListener()
        {
            onceEvent.AddListener(TestListener);
        }

        protected override void Reset()
        {
            onceEvent.Reset();
        }
    }


    public class IntUnityEvent : UnityEvent<int> { }
    public class IntRunOnceEvent : RunOnceEvent<int, IntUnityEvent> { }
    public class RunOnceEvent1ArgTest : RunOnceEventTestsBase
    {
        IntRunOnceEvent onceEvent;

        protected override void Init()
        {
            onceEvent = new IntRunOnceEvent();
        }

        protected override void Invoke()
        {
            onceEvent.Invoke(default);
        }

        protected override void AddListener()
        {
            onceEvent.AddListener((_) => TestListener());
        }

        protected override void Reset()
        {
            onceEvent.Reset();
        }

        [Test]
        public void ListenerIsInvokedWithCorrectArgs()
        {
            const int arg0 = 10;

            int callCount = 0;

            onceEvent.AddListener((a0) =>
            {
                callCount++;
                Assert.That(a0, Is.EqualTo(arg0));
            });


            onceEvent.Invoke(arg0);
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void ListenerIsInvokedLateWithCorrectArgs()
        {
            const int arg0 = 10;

            int callCount = 0;

            // invoke before adding handler
            onceEvent.Invoke(arg0);

            onceEvent.AddListener((a0) =>
            {
                callCount++;
                Assert.That(a0, Is.EqualTo(arg0));
            });

            Assert.That(callCount, Is.EqualTo(1));
        }
    }


    public class IntStringUnityEvent : UnityEvent<int, string> { }
    public class IntStringRunOnceEvent : RunOnceEvent<int, string, IntStringUnityEvent> { }
    public class RunOnceEvent2ArgTest : RunOnceEventTestsBase
    {
        IntStringRunOnceEvent onceEvent;

        protected override void Init()
        {
            onceEvent = new IntStringRunOnceEvent();
        }

        protected override void Invoke()
        {
            onceEvent.Invoke(default, default);
        }

        protected override void AddListener()
        {
            onceEvent.AddListener((_, __) => TestListener());
        }

        protected override void Reset()
        {
            onceEvent.Reset();
        }

        [Test]
        public void ListenerIsInvokedWithCorrectArgs()
        {
            const int arg0 = 10;
            const string arg1 = "hell world";

            int callCount = 0;

            onceEvent.AddListener((a0, a1) =>
            {
                callCount++;
                Assert.That(a0, Is.EqualTo(arg0));
                Assert.That(a1, Is.EqualTo(arg1));
            });


            onceEvent.Invoke(arg0, arg1);
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void ListenerIsInvokedLateWithCorrectArgs()
        {
            const int arg0 = 10;
            const string arg1 = "hell world";

            int callCount = 0;

            // invoke before adding handler
            onceEvent.Invoke(arg0, arg1);

            onceEvent.AddListener((a0, a1) =>
            {
                callCount++;
                Assert.That(a0, Is.EqualTo(arg0));
                Assert.That(a1, Is.EqualTo(arg1));
            });

            Assert.That(callCount, Is.EqualTo(1));
        }
    }
}
