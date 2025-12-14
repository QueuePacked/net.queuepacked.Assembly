using System.Linq;
using net.queuepacked.Assembly.CPU;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    internal class StateTests
    {
        [Test]
        public void Construct()
        {
            State state = new([]);
            Assert.That(state, Is.EqualTo(State.Empty));

            state = new State(0, 0, false, null, null, Step.ReadOpCode, [], false, false, string.Empty,0);
            Assert.That(state, Is.EqualTo(State.Empty));
        }

        [Test]
        public void Equality()
        {
            State[] states =
            [
                new State(1, 2, false, null, null, Step.ReadOpCode, Enumerable.Range(0, 256).Select(i => (byte)i), false, false, string.Empty,0),
                new State(1, 2, false, null, null, Step.ReadOpCode, Enumerable.Range(0, 256).Select(i => (byte)i), false, false, string.Empty,0),
                new State(0, 2, false, null, null, Step.ReadOpCode, Enumerable.Range(0, 256).Select(i => (byte)i), false, false, string.Empty,0),
                new State(1, 0, false, null, null, Step.ReadOpCode, Enumerable.Range(0, 256).Select(i => (byte)i), false, false, string.Empty,0),
                new State(1, 2, true, null, null, Step.ReadOpCode, Enumerable.Range(0, 256).Select(i => (byte)i), false, false, string.Empty,0),
                new State(1, 2, false, Operation.ADD, null, Step.ReadOpCode, Enumerable.Range(0, 256).Select(i => (byte)i), false, false, string.Empty,0),
                new State(1, 2, false, null, 0, Step.ReadOpCode, Enumerable.Range(0, 256).Select(i => (byte)i), false, false, string.Empty,0),
                new State(1, 2, false, null, null, Step.ReadOptionalArgument, Enumerable.Range(0, 256).Select(i => (byte)i), false, false, string.Empty,0),
                new State(1, 2, false, null, null, Step.ReadOpCode, Enumerable.Range(0, 256).Select(i => (byte)(255 - i)), false, false, string.Empty,0),
                new State(1, 2, false, null, null, Step.ReadOpCode, Enumerable.Range(0, 256).Select(i => (byte)i), true, false, string.Empty,0),
                new State(1, 2, false, null, null, Step.ReadOpCode, Enumerable.Range(0, 256).Select(i => (byte)i), false, true, string.Empty,0),
                new State(1, 2, false, null, null, Step.ReadOpCode, Enumerable.Range(0, 256).Select(i => (byte)i), false, false, "error",0),
            ];

            Assert.That(states[0], Is.EqualTo(states[1]));
            for (int i = 2; i < states.Length; ++i)
                Assert.That(states[0], Is.Not.EqualTo(states[i]), $"State {i}");
        }
    }
}
