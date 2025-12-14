using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using net.queuepacked.Assembly.CPU;

namespace net.queuepacked.Assembly.Tipsy
{
    internal class StateMachine
    {
        private readonly string? _outputFile;
        private readonly string? _inputFile;
        private int _inputLineCounter;

        private Cpu _cpu;

        public int CpuTickChanges => _cpu.TickChanges;

        public State State => _cpu.State;

        public bool ReadsIn => _cpu.State is { LoadedOpCode: Operation.RIO, ProcessCycleStep: Step.ProcessOperation, CycleAdvance: false };

        public bool WritesOut => _cpu.State is { LoadedOpCode: Operation.WIO, ProcessCycleStep: Step.ProcessOperation, CycleAdvance: true };

        public StateMachine(ImmutableArray<byte> machineCode, string? outputFile, string? inputFile)
        {
            _outputFile = outputFile;
            _inputFile = inputFile;
            _inputLineCounter = 0;
            _cpu = new Cpu(machineCode);
        }

        public void LoadNewProgram(IEnumerable<byte> machineCode)
        {
            _cpu = new Cpu(machineCode);
            _inputLineCounter = 0;
        }

        public void Reset()
        {
            _cpu.Reset();

            _inputLineCounter = 0;
        }

        public void StepForward()
        {
            _cpu.Forward();

            if (WritesOut)
                WriteOutputLine($"{_cpu.State.InputOutput:D3} | {_cpu.State.InputOutput:X2} | {_cpu.State.InputOutput:B8}");

            if (ReadsIn && TryReadInputLine(out byte input))
                _cpu.UpdateInputOutput(input);
        }

        public void StepBackwards() => _cpu.Back();

        public void UpdateInputOutput(byte value) => _cpu.UpdateInputOutput(value);

        public void InstructionForward()
        {
            int count = 0;
            do
            {
                if (++count > 8)
                    break;

                StepForward();

                if (ReadsIn || WritesOut)
                    break;
            }
            while (_cpu.State.ProcessCycleStep != Step.IncrementProgramCounter || !_cpu.State.CycleAdvance);
        }

        public void InstructionBackwards()
        {
            int count = 0;
            do
            {
                if (++count > 8)
                    break;

                _cpu.Back();

                if (ReadsIn || WritesOut)
                    break;
            }
            while (_cpu.State.ProcessCycleStep != Step.ReadOpCode || _cpu.State.CycleAdvance);
        }

        private void WriteOutputLine(string line)
        {
            if (_outputFile is null)
                return;

            try
            {
                using StreamWriter streamWriter = File.AppendText(_outputFile);
                streamWriter.WriteLine(line);
            }
            catch
            {
                // ignored
            }
        }

        private bool TryReadInputLine(out byte value)
        {
            value = 0;
            if (!File.Exists(_inputFile))
                return false;
            try
            {
                int lineCount = -1;
                string? firstLine = null;
                string? targetLine = null;
                foreach (string line in File.ReadLines(_inputFile))
                {
                    ++lineCount;

                    if (lineCount == 0)
                        firstLine = line;

                    if (lineCount >= _inputLineCounter)
                    {
                        targetLine = line;
                        break;
                    }
                }

                if (targetLine is null)
                {
                    _inputLineCounter = 0;
                    targetLine = firstLine;
                }

                ++_inputLineCounter;

                return byte.TryParse(targetLine, out value)
                       || byte.TryParse(targetLine, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value)
                       || byte.TryParse(targetLine, NumberStyles.BinaryNumber, CultureInfo.InvariantCulture, out value);
            }
            catch
            {
                return false;
            }
        }
    }
}