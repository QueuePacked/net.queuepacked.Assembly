using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace net.queuepacked.Assembly.CPU
{
    public record State
    {
        public static readonly State Empty;

        static State()
        {
            Empty = new State(0, 0, false, null, null, Step.ReadOpCode, [.. new byte[byte.MaxValue + 1]], false, false, string.Empty, 0);
        }

        public byte ProgramCounter { get; private init; }

        public byte Accumulator { get; private init; }

        public bool AluFlag { get; private init; }

        public ImmutableArray<byte> Memory { get; private init; }

        public Operation? LoadedOpCode { get; private init; }

        public byte? LoadedMemoryAddressArgument { get; private init; }

        public Step ProcessCycleStep { get; private init; }

        public bool Stopped { get; private init; }

        public bool CycleAdvance { get; private init; }

        public string Error { get; private init; }

        public byte InputOutput { get; private init; }

        public State(byte programCounter, byte accumulator, bool aluFlag, Operation? loadedOpCode, byte? loadedMemoryAddressArgument, Step processCycleStep, IEnumerable<byte> memory, bool stopped, bool cycleAdvance, string error, byte inputOutput)
            : this(programCounter, accumulator, aluFlag, loadedOpCode, loadedMemoryAddressArgument, processCycleStep, stopped, cycleAdvance, error, inputOutput)
        {
            ImmutableArray<byte>.Builder builder = ImmutableArray.CreateBuilder<byte>(byte.MaxValue + 1);
            int count = 0;
            foreach (byte value in memory)
            {
                builder.Add(value);
                if (++count >= builder.Capacity)
                    break;
            }

            for (; count < builder.Capacity; ++count)
                builder.Add(0);

            Memory = builder.MoveToImmutable();
        }

        public State(IEnumerable<byte> memory) : this(0, 0, false, null, null, Step.ReadOpCode, memory, false, false, string.Empty, 0)
        {
        }

        private State(byte programCounter, byte accumulator, bool aluFlag, Operation? loadedOpCode, byte? loadedMemoryAddressArgument, Step processCycleStep, bool stopped, bool cycleAdvance, string error, byte inputOutput)
        {
            ProgramCounter = programCounter;
            Accumulator = accumulator;
            AluFlag = aluFlag;
            LoadedOpCode = loadedOpCode;
            LoadedMemoryAddressArgument = loadedMemoryAddressArgument;
            ProcessCycleStep = processCycleStep;
            Stopped = stopped;
            CycleAdvance = cycleAdvance;
            Error = error;
            InputOutput = inputOutput;
        }

        public State ChangeInputOutput(byte value) => this with { InputOutput = value };

        public State Tick()
        {
            try
            {
                if (CycleAdvance)
                {
                    switch (ProcessCycleStep)
                    {
                        case Step.ReadOpCode:
                            return this with { ProcessCycleStep = Step.ReadOptionalArgument, CycleAdvance = false };
                        case Step.ReadOptionalArgument:
                            return this with { ProcessCycleStep = Step.ProcessOperation, CycleAdvance = false };
                        case Step.ProcessOperation:
                            return this with { ProcessCycleStep = Step.IncrementProgramCounter, CycleAdvance = false };
                        case Step.IncrementProgramCounter:
                            return this with { ProcessCycleStep = Step.ReadOpCode, CycleAdvance = false };

                        default:
                            throw new InvalidOperationException($"Invalid step '{ProcessCycleStep}'");
                    }
                }

                switch (ProcessCycleStep)
                {
                    case Step.ReadOpCode:
                        return ReadOpCode();
                    case Step.ReadOptionalArgument:
                        return ReadOptionalArgument();
                    case Step.ProcessOperation:
                        return ProcessOperation();
                    case Step.IncrementProgramCounter:
                        return IncrementProgramCounter();

                    default:
                        throw new InvalidOperationException($"Invalid step '{ProcessCycleStep}'");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                return this with { Stopped = true, Error = e.Message };
            }
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            HashCode hashCode = new();

            hashCode.Add(ProgramCounter);
            hashCode.Add(Accumulator);
            hashCode.Add(AluFlag);
            hashCode.Add(Memory);
            hashCode.Add(LoadedOpCode);
            hashCode.Add(LoadedMemoryAddressArgument);
            hashCode.Add(ProcessCycleStep);
            hashCode.Add(Stopped);
            hashCode.Add(InputOutput);
            hashCode.Add(Error);
            hashCode.Add(CycleAdvance);

            return hashCode.ToHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            StringBuilder stringBuilder = new();

            stringBuilder.Append(nameof(ProgramCounter)).Append('=').Append(ProgramCounter);
            stringBuilder.Append(", ");
            stringBuilder.Append(nameof(Accumulator)).Append('=').Append(Accumulator);
            stringBuilder.Append(", ");
            stringBuilder.Append(nameof(AluFlag)).Append('=').Append(AluFlag);
            stringBuilder.Append(", ");
            stringBuilder.Append(nameof(InputOutput)).Append('=').Append(InputOutput);
            stringBuilder.Append(", ");
            stringBuilder.Append(nameof(LoadedOpCode)).Append('=').Append(LoadedOpCode);
            stringBuilder.Append(", ");
            stringBuilder.Append(nameof(LoadedMemoryAddressArgument)).Append('=').Append(LoadedMemoryAddressArgument);
            stringBuilder.Append(", ");
            stringBuilder.Append(nameof(ProcessCycleStep)).Append('=').Append(ProcessCycleStep);
            stringBuilder.Append(", ");
            stringBuilder.Append(nameof(CycleAdvance)).Append('=').Append(CycleAdvance);
            stringBuilder.Append(", ");
            stringBuilder.Append(nameof(Stopped)).Append('=').Append(Stopped);
            stringBuilder.Append(", ");
            stringBuilder.Append(nameof(Error)).AppendLine("=").Append('\t').Append(Error.ReplaceLineEndings(Environment.NewLine + "\t"));
            stringBuilder.Append(", ");
            stringBuilder.Append(nameof(Memory)).Append('=');

            int maxIndex = -1;
            int maxLength = 100;//Minimum for shortening
            int index = -1;
            int i;
            for (i = 0; i < Memory.Length; ++i)
            {
                if (Memory[i] == 0)
                {
                    if (index < 0)
                        index = i;
                }
                else
                {
                    if (index >= 0)
                    {
                        if (i - index >= maxLength)
                        {
                            maxIndex = index;
                            maxLength = i - index;
                        }

                        index = -1;
                    }
                }
            }
            if (index >= 0 && i - index >= maxLength)
            {
                maxIndex = index;
                maxLength = i - index;
            }

            bool first = true;
            for (i = 0; i < Memory.Length; ++i)
            {
                if (first)
                    first = false;
                else
                    stringBuilder.Append(", ");

                if (i == maxIndex)
                {
                    stringBuilder.Append("[0 ").Append(i);
                    i += maxLength;
                    stringBuilder.Append('-').Append(i).Append(']');
                    --i;
                }
                else
                    stringBuilder.Append(Memory[i]);
            }

            return stringBuilder.ToString();
        }

        private State ReadOpCode()
        {
            byte opCode = Memory[ProgramCounter];

            if (opCode > 12)
                throw new InvalidOperationException($"Invalid op code {opCode}");

            return this with { LoadedOpCode = (Operation)opCode, CycleAdvance = true };
        }

        private State ReadOptionalArgument()
        {
            if (LoadedOpCode is null)
                throw new InvalidOperationException("No op code was loaded");

            switch (LoadedOpCode)
            {
                case Operation.RAC:
                case Operation.WAC:
                case Operation.ADD:
                case Operation.SUB:
                case Operation.JMP:
                case Operation.JPZ:
                case Operation.JPF:
                case Operation.RIO:
                case Operation.WIO:
                    if (ProgramCounter == byte.MaxValue)
                        throw new InvalidOperationException("Reached end of memory, cannot load argument for operation");
                    break;

                case Operation.NOP:
                case Operation.SWP:
                case Operation.BSL:
                case Operation.BSR:
                    return this with { CycleAdvance = true };

                default:
                    throw new ArgumentOutOfRangeException(nameof(LoadedOpCode), LoadedOpCode, $"Invalid operation {LoadedOpCode}");
            }

            byte address = Memory[ProgramCounter + 1];

            return this with { LoadedMemoryAddressArgument = address, CycleAdvance = true };
        }

        private State ProcessOperation()
        {
            if (LoadedOpCode is null)
                throw new InvalidOperationException("No op code was loaded");

            switch (LoadedOpCode)
            {
                case Operation.NOP:
                    return this with { CycleAdvance = true };

                case Operation.RAC:
                    {
                        if (LoadedMemoryAddressArgument is null)
                            throw new InvalidOperationException("No address argument was loaded");

                        ImmutableArray<byte> memory = Memory.SetItem(LoadedMemoryAddressArgument.Value, Accumulator);
                        return this with { CycleAdvance = true, Memory = memory };
                    }

                case Operation.WAC:
                    {
                        if (LoadedMemoryAddressArgument is null)
                            throw new InvalidOperationException("No address argument was loaded");

                        return this with { CycleAdvance = true, Accumulator = Memory[LoadedMemoryAddressArgument.Value] };
                    }

                case Operation.SWP:
                    {
                        return this with { CycleAdvance = true, ProgramCounter = Accumulator, Accumulator = ProgramCounter };
                    }

                case Operation.ADD:
                    {
                        if (LoadedMemoryAddressArgument is null)
                            throw new InvalidOperationException("No address argument was loaded");

                        int accumulator = Accumulator + Memory[LoadedMemoryAddressArgument.Value];

                        return this with { CycleAdvance = true, Accumulator = (byte)(accumulator & 0xFF), AluFlag = accumulator > byte.MaxValue };
                    }

                case Operation.SUB:
                    {
                        if (LoadedMemoryAddressArgument is null)
                            throw new InvalidOperationException("No address argument was loaded");

                        int accumulator = Accumulator - Memory[LoadedMemoryAddressArgument.Value];

                        return this with { CycleAdvance = true, Accumulator = (byte)(accumulator & 0xFF), AluFlag = accumulator < 0 };
                    }

                case Operation.BSL:
                    {
                        int accumulator = Accumulator << 1;

                        return this with { CycleAdvance = true, Accumulator = (byte)(accumulator & 0xFF), AluFlag = (Accumulator & 0x80) == 0x80 };
                    }

                case Operation.BSR:
                    {
                        int accumulator = Accumulator >> 1;

                        return this with { CycleAdvance = true, Accumulator = (byte)(accumulator & 0xFF), AluFlag = (Accumulator & 0x01) == 0x01 };
                    }

                case Operation.JMP:
                    {
                        if (LoadedMemoryAddressArgument is null)
                            throw new InvalidOperationException("No address argument was loaded");

                        return this with { CycleAdvance = true, ProgramCounter = LoadedMemoryAddressArgument.Value };
                    }

                case Operation.JPZ:
                    {
                        if (LoadedMemoryAddressArgument is null)
                            throw new InvalidOperationException("No address argument was loaded");

                        if (Accumulator == 0)
                            return this with { CycleAdvance = true, ProgramCounter = LoadedMemoryAddressArgument.Value };

                        return this with { CycleAdvance = true };
                    }

                case Operation.JPF:
                    {
                        if (LoadedMemoryAddressArgument is null)
                            throw new InvalidOperationException("No address argument was loaded");

                        if (AluFlag)
                            return this with { CycleAdvance = true, ProgramCounter = LoadedMemoryAddressArgument.Value };

                        return this with { CycleAdvance = true };
                    }

                case Operation.RIO:
                    {
                        if (LoadedMemoryAddressArgument is null)
                            throw new InvalidOperationException("No address argument was loaded");

                        ImmutableArray<byte> memory = Memory.SetItem(LoadedMemoryAddressArgument.Value, InputOutput);
                        return this with { CycleAdvance = true, Memory = memory };
                    }

                case Operation.WIO:
                    {
                        if (LoadedMemoryAddressArgument is null)
                            throw new InvalidOperationException("No address argument was loaded");

                        return this with { CycleAdvance = true, InputOutput = Memory[LoadedMemoryAddressArgument.Value] };
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(LoadedOpCode), LoadedOpCode, $"Invalid operation {LoadedOpCode}");
            }
        }

        private State IncrementProgramCounter()
        {
            if (LoadedOpCode is null)
                throw new InvalidOperationException("No op code was loaded");

            switch (LoadedOpCode)
            {
                case Operation.NOP:
                case Operation.BSL:
                case Operation.BSR:
                    {
                        if (ProgramCounter == byte.MaxValue)
                            return this with { CycleAdvance = true, LoadedOpCode = null, LoadedMemoryAddressArgument = null, Stopped = true };

                        return this with { CycleAdvance = true, ProgramCounter = (byte)(ProgramCounter + 1), LoadedOpCode = null, LoadedMemoryAddressArgument = null };
                    }

                case Operation.RAC:
                case Operation.WAC:
                case Operation.ADD:
                case Operation.SUB:
                case Operation.JPZ when Accumulator > 0:
                case Operation.JPF when !AluFlag:
                case Operation.RIO:
                case Operation.WIO:
                    {
                        if (ProgramCounter >= byte.MaxValue - 1)
                            return this with { CycleAdvance = true, LoadedOpCode = null, LoadedMemoryAddressArgument = null, Stopped = true };

                        return this with { CycleAdvance = true, ProgramCounter = (byte)(ProgramCounter + 2), LoadedOpCode = null, LoadedMemoryAddressArgument = null };
                    }

                case Operation.SWP:
                case Operation.JMP:
                case Operation.JPZ when Accumulator == 0:
                case Operation.JPF when AluFlag:
                    return this with { CycleAdvance = true, LoadedOpCode = null, LoadedMemoryAddressArgument = null };

                default:
                    throw new ArgumentOutOfRangeException(nameof(LoadedOpCode), LoadedOpCode, $"Invalid operation {LoadedOpCode}");
            }
        }

        /// <inheritdoc />
        public virtual bool Equals(State? other)
        {
            if (other is null)
                return false;

            if (ProgramCounter != other.ProgramCounter ||
                Accumulator != other.Accumulator ||
                AluFlag != other.AluFlag ||
                LoadedOpCode != other.LoadedOpCode ||
                LoadedMemoryAddressArgument != other.LoadedMemoryAddressArgument ||
                ProcessCycleStep != other.ProcessCycleStep ||
                Stopped != other.Stopped ||
                CycleAdvance != other.CycleAdvance ||
                Error != other.Error ||
                InputOutput != other.InputOutput)
                return false;

            return Enumerable.SequenceEqual(Memory, other.Memory);
        }
    }
}