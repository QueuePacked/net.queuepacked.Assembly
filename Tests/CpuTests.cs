using System.Linq;
using net.queuepacked.Assembly.CPU;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    internal class CpuTests
    {
        [Test]
        public void Construct()
        {
            Cpu cpu = new();
            Assert.That(cpu.Stopped, Is.False);
            Assert.That(cpu.TickChanges, Is.Zero);
            Assert.That(cpu.State, Is.EqualTo(State.Empty));

            State state = new(4, 7, true, null, null, Step.ReadOpCode, [38, 9], false, false, string.Empty, 0);
            cpu = new Cpu(state);
            Assert.That(cpu.Stopped, Is.False);
            Assert.That(cpu.TickChanges, Is.Zero);
            Assert.That(cpu.State, Is.EqualTo(state));

            cpu = new Cpu([1, 2, 3, 4]);
            Assert.That(cpu.Stopped, Is.False);
            Assert.That(cpu.TickChanges, Is.Zero);
            Assert.That(cpu.State.Memory[0], Is.EqualTo(1));
            Assert.That(cpu.State.Memory[1], Is.EqualTo(2));
            Assert.That(cpu.State.Memory[2], Is.EqualTo(3));
            Assert.That(cpu.State.Memory[3], Is.EqualTo(4));
        }

        private void TestOperationSequence(State[] states)
        {
            Cpu cpu = new(states[0]);
            Assert.That(cpu.Stopped, Is.False);
            Assert.That(cpu.TickChanges, Is.Zero);
            Assert.That(cpu.State, Is.EqualTo(states[0]));

            for (int i = 1; i < states.Length; ++i)
            {
                Assert.That(cpu.Forward(), $"Step advance {i}");

                Assert.That(cpu.Forward(), $"Step {i}");
                Assert.That(cpu.Stopped, Is.False, $"Step {i}");
                Assert.That(cpu.TickChanges, Is.EqualTo(i * 2), $"Step {i}");
                Assert.That(cpu.State, Is.EqualTo(states[i]), $"Step {i}");
            }
        }

        [Test]
        public void Operation_Nop()
        {
            State[] states =
            [
                new State(0, 0, false, null, null, Step.ReadOpCode, [(byte)Operation.NOP], false, false, string.Empty, 0),
                new State(0, 0, false, Operation.NOP, null, Step.ReadOptionalArgument, [(byte)Operation.NOP], false, false, string.Empty, 0),
                new State(0, 0, false, Operation.NOP, null, Step.ProcessOperation, [(byte)Operation.NOP], false, false, string.Empty, 0),
                new State(0, 0, false, Operation.NOP, null, Step.IncrementProgramCounter, [(byte)Operation.NOP], false, false, string.Empty, 0),
                new State(1, 0, false, null, null, Step.ReadOpCode, [(byte)Operation.NOP], false, false, string.Empty, 0)
            ];

            TestOperationSequence(states);
        }

        [Test]
        public void Operation_Rac()
        {
            State[] states =
            [
                new State(0, 3, false, null, null, Step.ReadOpCode, [(byte)Operation.RAC,2,0], false, false, string.Empty, 0),
                new State(0, 3, false, Operation.RAC, null, Step.ReadOptionalArgument, [(byte)Operation.RAC,2,0], false, false, string.Empty, 0),
                new State(0, 3, false, Operation.RAC, 2, Step.ProcessOperation, [(byte)Operation.RAC,2,0], false, false, string.Empty, 0),
                new State(0, 3, false, Operation.RAC, 2, Step.IncrementProgramCounter, [(byte)Operation.RAC,2,3], false, false, string.Empty, 0),
                new State(2, 3, false, null, null, Step.ReadOpCode, [(byte)Operation.RAC,2,3], false, false, string.Empty, 0)
            ];

            TestOperationSequence(states);
        }

        [Test]
        public void Operation_Wac()
        {
            State[] states =
            [
                new State(0, 0, false, null, null, Step.ReadOpCode, [(byte)Operation.WAC,2,5], false, false, string.Empty, 0),
                new State(0, 0, false, Operation.WAC, null, Step.ReadOptionalArgument, [(byte)Operation.WAC, 2,5], false, false, string.Empty, 0),
                new State(0, 0, false, Operation.WAC, 2, Step.ProcessOperation, [(byte)Operation.WAC, 2,5], false, false, string.Empty, 0),
                new State(0, 5, false, Operation.WAC, 2, Step.IncrementProgramCounter, [(byte)Operation.WAC, 2,5], false, false, string.Empty, 0),
                new State(2, 5, false, null, null, Step.ReadOpCode, [(byte)Operation.WAC, 2,5], false, false, string.Empty, 0)
            ];

            TestOperationSequence(states);
        }

        [Test]
        public void Operation_Swp()
        {
            State[] states =
            [
                new State(1, 3, false, null, null, Step.ReadOpCode, [0, (byte)Operation.SWP], false, false, string.Empty, 0),
                new State(1, 3, false, Operation.SWP, null, Step.ReadOptionalArgument, [0, (byte)Operation.SWP], false, false, string.Empty, 0),
                new State(1, 3, false, Operation.SWP, null, Step.ProcessOperation, [0, (byte)Operation.SWP], false, false, string.Empty, 0),
                new State(3, 1, false, Operation.SWP, null, Step.IncrementProgramCounter, [0, (byte)Operation.SWP], false, false, string.Empty, 0),
                new State(3, 1, false, null, null, Step.ReadOpCode, [0, (byte)Operation.SWP], false, false, string.Empty, 0)
            ];

            TestOperationSequence(states);
        }

        [Test]
        public void Operation_Add()
        {
            State[] states =
            [
                new State(0, 3, true, null, null, Step.ReadOpCode, [(byte)Operation.ADD,2,5], false, false, string.Empty, 0),
                new State(0, 3, true, Operation.ADD, null, Step.ReadOptionalArgument, [(byte)Operation.ADD, 2,5], false, false, string.Empty, 0),
                new State(0, 3, true, Operation.ADD, 2, Step.ProcessOperation, [(byte)Operation.ADD, 2,5], false, false, string.Empty, 0),
                new State(0, 8, false, Operation.ADD, 2, Step.IncrementProgramCounter, [(byte)Operation.ADD, 2,5], false, false, string.Empty, 0),
                new State(2, 8, false, null, null, Step.ReadOpCode, [(byte)Operation.ADD, 2,5], false, false, string.Empty, 0)
            ];

            TestOperationSequence(states);

            states =
            [
                new State(0, 253, false, null, null, Step.ReadOpCode, [(byte)Operation.ADD,2,5], false, false, string.Empty, 0),
                new State(0, 253, false, Operation.ADD, null, Step.ReadOptionalArgument, [(byte)Operation.ADD, 2,5], false, false, string.Empty, 0),
                new State(0, 253, false, Operation.ADD, 2, Step.ProcessOperation, [(byte)Operation.ADD, 2,5], false, false, string.Empty, 0),
                new State(0, 2, true, Operation.ADD, 2, Step.IncrementProgramCounter, [(byte)Operation.ADD, 2,5], false, false, string.Empty, 0),
                new State(2, 2, true, null, null, Step.ReadOpCode, [(byte)Operation.ADD, 2,5], false, false, string.Empty, 0)
            ];

            TestOperationSequence(states);
        }

        [Test]
        public void Operation_Sub()
        {
            State[] states =
            [
                new State(0, 8, true, null, null, Step.ReadOpCode, [(byte)Operation.SUB,2,5], false, false, string.Empty, 0),
                new State(0, 8, true, Operation.SUB, null, Step.ReadOptionalArgument, [(byte)Operation.SUB, 2,5], false, false, string.Empty, 0),
                new State(0, 8, true, Operation.SUB, 2, Step.ProcessOperation, [(byte)Operation.SUB, 2,5], false, false, string.Empty, 0),
                new State(0, 3, false, Operation.SUB, 2, Step.IncrementProgramCounter, [(byte)Operation.SUB, 2,5], false, false, string.Empty, 0),
                new State(2, 3, false, null, null, Step.ReadOpCode, [(byte)Operation.SUB, 2,5], false, false, string.Empty, 0)
            ];

            TestOperationSequence(states);

            states =
            [
                new State(0, 2, false, null, null, Step.ReadOpCode, [(byte)Operation.SUB, 2,5], false, false, string.Empty, 0),
                new State(0, 2, false, Operation.SUB, null, Step.ReadOptionalArgument, [(byte)Operation.SUB, 2,5], false, false, string.Empty, 0),
                new State(0, 2, false, Operation.SUB, 2, Step.ProcessOperation, [(byte)Operation.SUB, 2,5], false, false, string.Empty, 0),
                new State(0, 253, true, Operation.SUB, 2, Step.IncrementProgramCounter, [(byte)Operation.SUB, 2,5], false, false, string.Empty, 0),
                new State(2, 253, true, null, null, Step.ReadOpCode, [(byte)Operation.SUB, 2,5], false, false, string.Empty, 0)
            ];

            TestOperationSequence(states);
        }

        [Test]
        public void Operation_Bsl()
        {
            State[] states =
            [
                new State(0, 5, true, null, null, Step.ReadOpCode, [(byte)Operation.BSL], false, false, string.Empty, 0),
                new State(0, 5, true, Operation.BSL, null, Step.ReadOptionalArgument, [(byte)Operation.BSL], false, false, string.Empty, 0),
                new State(0, 5, true, Operation.BSL, null, Step.ProcessOperation, [(byte)Operation.BSL], false, false, string.Empty, 0),
                new State(0, 10, false, Operation.BSL, null, Step.IncrementProgramCounter, [(byte)Operation.BSL], false, false, string.Empty, 0),
                new State(1, 10, false, null, null, Step.ReadOpCode, [(byte)Operation.BSL], false, false, string.Empty, 0)
            ];

            TestOperationSequence(states);

            states =
            [
                new State(0, 255, false, null, null, Step.ReadOpCode, [(byte)Operation.BSL], false, false, string.Empty, 0),
                new State(0, 255, false, Operation.BSL, null, Step.ReadOptionalArgument, [(byte)Operation.BSL], false, false, string.Empty, 0),
                new State(0, 255, false, Operation.BSL, null, Step.ProcessOperation, [(byte)Operation.BSL], false, false, string.Empty, 0),
                new State(0, 254, true, Operation.BSL, null, Step.IncrementProgramCounter, [(byte)Operation.BSL], false, false, string.Empty, 0),
                new State(1, 254, true, null, null, Step.ReadOpCode, [(byte)Operation.BSL], false, false, string.Empty, 0)
            ];

            TestOperationSequence(states);
        }

        [Test]
        public void Operation_Bsr()
        {
            State[] states =
            [
                new State(0, 10, true, null, null, Step.ReadOpCode, [(byte)Operation.BSR], false, false, string.Empty, 0),
                new State(0, 10, true, Operation.BSR, null, Step.ReadOptionalArgument, [(byte)Operation.BSR], false, false, string.Empty, 0),
                new State(0, 10, true, Operation.BSR, null, Step.ProcessOperation, [(byte)Operation.BSR], false, false, string.Empty, 0),
                new State(0, 5, false, Operation.BSR, null, Step.IncrementProgramCounter, [(byte)Operation.BSR], false, false, string.Empty, 0),
                new State(1, 5, false, null, null, Step.ReadOpCode, [(byte)Operation.BSR], false, false, string.Empty, 0)
            ];

            TestOperationSequence(states);

            states =
            [
                new State(0, 5, false, null, null, Step.ReadOpCode, [(byte)Operation.BSR], false, false, string.Empty, 0),
                new State(0, 5, false, Operation.BSR, null, Step.ReadOptionalArgument, [(byte)Operation.BSR], false, false, string.Empty, 0),
                new State(0, 5, false, Operation.BSR, null, Step.ProcessOperation, [(byte)Operation.BSR], false, false, string.Empty, 0),
                new State(0, 2, true, Operation.BSR, null, Step.IncrementProgramCounter, [(byte)Operation.BSR], false, false, string.Empty, 0),
                new State(1, 2, true, null, null, Step.ReadOpCode, [(byte)Operation.BSR], false, false, string.Empty, 0)
            ];

            TestOperationSequence(states);
        }

        [Test]
        public void Operation_Jmp()
        {
            State[] states =
            [
                new State(0, 0, false, null, null, Step.ReadOpCode, [(byte)Operation.JMP,3], false, false, string.Empty, 0),
                new State(0, 0, false, Operation.JMP, null, Step.ReadOptionalArgument, [(byte)Operation.JMP, 3], false, false, string.Empty, 0),
                new State(0, 0, false, Operation.JMP, 3, Step.ProcessOperation, [(byte)Operation.JMP, 3], false, false, string.Empty, 0),
                new State(3, 0, false, Operation.JMP, 3, Step.IncrementProgramCounter, [(byte)Operation.JMP, 3], false, false, string.Empty, 0),
                new State(3, 0, false, null, null, Step.ReadOpCode, [(byte)Operation.JMP, 3], false, false, string.Empty, 0)
            ];

            TestOperationSequence(states);
        }

        [Test]
        public void Operation_Jpz()
        {
            State[] states =
            [
                new State(0, 1, false, null, null, Step.ReadOpCode, [(byte)Operation.JPZ,3], false, false, string.Empty, 0),
                new State(0, 1, false, Operation.JPZ, null, Step.ReadOptionalArgument, [(byte)Operation.JPZ, 3], false, false, string.Empty, 0),
                new State(0, 1, false, Operation.JPZ, 3, Step.ProcessOperation, [(byte)Operation.JPZ, 3], false, false, string.Empty, 0),
                new State(0, 1, false, Operation.JPZ, 3, Step.IncrementProgramCounter, [(byte)Operation.JPZ, 3], false, false, string.Empty, 0),
                new State(2, 1, false, null, null, Step.ReadOpCode, [(byte)Operation.JPZ, 3], false, false, string.Empty, 0)
            ];

            TestOperationSequence(states);

            states =
            [
                new State(0, 0, false, null, null, Step.ReadOpCode, [(byte)Operation.JPZ,3], false, false, string.Empty, 0),
                new State(0, 0, false, Operation.JPZ, null, Step.ReadOptionalArgument, [(byte)Operation.JPZ, 3], false, false, string.Empty, 0),
                new State(0, 0, false, Operation.JPZ, 3, Step.ProcessOperation, [(byte)Operation.JPZ, 3], false, false, string.Empty, 0),
                new State(3, 0, false, Operation.JPZ, 3, Step.IncrementProgramCounter, [(byte)Operation.JPZ, 3], false, false, string.Empty, 0),
                new State(3, 0, false, null, null, Step.ReadOpCode, [(byte)Operation.JPZ, 3], false, false, string.Empty, 0)
            ];

            TestOperationSequence(states);
        }

        [Test]
        public void Operation_Jpf()
        {
            State[] states =
            [
                new State(0, 0, false, null, null, Step.ReadOpCode, [(byte)Operation.JPF, 3], false, false, string.Empty, 0),
                new State(0, 0, false, Operation.JPF, null, Step.ReadOptionalArgument, [(byte)Operation.JPF, 3], false, false, string.Empty, 0),
                new State(0, 0, false, Operation.JPF, 3, Step.ProcessOperation, [(byte)Operation.JPF, 3], false, false, string.Empty, 0),
                new State(0, 0, false, Operation.JPF, 3, Step.IncrementProgramCounter, [(byte)Operation.JPF, 3], false, false, string.Empty, 0),
                new State(2, 0, false, null, null, Step.ReadOpCode, [(byte)Operation.JPF, 3], false, false, string.Empty, 0)
            ];

            TestOperationSequence(states);

            states =
            [
                new State(0, 0, true, null, null, Step.ReadOpCode, [(byte)Operation.JPF, 3], false, false, string.Empty, 0),
                new State(0, 0, true, Operation.JPF, null, Step.ReadOptionalArgument, [(byte)Operation.JPF, 3], false, false, string.Empty, 0),
                new State(0, 0, true, Operation.JPF, 3, Step.ProcessOperation, [(byte)Operation.JPF, 3], false, false, string.Empty, 0),
                new State(3, 0, true, Operation.JPF, 3, Step.IncrementProgramCounter, [(byte)Operation.JPF, 3], false, false, string.Empty, 0),
                new State(3, 0, true, null, null, Step.ReadOpCode, [(byte)Operation.JPF, 3], false, false, string.Empty, 0)
            ];

            TestOperationSequence(states);
        }

        [Test]
        public void Stopping()
        {
            State[] states =
            [
                new State(255, 1, false, null, null, Step.ReadOpCode,Enumerable.Repeat((byte)Operation.NOP, 255).Append((byte)Operation.BSL), false, false, string.Empty,0),
                new State(255, 1, false, Operation.BSL, null, Step.ReadOptionalArgument, Enumerable.Repeat((byte)Operation.NOP, 255).Append((byte)Operation.BSL), false, false, string.Empty,0),
                new State(255, 1, false, Operation.BSL, null, Step.ProcessOperation, Enumerable.Repeat((byte)Operation.NOP, 255).Append((byte)Operation.BSL), false, false, string.Empty,0),
                new State(255, 2, false, Operation.BSL, null, Step.IncrementProgramCounter, Enumerable.Repeat((byte)Operation.NOP, 255).Append((byte)Operation.BSL), false, false, string.Empty,0),
                new State(255, 2, false, null, null, Step.ReadOpCode, Enumerable.Repeat((byte)Operation.NOP, 255).Append((byte)Operation.BSL), true, false, string.Empty,0)
            ];

            Cpu cpu = new(states[0]);
            for (int i = 1; i < 4; ++i)
            {
                Assert.That(cpu.Forward(), $"Step advance {i}");

                Assert.That(cpu.Forward(), $"Step {i}");
                Assert.That(cpu.Stopped, Is.False, $"Step {i}");
                Assert.That(cpu.State, Is.EqualTo(states[i]), $"Step {i}");
            }

            Assert.That(cpu.Forward, Is.False);
            Assert.That(cpu.Stopped);
        }

        [Test]
        public void Operation_Rio()
        {
            State[] states =
            [
                new State(0, 0, false, null, null, Step.ReadOpCode, [(byte)Operation.RIO,2,0], false, false, string.Empty, 3),
                new State(0, 0, false, Operation.RIO, null, Step.ReadOptionalArgument, [(byte)Operation.RIO, 2,0], false, false, string.Empty, 3),
                new State(0, 0, false, Operation.RIO, 2, Step.ProcessOperation, [(byte)Operation.RIO, 2,0], false, false, string.Empty, 3),
                new State(0, 0, false, Operation.RIO, 2, Step.IncrementProgramCounter, [(byte)Operation.RIO, 2,3], false, false, string.Empty, 3),
                new State(2, 0, false, null, null, Step.ReadOpCode, [(byte)Operation.RIO, 2,3], false, false, string.Empty, 3)
            ];

            TestOperationSequence(states);
        }

        [Test]
        public void Operation_Wio()
        {
            State[] states =
            [
                new State(0, 0, false, null, null, Step.ReadOpCode, [(byte)Operation.WIO, 2,5], false, false, string.Empty, 0),
                new State(0, 0, false, Operation.WIO, null, Step.ReadOptionalArgument, [(byte)Operation.WIO, 2,5], false, false, string.Empty, 0),
                new State(0, 0, false, Operation.WIO, 2, Step.ProcessOperation, [(byte)Operation.WIO, 2,5], false, false, string.Empty, 0),
                new State(0, 0, false, Operation.WIO, 2, Step.IncrementProgramCounter, [(byte)Operation.WIO, 2,5], false, false, string.Empty, 5),
                new State(2, 0, false, null, null, Step.ReadOpCode, [(byte)Operation.WIO, 2,5], false, false, string.Empty, 5)
            ];

            TestOperationSequence(states);
        }
    }
}
