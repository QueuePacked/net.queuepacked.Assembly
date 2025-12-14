using System;
using System.Collections.Generic;
using System.IO;
using net.queuepacked.Assembly.CPU;

namespace net.queuepacked.Assembly.Tipsy
{
    internal class StateRenderer
    {
        public enum Format
        {
            Binary,
            Decimal,
            Hexadecimal,
        }

        private readonly Window _window;

        public Format NumberFormat { get; set; }

        private string? _oneTimeErrorOverwrite;

        private int NumberLength => NumberFormat switch
        {
            Format.Binary => 8,
            Format.Decimal => 3,
            Format.Hexadecimal => 2,
            _ => throw new ArgumentOutOfRangeException()
        };

        private string NumberFormatString => NumberFormat switch
        {
            Format.Binary => "B8",
            Format.Decimal => "D3",
            Format.Hexadecimal => "X2",
            _ => throw new ArgumentOutOfRangeException()
        };

        public StateRenderer(Window window)
        {
            _window = window;
            NumberFormat = Format.Decimal;
        }

        public void ShowError(string error)
        {
            _oneTimeErrorOverwrite = error;
        }

        public void Render(State state, int tickChanges)
        {
            _window.Fill(' ', ConsoleColor.White, ConsoleColor.Black);

            DrawSteps(state);
            DrawMemory(state);
            DrawRegisters(state);
            DrawStats(state, tickChanges);
            DrawOperationInfo(state);
            DrawErrorAndStop(state);

            DrawLegend();

            _window.Render(false);
        }

        private void DrawSteps(State state)
        {
            const int height = 1;

            string readOp = " Read operation ";
            string readArg = " Read argument ";
            string processOp = " Process operation ";
            string incPc = " Increment PC ";

            int totalTextLength = readOp.Length + readArg.Length + processOp.Length + incPc.Length;

            int space = (_window.Width - totalTextLength) / 5;

            ConsoleColor fgSelected = ConsoleColor.Black;
            ConsoleColor fgUnselected = ConsoleColor.Black;
            ConsoleColor bgSelected = ConsoleColor.Gray;
            ConsoleColor bgUnselected = ConsoleColor.DarkGray;

            int left = space;
            _window.Write(space / 2, height, '>', state.ProcessCycleStep == Step.ReadOpCode && !state.CycleAdvance ? ConsoleColor.White : ConsoleColor.Black, ConsoleColor.Black);
            _window.Write(left, height, readOp, state.ProcessCycleStep == Step.ReadOpCode ? fgSelected : fgUnselected, state.ProcessCycleStep == Step.ReadOpCode ? bgSelected : bgUnselected);
            left += readOp.Length;
            _window.Write(left + space / 2, height, '>', state.ProcessCycleStep == Step.ReadOptionalArgument && !state.CycleAdvance ? ConsoleColor.White : ConsoleColor.Black, ConsoleColor.Black);
            left += space;
            _window.Write(left, height, readArg, state.ProcessCycleStep == Step.ReadOptionalArgument ? fgSelected : fgUnselected, state.ProcessCycleStep == Step.ReadOptionalArgument ? bgSelected : bgUnselected);
            left += readArg.Length;
            _window.Write(left + space / 2, height, '>', state.ProcessCycleStep == Step.ProcessOperation && !state.CycleAdvance ? ConsoleColor.White : ConsoleColor.Black, ConsoleColor.Black);
            left += space;
            _window.Write(left, height, processOp, state.ProcessCycleStep == Step.ProcessOperation ? fgSelected : fgUnselected, state.ProcessCycleStep == Step.ProcessOperation ? bgSelected : bgUnselected);
            left += processOp.Length;
            _window.Write(left + space / 2, height, '>', state.ProcessCycleStep == Step.IncrementProgramCounter && !state.CycleAdvance ? ConsoleColor.White : ConsoleColor.Black, ConsoleColor.Black);
            left += space;
            _window.Write(left, height, incPc, state.ProcessCycleStep == Step.IncrementProgramCounter ? fgSelected : fgUnselected, state.ProcessCycleStep == Step.IncrementProgramCounter ? bgSelected : bgUnselected);
            left += incPc.Length;
            _window.Write(left + space / 2, height, '>', state.ProcessCycleStep == Step.ReadOpCode && !state.CycleAdvance ? ConsoleColor.White : ConsoleColor.Black, ConsoleColor.Black);
        }

