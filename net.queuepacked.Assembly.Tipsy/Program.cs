using System;
using System.IO;
using System.Linq;
using net.queuepacked.Assembly.CPU;

namespace net.queuepacked.Assembly.Tipsy
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length <= 0)
            {
                Console.WriteLine
                (
                    """
                    Tipsy - A fictional CPU simulator
                    
                    Usage
                        Tipsy <program-file> [-comp] [-output <output-file>] [-input <input-file>]
                    
                    Description
                        Tipsy is used as an educational tool for learning how computers work
                        and how programs turn to numbers.
                        It is not representative of any specific hardware.
                    
                    Options
                        -comp       Generate output files for each stage of 'compilation' when starting or reloading.
                                    The generated files are named based on the provided file.
                                    Existing files are overwritten.
                                    
                        -output     Whenever the I/O register triggers an 'OUT',
                                    the value will be appended as a new line to 'output-file'.
                                    
                        -input      Whenever the I/O register triggers an 'IN',
                                    a single line will be read from 'input-file', starting at the top.
                                    When all lines are read, reading will start again at the top.
                    
                    Press any key for next page...
                    """
                );

                Console.ReadKey(true);
                Console.WriteLine();

                Console.WriteLine("OP codes");
                foreach (Operation operation in Enum.GetValues<Operation>().Order())
                {
                    (string name, string description) = OperationDescriptions.GetOperationDetails(operation);
                    Console.WriteLine($"\t{(int)operation:00}|{operation:F} - {name}");
                    string indented = "\t  " + description.ReplaceLineEndings(Environment.NewLine + "\t  ");
                    Console.WriteLine(indented);
                    Console.WriteLine();
                }

                Console.WriteLine();
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);

                Console.WriteLine
                (
                    """
                    
                    Assembly
                        Operations can be written as a decimal number or a 3 letter short:
                            ADD     or      4
                            10              10
                            
                        If an operations requires an address, it can be written on the same line, separated by a single space:
                            ADD     or      ADD 10
                            10
                        
                        /   Start a comment until the end of the line.
                        #   Reference a named address, e.g. '#sum' or '#add'.
                        :   Add a name to an address, e.g. '0:sum' or 'ADD:add'.
                            A line with a name but without value defaults to 0, e.g. ':i' is treated as '0:i'.
                        
                        Example program:
                            /Code
                            WAC #a
                            ADD #b
                            RAC #sum
                            /Values
                            5:a
                            10:b
                            :sum

                    Press any key for next page...
                    """
                );


                Console.ReadKey(true);

                Console.WriteLine
                (
                    """
                    
                    Higher level syntax
                                    
                        | define <name=value>|<name> [, <name=value>|<name>]
                                    
                        Define generates a block of named values. If only a name is provided, they default to 0.
                        Example:
                            define a, b=1, c
                                
                                
                        | set <named address> to <named address>|<number> [(+|-|<|>) <named address>|<number>]
                       
                        Changes the value of a specific address.
                        The new value can be copied from another address or a constant.
                        Optionally the new value can be achieved through an operation involving a third address or constant.
                        Examples:
                            set a to b
                            set a to b < 2
                            set a to a + b
                        
                        
                        | skip if|unless flag|(<named address> is zero)
                        |    [<code>]
                        | end of skip
                                    
                        A skip block can either execute or skip a section of code.
                        The condition can be inverted and depends on a value being equal to 0 or the AC flag being set.
                        Example:
                            skip if a is zero
                                set b to a < 1
                            end of skip
                        
                        
                        | repeat until|while flag|(<named address> is zero)
                        |    [<code>]
                        | end of repeat
                        
                        A repeat block can repeat a section of code.
                        The condition can be inverted and depends on a value being equal to 0 or the AC flag being set.
                        Example:
                            repeat until flag
                                set b to b - c
                            end of repeat
                        
                        
                        | function <name>
                        |     [<code>]
                        | end of function
                           
                        A block of code that is given a name to 'call' it repeatedly from different locations in the program.
                        Example:
                            function shiftAdd
                                set a to a < 1
                                set a to a + 1
                            end of function
                        
                        
                        | call <name>
                                    
                        Jumps to the location of a function which will jump back to this location when finished.
                        Example:
                            5:a
                            call shiftAdd
                    """
                );

                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                return;
            }

            string fileName = args[0];

            if (!File.Exists(fileName))
            {
                Console.WriteLine($"File '{fileName}' not found");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                return;
            }

            bool generateOutput = false;
            string? outputFile = null;
            string? inputFile = null;
            for (int i = 1; i < args.Length; ++i)
            {
                switch (args[i])
                {
                    case "-comp":
                        generateOutput = true;
                        break;

                    case "-output" when i < args.Length - 1:
                        outputFile = args[++i];
                        break;

                    case "-input" when i < args.Length - 1:
                        inputFile = args[++i];
                        break;
                }
            }

            ProgramLoader programLoader = new(fileName, generateOutput);

            if (!programLoader.LoadProgram(out string? error))
            {
                Console.WriteLine(error);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                return;
            }

            ConsoleData currentConsoleData = ConsoleData.ReadCurrent();

            try
            {
                Console.CursorVisible = false;
                Console.Title = "Tipsy";

                Window window = new();
                window.Render(true);

                StateRenderer stateRenderer = new(window);

                StateMachine stateMachine = new(programLoader.Program, outputFile, inputFile);

                ConsoleKeyInfo input;
                do
                {
                    stateRenderer.Render(stateMachine.State, stateMachine.CpuTickChanges);

                    input = Console.ReadKey(true);

                    //intercept input
                    if (stateMachine.ReadsIn)
                    {
                        int inputValue = -1;
                        if (input.KeyChar is >= '0' and <= '9')
                            inputValue = input.KeyChar - '0';
                        else if (input.KeyChar is >= 'a' and <= 'f')
                            inputValue = input.KeyChar - 'a' + 10;

                        if (inputValue >= 0)
                        {
                            int multiplier = stateRenderer.NumberFormat switch
                            {
                                StateRenderer.Format.Binary => 2,
                                StateRenderer.Format.Decimal => 10,
                                StateRenderer.Format.Hexadecimal => 16,
                                _ => throw new ArgumentOutOfRangeException()
                            };

                            int mod = stateRenderer.NumberFormat switch
                            {
                                StateRenderer.Format.Binary => 256,
                                StateRenderer.Format.Decimal => 1000,
                                StateRenderer.Format.Hexadecimal => 256,
                                _ => throw new ArgumentOutOfRangeException()
                            };

                            int newInputValue = (stateMachine.State.InputOutput * multiplier + inputValue) % mod;

                            if (stateRenderer.NumberFormat == StateRenderer.Format.Decimal)
                                while (newInputValue > 255)
                                    newInputValue -= 100;

                            stateMachine.UpdateInputOutput((byte)newInputValue);
                        }
                    }

                    switch (input.Key)
                    {
                        case ConsoleKey.LeftArrow:
                            stateMachine.StepBackwards();
                            break;
                        case ConsoleKey.RightArrow:
                            stateMachine.StepForward();
                            break;

                        case ConsoleKey.UpArrow:
                            stateMachine.InstructionForward();
                            break;
                        case ConsoleKey.DownArrow:
                            stateMachine.InstructionBackwards();
                            break;

                        case ConsoleKey.N:
                            stateRenderer.NumberFormat = stateRenderer.NumberFormat switch
                            {
                                StateRenderer.Format.Decimal => StateRenderer.Format.Binary,
                                StateRenderer.Format.Binary => StateRenderer.Format.Hexadecimal,
                                _ => StateRenderer.Format.Decimal
                            };
                            break;

                        case ConsoleKey.R:
                            stateMachine.Reset();
                            break;

                        case ConsoleKey.L:
                            {
                                if (!programLoader.LoadProgram(out error))
                                    stateRenderer.ShowError(error);
                                else
                                    stateMachine.LoadNewProgram(programLoader.Program);

                                break;
                            }
                    }
                }
                while (input.Key != ConsoleKey.Escape);
            }
            finally
            {
                currentConsoleData.Apply();
            }
        }
    }
}
