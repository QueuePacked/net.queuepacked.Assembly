using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace net.queuepacked.Assembly.Tipsy
{
    public class Window
    {
        private readonly Lock _dataLock;
        private readonly Lock _bufferLock;

        private readonly byte[] _colors;
        private readonly char[] _characters;

        private readonly byte[] _colorBuffer;
        private readonly char[] _charactersBuffer;

        public int Width { get; }
        public int Height { get; }

        private readonly int _bufferWidth;

        public bool Changed { get; private set; }

        private readonly Encoding _encoding;

        public Window()
        {
            Console.OutputEncoding = Encoding.UTF8;//Attempt to force an encoding
            _encoding = Console.OutputEncoding;//But use the one the console then uses, even if it is not the one we wanted

            Width = Console.WindowWidth;
            Height = Console.WindowHeight - 1;

            _bufferWidth = Console.BufferWidth;

            _dataLock = new Lock();
            _bufferLock = new Lock();

            _characters = new char[_bufferWidth * Height];
            Array.Fill(_characters, ' ');
            _colors = new byte[_characters.Length];

            _charactersBuffer = new char[_characters.Length];
            _colorBuffer = new byte[_charactersBuffer.Length];

            Changed = false;
        }

        private static byte CombineColor(ConsoleColor foregroundColor, ConsoleColor backgroundColor)
            => (byte)((int)foregroundColor << 4 | (int)backgroundColor);

        private static (ConsoleColor foregroundColor, ConsoleColor backgroundColor) SplitColor(byte combined)
            => ((ConsoleColor)(combined >> 4), (ConsoleColor)(combined & 0x0f));

        public void Fill(char fillerCharacter, ConsoleColor foregroundColor, ConsoleColor backgroundColor)
        {
            byte colorValue = CombineColor(foregroundColor, backgroundColor);

            lock (_dataLock)
            {
                for (int index = _characters.Length - 1; index >= 0; --index)
                {
                    if (_characters[index] != fillerCharacter)
                    {
                        Changed = true;
                        _characters[index] = fillerCharacter;
                    }

                    if (_colors[index] != colorValue)
                    {
                        Changed = true;
                        _colors[index] = colorValue;
                    }
                }
            }
        }

        public void Write(int left, int top, char character, ConsoleColor foregroundColor, ConsoleColor backgroundColor)
            => Write(left, top, [character], foregroundColor, backgroundColor);

        public void Write(int left, int top, IEnumerable<char> characters, ConsoleColor foregroundColor, ConsoleColor backgroundColor)
        {
            if (left < 0 || left >= Width)
                throw new ArgumentOutOfRangeException(nameof(left), left, $"Must be at least 0 and at most {Width - 1}");
            if (top < 0 || top >= Height)
                throw new ArgumentOutOfRangeException(nameof(top), top, $"Must be at least 0 and at most {Height - 1}");

            int index = top * _bufferWidth + left;
            byte colorValue = CombineColor(foregroundColor, backgroundColor);

            lock (_dataLock)
            {
                foreach (char codeUnit in characters)
                {
                    if (_characters[index] != codeUnit)
                    {
                        Changed = true;
                        _characters[index] = codeUnit;
                    }

                    if (_colors[index] != colorValue)
                    {
                        Changed = true;
                        _colors[index] = colorValue;
                    }

                    ++index;
                }
            }
        }

        public void Render(bool force)
        {
            lock (_bufferLock)
            {
                lock (_dataLock)
                {
                    if (!(Changed || force))
                        return;

                    Array.Copy(_characters, _charactersBuffer, _characters.Length);
                    Array.Copy(_colors, _colorBuffer, _colors.Length);
                    Changed = false;
                }

                Console.CursorLeft = 0;
                Console.CursorTop = 0;
                byte previousColor = _colorBuffer[0];
                int j;
                int count;

                using (Stream output = Console.OpenStandardOutput())
                {
                    for (int i = 0; i < _charactersBuffer.Length; ++i)
                    {
                        j = i + 1;
                        count = 1;
                        while (j < _charactersBuffer.Length && _colorBuffer[j] == previousColor)
                        {
                            ++count;
                            ++j;
                        }

                        (Console.ForegroundColor, Console.BackgroundColor) = SplitColor(previousColor);

                        byte[] buffer = _encoding.GetBytes(_charactersBuffer, i, count);
                        output.Write(buffer);
                        i = j - 1;

                        if (j >= _charactersBuffer.Length)
                            continue;

                        previousColor = _colorBuffer[j];
                    }
                }
            }
        }
    }
}