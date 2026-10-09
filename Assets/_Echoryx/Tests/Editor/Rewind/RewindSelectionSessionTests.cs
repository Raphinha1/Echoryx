using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Echoryx.Rewind.Tests
{
    public sealed class RewindSelectionSessionTests
    {
        private sealed class FakeMechanism : IRewindableMechanism
        {
            public RewindMode Mode { get; private set; }
            public bool IsAvailable { get; set; } = true;
            public bool CanRewind { get; set; } = true;
            public int Begins { get; private set; }
            public int Ends { get; private set; }

            public bool BeginRewind()
            {
                Begins++;
                if (!CanRewind || Mode == RewindMode.Rewinding) return false;
                Mode = RewindMode.Rewinding;
                return true;
            }

            public bool EndRewind()
            {
                Ends++;
                Mode = RewindMode.Normal;
                return true;
            }

            public void ResetHistory() => Mode = RewindMode.Normal;
        }

        [Test]
        public void PressLocksOneTargetAndReleaseEndsRewind()
        {
            var session = new RewindSelectionSession();
            var first = new FakeMechanism();
            var second = new FakeMechanism();
            session.Update(true, first);
            session.Update(true, second);

            Assert.That(session.Target, Is.SameAs(first));
            Assert.That(first.Begins, Is.EqualTo(1));
            Assert.That(second.Begins, Is.Zero);
            session.Update(false, second);
            Assert.That(first.Ends, Is.EqualTo(1));
            Assert.That(session.Target, Is.Null);
            Assert.That(session.IsHolding, Is.False);

            session.Update(true, second);
            Assert.That(session.Target, Is.SameAs(second));
            Assert.That(second.Begins, Is.EqualTo(1));
        }

        [Test]
        public void InsufficientHistoryWaitsForSameTargetWithoutSwitching()
        {
            var session = new RewindSelectionSession();
            var first = new FakeMechanism { CanRewind = false };
            var second = new FakeMechanism();
            session.Update(true, first);
            first.CanRewind = true;
            session.Update(true, second);
            Assert.That(session.Target, Is.SameAs(first));
            Assert.That(first.Begins, Is.EqualTo(1));
            Assert.That(second.Begins, Is.Zero);

            session.Update(false, null);
            session.Update(true, second);
            Assert.That(session.Target, Is.SameAs(second));
        }

        [Test]
        public void DisabledTargetEndsAndCannotBeReplacedDuringSamePress()
        {
            var session = new RewindSelectionSession();
            var first = new FakeMechanism();
            var second = new FakeMechanism();
            session.Update(true, first);
            first.IsAvailable = false;
            session.Update(true, second);

            Assert.That(first.Ends, Is.EqualTo(1));
            Assert.That(session.Target, Is.Null);
            Assert.That(second.Begins, Is.Zero);
            session.Update(false, null);
            session.Update(true, second);
            Assert.That(second.Begins, Is.EqualTo(1));
        }

        [Test]
        public void DestroyedUnityTargetIsReleasedWithoutAccessingIt()
        {
            var target = ScriptableObject.CreateInstance<ScriptableTarget>();
            var session = new RewindSelectionSession();
            session.Update(true, target);
            Assert.That(session.Target, Is.SameAs(target));
            Object.DestroyImmediate(target);

            Assert.DoesNotThrow(() => session.Update(true, null));
            Assert.That(session.Target, Is.Null);
            Assert.DoesNotThrow(session.Release);
        }

        [Test]
        public void ReleaseWithoutTargetIsSafeAndIdempotent()
        {
            var session = new RewindSelectionSession();
            session.Release();
            session.Update(true, null);
            session.Release();
            session.Release();
            Assert.That(session.Target, Is.Null);
            Assert.That(session.IsHolding, Is.False);
        }

        [Test]
        public void OnlyOneConcreteMechanismCanRewindGlobally()
        {
            var firstObject = new GameObject("First Rotor");
            var secondObject = new GameObject("Second Rotor");
            var first = firstObject.AddComponent<RewindTestRotor>();
            var second = secondObject.AddComponent<RewindTestRotor>();
            try
            {
                SetHistory(first);
                SetHistory(second);
                Assert.That(first.BeginRewind(), Is.True);
                Assert.That(second.BeginRewind(), Is.False);
                Assert.That(first.EndRewind(), Is.True);
                Assert.That(second.BeginRewind(), Is.True);
            }
            finally
            {
                first.EndRewind();
                second.EndRewind();
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
            }
        }

        private static void SetHistory(RewindTestRotor rotor)
        {
            var history = new RewindHistory();
            var pose = new RewindPose(rotor.transform.position, rotor.transform.rotation);
            history.Reset(pose);
            history.AdvanceNormal(1d, pose);
            typeof(RewindableMechanism).GetField("history", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(rotor, history);
        }
    }

    public sealed class ScriptableTarget : ScriptableObject, IRewindableMechanism
    {
        public RewindMode Mode { get; private set; }
        public bool IsAvailable => true;
        public bool CanRewind => true;
        public bool BeginRewind() { Mode = RewindMode.Rewinding; return true; }
        public bool EndRewind() { Mode = RewindMode.Normal; return true; }
        public void ResetHistory() { Mode = RewindMode.Normal; }
    }
}
