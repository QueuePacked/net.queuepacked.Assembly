using System;
using System.Text;

namespace net.queuepacked.Assembly.Tipsy;

internal record ConsoleData(ConsoleColor ForegroundColor, ConsoleColor BackgroundColor, Encoding OutputEncoding, Encoding InputEncoding, bool TreatControlCAsInput)
{
    public static ConsoleData ReadCurrent()
    {
        return new ConsoleData
        (
            Console.ForegroundColor,
            Console.BackgroundColor,
            Console.OutputEncoding,
            Console.InputEncoding,
            Console.TreatControlCAsInput
        );
    }

    public void Apply()
    {
        Console.ForegroundColor = ForegroundColor;
        Console.BackgroundColor = BackgroundColor;
        Console.OutputEncoding = OutputEncoding;
        Console.InputEncoding = InputEncoding;
        Console.TreatControlCAsInput = TreatControlCAsInput;
    }
}