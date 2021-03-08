using System;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Mirage.Tests
{
    public class RunOnceEventTests
    {
        RunOnceEvent onceEvent;
        int listenerCallCount;
        void TestListener() => listenerCallCount++;

        [SetUp]
        public void Setup()
        {
            onceEvent = new RunOnceEvent();
            listenerCallCount = 0;
        }

        [Test]
        public void EventCanBeInvokedOnce()
        {
            onceEvent.AddListener(TestListener);

            onceEvent.Invoke();
            Assert.That(listenerCallCount, Is.EqualTo(1));
        }

        [Test]
        public void EventCantBeInvokedTwice()
        {
            onceEvent.AddListener(TestListener);

            onceEvent.Invoke();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            {
                onceEvent.Invoke();
            });
            Assert.That(exception, Has.Message.EqualTo("Event can only be invoked once Invoke"));
        }

        [Test]
        public void EventCantBeInvokedEmpty()
        {
            Assert.DoesNotThrow(() =>
            {
                onceEvent.Invoke();
            });
        }

        [Test]
        public void AddingListenerLateRunsListener()
        {
            onceEvent.Invoke();
            Assert.That(listenerCallCount, Is.EqualTo(0));
            onceEvent.AddListener(TestListener);
            Assert.That(listenerCallCount, Is.EqualTo(1));
        }

        [Test]
        public void ResetEventAllowsEventToBeInvokedAgain()
        {
            onceEvent.AddListener(TestListener);

            onceEvent.Invoke();
            Assert.That(listenerCallCount, Is.EqualTo(1));

            // Reset
            onceEvent.Reset();

            onceEvent.AddListener(TestListener);

            onceEvent.Invoke();
            Assert.That(listenerCallCount, Is.EqualTo(2));
        }

        [Test]
        public void ResetEventRemovesOldListners()
        {
            onceEvent.AddListener(TestListener);

            onceEvent.Invoke();
            Assert.That(listenerCallCount, Is.EqualTo(1));

            // Reset
            onceEvent.Reset();

            Assert.DoesNotThrow(() =>
            {
                onceEvent.Invoke();
            });
            // listener removed so no increase to count
            Assert.That(listenerCallCount, Is.EqualTo(1));
        }
    }
}