        private void DrawMemory(State state)
        {
            const int height = 3;

            int cellLength = NumberLength + 1;
            int cellCount = (_window.Width - 1) / cellLength;
            int totalSpace = _window.Width - (cellCount * cellLength + 1);
            int spaceLeft = totalSpace / 2;

            int cellCountCenter = cellCount / 2;
            bool leftmostVisible = false;
            bool rightmostVisible = false;
            int pcCellNumber;
            if (state.ProgramCounter < cellCountCenter)
            {
                leftmostVisible = true;
                pcCellNumber = state.ProgramCounter;
            }
            else if (state.Memory.Length - state.ProgramCounter - 1 < cellCount - cellCountCenter)
            {
                rightmostVisible = true;
                pcCellNumber = cellCount - (state.Memory.Length - state.ProgramCounter - 1) - 1;
            }
            else
                pcCellNumber = cellCountCenter;

            int memoryStartAddress = state.ProgramCounter - pcCellNumber;


            char[] topLine = new char[_window.Width - totalSpace];
            char[] centerLine = new char[_window.Width - totalSpace];
            char[] bottomLine = new char[_window.Width - totalSpace];
            char[] addressLine = new char[_window.Width - totalSpace];

            topLine[0] = leftmostVisible ? '┏' : '┳';
            centerLine[0] = '┃';
            bottomLine[0] = leftmostVisible ? '┗' : '┻';
            addressLine[0] = '╵';

            for (int cellNumber = 0; cellNumber < cellCount; ++cellNumber)
            {
                int cellStartIndex = cellNumber * cellLength;
                for (int i = 1; i < cellLength; ++i)
                {
                    topLine[cellStartIndex + i] = '━';
                    centerLine[cellStartIndex + i] = ' ';
                    bottomLine[cellStartIndex + i] = '━';
                    addressLine[cellStartIndex + i] = ' ';
                }

                if (cellNumber < cellCount - 1)
                {
                    topLine[cellStartIndex + cellLength] = '┳';
                    centerLine[cellStartIndex + cellLength] = '┃';
                    bottomLine[cellStartIndex + cellLength] = '┻';
                    addressLine[cellStartIndex + cellLength] = '╵';
                }
            }

            topLine[^1] = rightmostVisible ? '┓' : '┳';
            centerLine[^1] = '┃';
            bottomLine[^1] = rightmostVisible ? '┛' : '┻';
            addressLine[^1] = '╵';

            _window.Write(spaceLeft, height, topLine, ConsoleColor.DarkGray, ConsoleColor.Black);
            _window.Write(spaceLeft, height + 1, centerLine, ConsoleColor.DarkGray, ConsoleColor.Black);
            _window.Write(spaceLeft, height + 2, bottomLine, ConsoleColor.DarkGray, ConsoleColor.Black);
            _window.Write(spaceLeft, height + 3, addressLine, ConsoleColor.DarkGray, ConsoleColor.Black);


            int pcCellLeft = pcCellNumber * cellLength + spaceLeft;
            _window.Write(pcCellLeft, height, pcCellLeft > spaceLeft ? '╦' : '╔', ConsoleColor.White, ConsoleColor.Black);
            _window.Write(pcCellLeft, height + 1, '║', ConsoleColor.White, ConsoleColor.Black);
            _window.Write(pcCellLeft, height + 2, pcCellLeft > spaceLeft ? '╩' : '╚', ConsoleColor.White, ConsoleColor.Black);

            int pcCellMarkerLength = state.LoadedMemoryAddressArgument.HasValue ? cellLength * 2 : cellLength;
            for (int i = 1; i < pcCellMarkerLength; ++i)
            {
                int index = pcCellLeft + i;
                _window.Write(index, height, '═', ConsoleColor.White, ConsoleColor.Black);
                _window.Write(index, height + 2, '═', ConsoleColor.White, ConsoleColor.Black);
            }
            pcCellLeft += pcCellMarkerLength;
            _window.Write(pcCellLeft, height, pcCellLeft < topLine.Length - 1 ? '╦' : '╗', ConsoleColor.White, ConsoleColor.Black);
            _window.Write(pcCellLeft, height + 1, '║', ConsoleColor.White, ConsoleColor.Black);
            _window.Write(pcCellLeft, height + 2, pcCellLeft < topLine.Length - 1 ? '╩' : '╝', ConsoleColor.White, ConsoleColor.Black);


            for (int cellNumber = 0; cellNumber < cellCount; ++cellNumber)
            {
                int index = spaceLeft + cellNumber * cellLength + 1;

                string memory = state.Memory[memoryStartAddress + cellNumber].ToString(NumberFormatString);
                string address = (memoryStartAddress + cellNumber).ToString(NumberFormatString);

                _window.Write(index, height + 1, memory, ConsoleColor.White, ConsoleColor.Black);
                _window.Write(index, height + 3, address, ConsoleColor.White, ConsoleColor.Black);
            }
        }

