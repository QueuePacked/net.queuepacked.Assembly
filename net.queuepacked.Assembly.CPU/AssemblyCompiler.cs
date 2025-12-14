using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace net.queuepacked.Assembly.CPU
{
    public static partial class AssemblyCompiler
    {
        private static readonly ImmutableDictionary<string, Operation> OperationLookup;

        [GeneratedRegex(@"repeat (?<negate>until|while) (?:flag|#?(?<target>\w+) is zero)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex RepeatStartPattern();

        [GeneratedRegex(@"end of repeat", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex RepeatEndPattern();

        [GeneratedRegex(@"skip (?<negate>if|unless) (?:flag|#?(?<target>\w+) is zero)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex SkipStartPattern();

        [GeneratedRegex(@"end of skip", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex SkipEndPattern();

        [GeneratedRegex(@"set #?(?<target>\w+) to (?<source>#?[\w\d]+)(?: (?<operation>\+|-|<|>) (?<argument>#?[\w\d]+))?", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex SetValuePattern();

        [GeneratedRegex(@"define(?:[ ,]+(?<withvalue>\w+\=\d+)|[ ,]+(?<nameonly>\w+))+", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex DefinePattern();

        [GeneratedRegex(@"function :?(?<name>\w+)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex FuncStartPattern();

        [GeneratedRegex(@"call :?(?<name>\w+)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex FuncCallPattern();

        [GeneratedRegex(@"end of function", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex FuncEndPattern();

        static AssemblyCompiler()
        {
            ImmutableDictionary<string, Operation>.Builder builder = ImmutableDictionary.CreateBuilder<string, Operation>();
            foreach (Operation operation in Enum.GetValues<Operation>())
                builder.Add(operation.ToString("G").ToLowerInvariant(), operation);
            OperationLookup = builder.ToImmutable();
        }

        public static List<byte> CodesToProgram(string input)
        {
            StringReader stringReader = new(input);

            List<byte> program = [];
            int lineNumber = 0;
            while (stringReader.ReadLine() is string line)
            {
                ++lineNumber;

                if (!byte.TryParse(line, out byte value))
                    throw new Exception($"Cannot parse line {lineNumber} into byte value: '{line}'");

                program.Add(value);
            }

            return program;
        }

        public static string OperationsToCodes(string input)
        {
            StringReader stringReader = new(input);
            StringBuilder stringBuilder = new();

            int lineNumber = 0;
            while (stringReader.ReadLine() is string line)
            {
                ++lineNumber;
                if (lineNumber > 1)
                    stringBuilder.AppendLine();

                stringBuilder.Append(OperationLookup.TryGetValue(line.ToLowerInvariant(), out Operation operation) ? operation.ToString("D") : line);
            }

            return stringBuilder.ToString();
        }

        public static string ReferencesToAddresses(string input, out string[] markers)
        {
            StringReader stringReader = new(input);

            List<string> markerCollection = new();
            Dictionary<string, int> references = [];
            List<string> lines = [];
            while (stringReader.ReadLine() is string line)
            {
                int startOfLabel = line.IndexOf(':');

                if (startOfLabel < 0)
                {
                    lines.Add(line);
                    markerCollection.Add(string.Empty);
                    continue;
                }

                if (startOfLabel == line.Length - 1)
                    throw new Exception($"Invalid definition of reference in line {lines.Count + 1}: '{line}'");

                lines.Add(startOfLabel == 0 ? "0" : line[..startOfLabel]);

                string reference = line[(startOfLabel + 1)..];

                if (references.TryGetValue(reference, out int lineNumber))
                    throw new Exception($"Duplicate definition of reference '{reference}' in line {lineNumber} and {lines.Count}");

                references.Add(reference.ToLowerInvariant(), lines.Count - 1);
                markerCollection.Add(reference.ToLowerInvariant());
            }

            for (int i = 0; i < lines.Count; ++i)
            {
                string line = lines[i];

                if (line.Length < 2 || line[0] != '#')
                    continue;

                markerCollection[i] = line;

                if (!references.TryGetValue(line[1..].ToLowerInvariant(), out int lineNumber))
                    throw new Exception($"Unknown reference '{line}' in line {lines.Count}");

                lines[i] = lineNumber.ToString();
            }

            markers = markerCollection.ToArray();
            return string.Join(Environment.NewLine, lines);
        }

        public static string SingleLineToSequence(string input)
        {
            StringReader stringReader = new(input);
            StringBuilder stringBuilder = new();

            int lineNumber = 0;
            while (stringReader.ReadLine() is string line)
            {
                ++lineNumber;
                if (lineNumber > 1)
                    stringBuilder.AppendLine();

                int indexOf = line.IndexOf(' ');
                if (indexOf < 0)
                {
                    stringBuilder.Append(line);
                    continue;
                }

                stringBuilder.AppendLine(line[..indexOf]);
                stringBuilder.Append(line[(indexOf + 1)..].TrimStart(' '));
            }

            return stringBuilder.ToString();
        }

        public static string ExpandSyntax(string input)
        {
            StringReader stringReader = new(input);
            StringBuilder stringBuilder = new();

            const char startReference = '◚';
            const char endReference = '◛';
            const char negateReference = '◘';
            const char loopReferenceBase = '↺';
            const char skipReferenceBase = '↲';
            const char defineReferenceBase = '∹';
            const char constantReferenceBase = '∷';
            const char functionReferenceBase = 'ε';
            const char functionSkipBase = '⨋';
            const char functionPointerBase = '↸';
            const char constBase = '⨀';
            const char functionPointerReferenceBase = '⇲';

            int loopCounter = 0;
            int skipCounter = 0;
            int defineCounter = 0;
            Stack<string> repeatReferenceStack = new();
            Stack<string> skipReferenceStack = new();

            string? currentFunction = null;

            HashSet<int> constantsToAdd = new();

            bool firstLine = true;
            while (stringReader.ReadLine() is string line)
            {
                if (firstLine)
                    firstLine = false;
                else
                    stringBuilder.AppendLine();

                if (RepeatStartPattern().Match(line) is { Success: true } repeatStart)
                {
                    string target = repeatStart.Groups["target"].Value;
                    string negation = repeatStart.Groups["negate"].Value;

                    string start = string.Concat(loopReferenceBase, startReference, ++loopCounter);
                    string end = string.Concat(loopReferenceBase, endReference, loopCounter);
                    string negate = string.Concat(loopReferenceBase, negateReference, loopCounter);

                    if (target.Length > 0)
                    {
                        stringBuilder.Append(Operation.WAC).Append(':').Append(start).Append(' ');
                        if (target[0] != '#')
                            stringBuilder.Append('#');
                        stringBuilder.AppendLine(target);

                        if (negation == "while")
                        {
                            stringBuilder.Append(Operation.JPZ).Append(" #").AppendLine(negate);
                            stringBuilder.Append(Operation.JMP).Append(" #").AppendLine(end);
                            stringBuilder.Append(Operation.NOP).Append(':').Append(negate);
                        }
                        else
                        {
                            stringBuilder.Append(Operation.JPZ).Append(" #").Append(end);
                        }
                    }
                    else
                    {
                        if (negation == "while")
                        {
                            stringBuilder.Append(Operation.JPF).Append(':').Append(start).Append(" #").AppendLine(negate);
                            stringBuilder.Append(Operation.JMP).Append(" #").AppendLine(end);
                            stringBuilder.Append(Operation.NOP).Append(':').Append(negate);
                        }
                        else
                        {
                            stringBuilder.Append(Operation.JPF).Append(':').Append(start).Append(" #").Append(end);
                        }
                    }

                    repeatReferenceStack.Push(end);
                    repeatReferenceStack.Push(start);
                }
                else if (RepeatEndPattern().Match(line).Success)
                {
                    string start = repeatReferenceStack.Pop();
                    string end = repeatReferenceStack.Pop();

                    stringBuilder.Append(Operation.JMP).Append(" #").AppendLine(start);
                    stringBuilder.Append(Operation.NOP).Append(':').Append(end);
                }
                else if (SkipStartPattern().Match(line) is { Success: true } skipStart)
                {
                    string target = skipStart.Groups["target"].Value;
                    string negation = skipStart.Groups["negate"].Value;

                    string end = string.Concat(skipReferenceBase, ++skipCounter);
                    string negate = string.Concat(skipReferenceBase, negateReference, skipCounter);

                    if (target.Length > 0)
                    {
                        stringBuilder.Append(Operation.WAC).Append(' ');
                        if (target[0] != '#')
                            stringBuilder.Append('#');
                        stringBuilder.AppendLine(target);

                        if (negation == "unless")
                        {
                            stringBuilder.Append(Operation.JPZ).Append(" #").AppendLine(negate);
                            stringBuilder.Append(Operation.JMP).Append(" #").AppendLine(end);
                            stringBuilder.Append(Operation.NOP).Append(':').Append(negate);
                        }
                        else
                        {
                            stringBuilder.Append(Operation.JPZ).Append(" #").Append(end);
                        }
                    }
                    else
                    {
                        if (negation == "unless")
                        {
                            stringBuilder.Append(Operation.JPF).Append(" #").AppendLine(negate);
                            stringBuilder.Append(Operation.JMP).Append(" #").AppendLine(end);
                            stringBuilder.Append(Operation.NOP).Append(':').Append(negate);
                        }
                        else
                        {
                            stringBuilder.Append(Operation.JPF).Append(" #").Append(end);
                        }
                    }

                    skipReferenceStack.Push(end);
                }
                else if (SkipEndPattern().Match(line).Success)
                {
                    string end = skipReferenceStack.Pop();

                    stringBuilder.Append(Operation.NOP).Append(':').Append(end);
                }
                else if (FuncStartPattern().Match(line) is { Success: true } function)
                {
                    if (currentFunction is not null)
                        throw new Exception("Cannot declare a function in another function");

                    currentFunction = function.Groups["name"].Value;
                    constantsToAdd.Add(1);

                    stringBuilder.Append(Operation.JMP).Append(" #").Append(functionSkipBase).AppendLine(currentFunction);
                    stringBuilder.Append('#').Append(functionReferenceBase).Append(currentFunction).Append(':').Append(functionPointerReferenceBase).AppendLine(currentFunction);
                    stringBuilder.Append(Operation.ADD).Append(':').Append(functionReferenceBase).Append(currentFunction).Append(" #").Append(constBase).AppendLine("1");
                    stringBuilder.Append(Operation.RAC).Append(" #").Append(functionPointerBase).Append(currentFunction);
                }
                else if (FuncCallPattern().Match(line) is { Success: true } functionCall)
                {
                    string functionName = functionCall.Groups["name"].Value;

                    stringBuilder.Append(Operation.WAC).Append(" #").Append(functionPointerReferenceBase).AppendLine(functionName);
                    stringBuilder.Append(Operation.SWP);
                }
                else if (FuncEndPattern().Match(line).Success)
                {
                    if (currentFunction is null)
                        throw new Exception("Cannot end a function in that doesn't exist");

                    stringBuilder.AppendLine(nameof(Operation.JMP));
                    stringBuilder.Append(':').Append(functionPointerBase).AppendLine(currentFunction);
                    stringBuilder.Append(Operation.NOP).Append(':').Append(functionSkipBase).Append(currentFunction);

                    currentFunction = null;
                }
                else if (SetValuePattern().Match(line) is { Success: true } setValue)
                {
                    string targetRef = setValue.Groups["target"].Value;
                    string sourceRef = setValue.Groups["source"].Value;
                    string operation = setValue.Groups["operation"].Value;

                    stringBuilder.Append(Operation.WAC).Append(' ');
                    bool sourceIsNumber = false;
                    if (int.TryParse(sourceRef, out int sourceAsInt))
                    {
                        constantsToAdd.Add(sourceAsInt);
                        sourceIsNumber = true;
                    }

                    if (sourceRef[0] != '#')
                        stringBuilder.Append('#');

                    if (sourceIsNumber)
                        stringBuilder.Append(constBase).Append(sourceAsInt).AppendLine();
                    else
                        stringBuilder.Append(sourceRef).AppendLine();

                    if (operation.Length > 0)
                    {
                        string argument = setValue.Groups["argument"].Value;
                        bool isNumber = true;
                        if (!int.TryParse(argument, out int argumentAsInt))
                        {
                            if (argument[0] == '#')
                                argument = argument[1..];
                            isNumber = false;
                        }

                        switch (operation[0])
                        {
                            case '+':
                                if (isNumber)
                                    constantsToAdd.Add(argumentAsInt);
                                stringBuilder.Append(Operation.ADD).Append(" #");
                                if (isNumber)
                                    stringBuilder.Append(constBase);
                                stringBuilder.AppendLine(argument);
                                break;
                            case '-':
                                if (isNumber)
                                    constantsToAdd.Add(argumentAsInt);
                                stringBuilder.Append(Operation.SUB).Append(" #");
                                if (isNumber)
                                    stringBuilder.Append(constBase);
                                stringBuilder.AppendLine(argument);
                                break;
                            case '<':
                                while (argumentAsInt-- > 0)
                                    stringBuilder.AppendLine(nameof(Operation.BSL));
                                break;
                            case '>':
                                while (argumentAsInt-- > 0)
                                    stringBuilder.AppendLine(nameof(Operation.BSR));
                                break;
                        }
                    }

                    stringBuilder.Append(Operation.RAC).Append(' ');
                    if (targetRef[0] != '#')
                        stringBuilder.Append('#');
                    stringBuilder.Append(targetRef);
                }
                else if (DefinePattern().Match(line) is { Success: true } define)
                {
                    string defineRef = string.Concat(defineReferenceBase, ++defineCounter);

                    Dictionary<string, int> definitions = [];
                    foreach (Capture capture in define.Groups["nameonly"].Captures)
                    {
                        definitions[capture.Value] = 0;
                    }
                    foreach (Capture capture in define.Groups["withvalue"].Captures)
                    {
                        int indexOf = capture.Value.IndexOf('=');
                        definitions[capture.Value.Substring(0, indexOf)] = int.Parse(capture.Value.Substring(indexOf + 1));
                    }

                    stringBuilder.Append(Operation.JMP).Append(" #").AppendLine(defineRef);

                    foreach ((string name, int value) in definitions.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                        stringBuilder.Append(value).Append(':').AppendLine(name);

                    stringBuilder.Append(Operation.NOP).Append(':').Append(defineRef);
                }
                else
                {
                    stringBuilder.Append(line);
                }
            }

            if (constantsToAdd.Count > 0)
            {
                stringBuilder.AppendLine();
                stringBuilder.Append(Operation.JMP).Append(" #").AppendLine(constantReferenceBase.ToString());
                foreach (int constant in constantsToAdd.Order())
                    stringBuilder.Append(constant).Append(':').Append(constBase).AppendLine(constant.ToString());

                stringBuilder.Append(Operation.NOP).Append(':').Append(constantReferenceBase.ToString());
            }

            return stringBuilder.ToString();
        }

        public static string RemoveCommentsAndWhitespace(string input)
        {
            StringReader stringReader = new(input);
            StringBuilder stringBuilder = new();

            bool firstLine = true;
            while (stringReader.ReadLine() is string line)
            {
                int startOfComment = line.IndexOf('/');
                if (startOfComment > -1)
                    line = line[..startOfComment];

                line = line.Trim([' ', '\t']);
                if (line.Length < 1)
                    continue;

                if (firstLine)
                    firstLine = false;
                else
                    stringBuilder.AppendLine();

                stringBuilder.Append(line);
            }

            return stringBuilder.ToString();
        }

        public static List<byte> AssemblyToProgram(string input, out string[] markers)
        {
            string cleaned = RemoveCommentsAndWhitespace(input);
            string expanded = ExpandSyntax(cleaned);
            string sequenced = SingleLineToSequence(expanded);
            string addressed = ReferencesToAddresses(sequenced, out markers);
            string codes = OperationsToCodes(addressed);
            return CodesToProgram(codes);
        }
    }
}