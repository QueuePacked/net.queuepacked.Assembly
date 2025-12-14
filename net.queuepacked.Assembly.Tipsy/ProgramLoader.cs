using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using net.queuepacked.Assembly.CPU;

namespace net.queuepacked.Assembly.Tipsy
{
    internal class ProgramLoader
    {
        private readonly string _filePath;
        private readonly bool _generateOutput;

        private readonly string _fileNameBase;
        private readonly string _extension;

        public ImmutableArray<byte> Program;

        public ProgramLoader(string filePath, bool generateOutput)
        {
            _filePath = filePath;
            _generateOutput = generateOutput;
            Program = [];

            string directoryName = Path.GetDirectoryName(_filePath) ?? string.Empty;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(_filePath);
            _fileNameBase = Path.Combine(directoryName, fileNameWithoutExtension) + ".";
            _extension = Path.GetExtension(_filePath);
        }

        public bool LoadProgram([NotNullWhen(false)] out string? error)
        {
            string errorAtStage = $"Failed to load from file '{_filePath}'";
            try
            {
                string program = File.ReadAllText(_filePath);

                int stage = 0;

                errorAtStage = "Failed to remove whitespace and comments";
                string cleaned = AssemblyCompiler.RemoveCommentsAndWhitespace(program);
                if (_generateOutput)
                {
                    errorAtStage = $"Failed to write output of stage {stage} '{nameof(cleaned)}'";
                    File.WriteAllText(_fileNameBase + ++stage + "_" + nameof(cleaned) + _extension, cleaned, Encoding.UTF8);
                }

                errorAtStage = "Failed to expand syntax";
                string expanded = AssemblyCompiler.ExpandSyntax(cleaned);
                if (_generateOutput)
                {
                    errorAtStage = $"Failed to write output of stage {stage} '{nameof(expanded)}'";
                    File.WriteAllText(_fileNameBase + ++stage + "_" + nameof(expanded) + _extension, expanded, Encoding.UTF8);
                }

                errorAtStage = "Failed to split into lines";
                string sequenced = AssemblyCompiler.SingleLineToSequence(expanded);
                if (_generateOutput)
                {
                    errorAtStage = $"Failed to write output of stage {stage} '{nameof(sequenced)}'";
                    File.WriteAllText(_fileNameBase + ++stage + "_" + nameof(sequenced) + _extension, sequenced, Encoding.UTF8);
                }

                errorAtStage = "Failed to resolve reference addresses";
                string addressed = AssemblyCompiler.ReferencesToAddresses(sequenced, out _);
                if (_generateOutput)
                {
                    errorAtStage = $"Failed to write output of stage {stage} '{nameof(addressed)}'";
                    File.WriteAllText(_fileNameBase + ++stage + "_" + nameof(addressed) + _extension, addressed, Encoding.UTF8);
                }

                errorAtStage = "Failed to convert operations to code";
                string codes = AssemblyCompiler.OperationsToCodes(addressed);
                if (_generateOutput)
                {
                    errorAtStage = $"Failed to write output of stage {stage} '{nameof(codes)}'";
                    File.WriteAllText(_fileNameBase + ++stage + "_" + nameof(codes) + _extension, codes, Encoding.UTF8);
                }

                errorAtStage = "Failed to convert program to code sequence";
                Program = [.. AssemblyCompiler.CodesToProgram(codes)];

                error = null;
                return true;
            }
            catch
            {
                error = errorAtStage;
                return false;
            }
        }
    }
}