        private void DrawRegisters(State state)
        {
            const int height = 8;

            int drawHeight = height;
            int drawLeft = 2;

            string namePc = "PC";
            string nameAc = "AC";

            int textLength = 8;

            int registerInternalSpace = NumberLength;

            DrawBox(drawLeft, drawHeight, 2 + textLength + registerInternalSpace + 2, 5, ConsoleColor.DarkGray, ConsoleColor.Black);
            _window.Write(drawLeft + textLength - namePc.Length, drawHeight + 2, namePc, ConsoleColor.White, ConsoleColor.Black);
            _window.Write(drawLeft + textLength + 1, drawHeight + 2, state.ProgramCounter.ToString(NumberFormatString), ConsoleColor.White, ConsoleColor.Black);
            DrawBox(drawLeft + textLength, drawHeight + 1, 2 + registerInternalSpace, 3, ConsoleColor.Gray, ConsoleColor.Black);

            drawHeight += 5;
            DrawBox(drawLeft, drawHeight, 2 + textLength + registerInternalSpace + 2, 5, ConsoleColor.DarkGray, ConsoleColor.Black);
            _window.Write(drawLeft + 2, drawHeight + 2, nameAc, ConsoleColor.White, ConsoleColor.Black);
            _window.Write(drawLeft + textLength + 1, drawHeight + 2, state.Accumulator.ToString(NumberFormatString), ConsoleColor.White, ConsoleColor.Black);
            DrawBox(drawLeft + textLength, drawHeight + 1, 2 + registerInternalSpace, 3, ConsoleColor.Gray, ConsoleColor.Black);
            _window.Write(drawLeft + 5, drawHeight + 2, "[ ]", ConsoleColor.White, ConsoleColor.Black);
            if (state.AluFlag)
                _window.Write(drawLeft + 6, drawHeight + 2, 'F', ConsoleColor.Black, ConsoleColor.Gray);

            bool readsIn = state is { LoadedOpCode: Operation.RIO, ProcessCycleStep: Step.ProcessOperation, CycleAdvance: false };
            bool writesOut = state is { LoadedOpCode: Operation.WIO, ProcessCycleStep: Step.ProcessOperation, CycleAdvance: true };

            drawHeight += 5;
            DrawBox(drawLeft, drawHeight, 2 + textLength + registerInternalSpace + 2, 5, ConsoleColor.DarkGray, ConsoleColor.Black);
            _window.Write(drawLeft + textLength - 6, drawHeight + 2, "IN", readsIn ? ConsoleColor.Black : ConsoleColor.White, readsIn ? ConsoleColor.Gray : ConsoleColor.Black);
            _window.Write(drawLeft + textLength - 4, drawHeight + 2, '/', ConsoleColor.White, ConsoleColor.Black);
            _window.Write(drawLeft + textLength - 3, drawHeight + 2, "OUT", writesOut ? ConsoleColor.Black : ConsoleColor.White, writesOut ? ConsoleColor.Gray : ConsoleColor.Black);
            _window.Write(drawLeft + textLength + 1, drawHeight + 2, state.InputOutput.ToString(NumberFormatString), readsIn || writesOut ? ConsoleColor.Black : ConsoleColor.White, readsIn || writesOut ? ConsoleColor.Gray : ConsoleColor.Black);
            DrawBox(drawLeft + textLength, drawHeight + 1, 2 + registerInternalSpace, 3, ConsoleColor.Gray, ConsoleColor.Black);
        }

