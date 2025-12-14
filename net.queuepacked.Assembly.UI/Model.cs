using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using net.queuepacked.Assembly.CPU;

namespace net.queuepacked.Assembly.UI
{
    public class Model : INotifyPropertyChanged
    {
        private Cpu _cpu;
        private State _state;
        private string _description;
        private string _name;
        private MemoryItem[] _memoryItems;
        private string _code;
        private int _tickChanges;
        private string[] _markers;
        private int _instructionCount;
        private byte _inputOutput;

        public State State
        {
            get => _state;
            private set
            {
                if (Equals(value, _state)) return;
                _state = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ReadsIn));
                OnPropertyChanged(nameof(DoesNotRead));
                OnPropertyChanged(nameof(WritesOut));

                UpdatedState();
            }
        }

        public string Description
        {
            get => _description;
            private set
            {
                if (value == _description) return;
                _description = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            private set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public MemoryItem[] MemoryItems
        {
            get => _memoryItems;
            private set
            {
                if (Equals(value, _memoryItems)) return;
                _memoryItems = value;
                OnPropertyChanged();
            }
        }

        public string Code
        {
            get => _code;
            set
            {
                if (value == _code) return;
                _code = value;

                CodeSegments.Clear();
                StringReader stringReader = new(_code);
                StringBuilder stringBuilder = new();
                int count = 0;
                while (stringReader.ReadLine() is string line)
                {
                    stringBuilder.AppendLine(line);

                    if (++count >= 12)
                    {
                        CodeSegments.Add(stringBuilder.ToString());
                        stringBuilder.Clear();
                        count = 0;
                    }
                }
                if (count > 0)
                    CodeSegments.Add(stringBuilder.ToString());

                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> CodeSegments { get; } = new();

        public Cpu Cpu
        {
            get => _cpu;
            set
            {
                if (Equals(value, _cpu)) return;
                _cpu = value;
                OnPropertyChanged();
            }
        }

        public int TickChanges
        {
            get => _tickChanges;
            set
            {
                if (value == _tickChanges) return;
                _tickChanges = value;
                OnPropertyChanged();
            }
        }

        public int InstructionCount
        {
            get => _instructionCount;
            set
            {
                if (value == _instructionCount) return;
                _instructionCount = value;
                OnPropertyChanged();
            }
        }

        public byte InputOutput
        {
            get => _inputOutput;
            set
            {
                if (value == _inputOutput) return;
                _inputOutput = value;
                Cpu.UpdateInputOutput(value);
                OnPropertyChanged();
            }
        }

        public bool DoesNotRead => !ReadsIn;
        public bool ReadsIn => State is { LoadedOpCode: Operation.RIO, ProcessCycleStep: Step.ProcessOperation, CycleAdvance: false };
        public bool WritesOut => State is { LoadedOpCode: Operation.WIO, ProcessCycleStep: Step.ProcessOperation, CycleAdvance: true };

        public CpuComponentVisibilities ComponentVisibilities { get; }

        public Model(Cpu cpu)
        {
            _cpu = cpu;
            _state = State.Empty;
            _description = string.Empty;
            _name = string.Empty;
            _memoryItems = [];
            _markers = [];

            _code = string.Empty;
            ComponentVisibilities = new CpuComponentVisibilities();
        }

        public void LoadNewProgram(IEnumerable<byte> byteCode, string[] markers, string code)
        {
            Cpu = new Cpu(byteCode);
            State = Cpu.State;
            _markers = markers;
            UpdatedState();
        }

        public void Forward()
        {
            Cpu.Forward();
            State = Cpu.State;
        }

        public void Backward()
        {
            Cpu.Back();
            State = Cpu.State;
        }

        private void UpdatedState()
        {
            UpdateMemory();

            if (Cpu.State.LoadedOpCode is Operation operation)
            {
                (string name, string description) = OperationDescriptions.GetOperationDetails(operation);
                Name = name;
                Description = description;
            }
            else
            {
                Name = string.Empty;
                Description = string.Empty;
            }

            TickChanges = Cpu.TickChanges;
            InstructionCount = (Cpu.TickChanges + 1) / 8;//1 Instruction = 4 ticks = 8 tick changes
            InputOutput = State.InputOutput;
        }

        private void UpdateMemory()
        {
            const int range = 10;

            int windowStart = State.ProgramCounter - range;
            int windowEnd = State.ProgramCounter + range;
            if (windowStart < 0)
            {
                windowEnd -= windowStart;
                windowStart = 0;
            }
            else if (windowEnd >= State.Memory.Length)
            {
                windowStart -= windowEnd - State.Memory.Length + 1;
                windowEnd = State.Memory.Length - 1;
            }

            MemoryItem[] memoryItems = new MemoryItem[range * 2 + 1 + (windowStart > 0 ? 1 : 0) + (windowEnd < State.Memory.Length - 1 ? 1 : 0)];
            int itemsIndex = 0;
            if (windowStart > 0)
            {
                memoryItems[0] = new MemoryItem(false, 0, 0, false, true, false, string.Empty, ComponentVisibilities);
                itemsIndex = 1;
            }
            if (windowEnd <= State.Memory.Length)
                memoryItems[^1] = new MemoryItem(false, 0, 0, false, true, false, string.Empty, ComponentVisibilities);

            for (int i = windowStart; i <= windowEnd && i < State.Memory.Length; ++i, ++itemsIndex)
            {
                bool offset = (State.ProcessCycleStep != Step.IncrementProgramCounter && State is not { CycleAdvance: true, ProcessCycleStep: Step.ProcessOperation })
                              && (i == State.ProgramCounter + 1 && State.LoadedMemoryAddressArgument.HasValue
                                  || i == State.ProgramCounter && State.LoadedOpCode.HasValue);

                string marker = i >= _markers.Length ? string.Empty : _markers[i];

                memoryItems[itemsIndex] = new MemoryItem(true, State.Memory[i], (byte)i, i == State.ProgramCounter, false, offset, marker, ComponentVisibilities);
            }

            MemoryItems = memoryItems;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}