        private void DrawStats(State state, int tickChanges)
        {
            const int height = 9;

            string loadedOperationLabel = "Current operation";
            string loadedAddressLabel = "Operation address";
            string instructionCountLabel = "Instruction count";
            string tickCountLabel = "CPU ticks";
            int instructionCount = (tickChanges + 1) / 8;//1 Instruction = 4 ticks = 8 tick changes
            int tickCount = (tickChanges + 1) / 2;

            int right = loadedOperationLabel.Length;
            if (right < loadedAddressLabel.Length)
                right = loadedAddressLabel.Length;
            if (right < instructionCountLabel.Length)
                right = instructionCountLabel.Length;
            if (right < tickCountLabel.Length)
                right = tickCountLabel.Length;
            right += 5;
            int left = _window.Width - right;

            ConsoleColor labelFg = ConsoleColor.White;
            ConsoleColor labelBg = ConsoleColor.Black;
            ConsoleColor numberFg = ConsoleColor.Black;
            ConsoleColor numberBg = ConsoleColor.Gray;

            string? loadedOperationNumber = state.LoadedOpCode?.ToString();
            string? loadedAddressNumber = state.LoadedMemoryAddressArgument?.ToString(NumberFormatString);
            string instructionCountNumber = instructionCount.ToString(NumberFormatString);
            string tickCountNumber = tickCount.ToString(NumberFormatString);

            int drawHeight = height;
            _window.Write(left + (right - loadedOperationLabel.Length) / 2, drawHeight, loadedOperationLabel, labelFg, labelBg);
            if (loadedOperationNumber is not null)
                _window.Write(left + (right - loadedOperationLabel.Length) / 2 + (loadedOperationLabel.Length - loadedOperationNumber.Length) / 2, drawHeight + 1, loadedOperationNumber, numberFg, numberBg);

            drawHeight += 4;
            _window.Write(left + (right - loadedAddressLabel.Length) / 2, drawHeight, loadedAddressLabel, labelFg, labelBg);
            if (loadedAddressNumber is not null)
                _window.Write(left + (right - loadedAddressLabel.Length) / 2 + (loadedAddressLabel.Length - loadedAddressNumber.Length) / 2, drawHeight + 1, loadedAddressNumber, numberFg, numberBg);

            drawHeight += 4;
            _window.Write(left + (right - instructionCountLabel.Length) / 2, drawHeight, instructionCountLabel, labelFg, labelBg);
            _window.Write(left + (right - instructionCountLabel.Length) / 2 + (instructionCountLabel.Length - instructionCountNumber.Length) / 2, drawHeight + 1, instructionCountNumber, numberFg, numberBg);

            drawHeight += 4;
            _window.Write(left + (right - tickCountLabel.Length) / 2, drawHeight, tickCountLabel, labelFg, labelBg);
            _window.Write(left + (right - tickCountLabel.Length) / 2 + (tickCountLabel.Length - tickCountNumber.Length) / 2, drawHeight + 1, tickCountNumber, numberFg, numberBg);
            _window.Write(left + (right - tickCountLabel.Length) / 2 + (tickCountLabel.Length - tickCountNumber.Length) / 2 + tickCountNumber.Length, drawHeight + 1, tickChanges % 2 == 0 ? '╻' : '╹', numberBg, numberFg);
        }

        private void DrawOperationInfo(State state)
        {
            const int height = 9;

            if (!state.LoadedOpCode.HasValue)
                return;

            int spaceRight = 20;
            int spaceLeft = 14 + NumberLength;
            int spaceForText = _window.Width - spaceLeft - spaceRight;

            (string operationName, string operationDescription) = OperationDescriptions.GetOperationDetails(state.LoadedOpCode.Value);

            operationName = " " + operationName + " ";

            int left = (spaceForText - operationName.Length) / 2 + spaceLeft;
            _window.Write(left, height, operationName, ConsoleColor.Black, ConsoleColor.White);


            int wrapRequiredAt = spaceForText - 4;

            List<string> lines = new();
            StringReader stringReader = new(operationDescription);
            while (stringReader.ReadLine() is string line)
            {
                if (line.Length <= wrapRequiredAt)
                {
                    lines.Add(line);
                    continue;
                }

                int wrapAfter = line.LastIndexOf(',', wrapRequiredAt);
                if (wrapAfter < 0)
                    wrapAfter = line.LastIndexOf(' ', wrapRequiredAt);
                wrapAfter++;

                lines.Add(line[..wrapAfter]);
                lines.Add(line.Substring(wrapAfter, line.Length - wrapAfter).TrimStart());
            }

            int maxWidth = 0;
            foreach (string line in lines)
                if (maxWidth < line.Length)
                    maxWidth = line.Length;

            int top = height + 1;
            left = (spaceForText - maxWidth) / 2 + spaceLeft;
            foreach (string line in lines)
                _window.Write(left, ++top, line, ConsoleColor.White, ConsoleColor.Black);
        }

        private void DrawErrorAndStop(State state)
        {
            const int height = 18;

            int spaceRight = 20;
            int spaceLeft = 14 + NumberLength;
            int spaceForText = _window.Width - spaceLeft - spaceRight;

            string error;
            if (_oneTimeErrorOverwrite is null)
                error = state.Error;
            else
            {
                error = _oneTimeErrorOverwrite;
                _oneTimeErrorOverwrite = null;
            }

            if (error.Length > 0)
                _window.Write((spaceForText - error.Length) / 2 + spaceLeft, height, error, ConsoleColor.Red, ConsoleColor.Black);

            if (!state.Stopped)
                return;

            string stoppedLabel = " CPU STOPPED ";
            _window.Write((spaceForText - stoppedLabel.Length) / 2 + spaceLeft, height + 3, stoppedLabel, ConsoleColor.Black, ConsoleColor.Red);
        }

        private void DrawLegend()
        {
            const int height = 25;

            ConsoleColor bright = ConsoleColor.Gray;
            ConsoleColor dark = ConsoleColor.Black;

            const int blockCount = 5;
            int space = 5;
            int length = (_window.Width - (blockCount + 1) * space) / blockCount;

            int left = space;
            _window.Write(left, height, "←", dark, bright);
            _window.Write(left + 2, height, "→", dark, bright);
            _window.Write(left + 4, height, "CPU clock", bright, dark);

            _window.Write(left, height + 2, "↑", dark, bright);
            _window.Write(left + 2, height + 2, "↓", dark, bright);
            _window.Write(left + 4, height + 2, "Instruction", bright, dark);

            left += space + length;
            _window.Write(left, height, "R", dark, bright);
            _window.Write(left + 2, height, "Reset program", bright, dark);

            _window.Write(left, height + 2, "L", dark, bright);
            _window.Write(left + 2, height + 2, "Reload program", bright, dark);

            left += space + length;
            _window.Write(left, height, "N", dark, bright);
            _window.Write(left + 2, height, "Cycle number format", bright, dark);

            _window.Write(left, height + 2, "0-9", dark, bright);
            _window.Write(left + 4, height + 2, "a-f", dark, bright);
            _window.Write(left + 8, height + 2, "Input value (RIO)", bright, dark);

            left += space + length * 2;
            _window.Write(left, height + 2, "Esc", dark, bright);
            _window.Write(left + 4, height + 2, "Exit", bright, dark);
        }

        private void DrawBox(int left, int top, int width, int height, ConsoleColor foregroundColor, ConsoleColor backgroundColor)
        {
            int right = left + width - 1;
            int bottom = top + height - 1;

            _window.Write(left, top, "┏", foregroundColor, backgroundColor);
            _window.Write(right, top, "┓", foregroundColor, backgroundColor);
            _window.Write(left, bottom, "┗", foregroundColor, backgroundColor);
            _window.Write(right, bottom, "┛", foregroundColor, backgroundColor);

            for (int i = left + width - 2; i > left; --i)
            {
                _window.Write(i, top, "━", foregroundColor, backgroundColor);
                _window.Write(i, bottom, "━", foregroundColor, backgroundColor);
            }
            for (int i = top + height - 2; i > top; --i)
            {
                _window.Write(left, i, "┃", foregroundColor, backgroundColor);
                _window.Write(right, i, "┃", foregroundColor, backgroundColor);
            }
        }
    }
}
