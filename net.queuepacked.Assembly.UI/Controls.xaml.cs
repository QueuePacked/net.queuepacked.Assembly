using net.queuepacked.Assembly.CPU;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace net.queuepacked.Assembly.UI
{
    /// <summary>
    /// Interaction logic for Controls.xaml
    /// </summary>
    public partial class Controls : Window
    {
        public Model Model { get; }

        private readonly CodeView _codeView;
        private readonly CpuView _cpuView;
        private readonly object _lock;

        private readonly bool _makeScreenshots;

        public Controls()
        {
            Model = new Model(new Cpu());
            DataContext = Model;
            _codeView = new CodeView(Model);
            _codeView.Show();
            _cpuView = new CpuView(Model);
            _cpuView.Show();

            _lock = new object();
            _makeScreenshots = false;

            InitializeComponent();
        }

        private void SaveCodeView()
        {
            if (_makeScreenshots)
                Task.Delay(20).ContinueWith(t => Dispatcher.Invoke(SaveCodeViewDelayed));
        }

        private void SaveCpuView()
        {
            if (_makeScreenshots)
                Task.Delay(20).ContinueWith(t => Dispatcher.Invoke(SaveCpuViewDelayed));
        }

        private void SaveCodeViewDelayed()
        {
            const string screenshotPath = @"G:\Recordings\WPF Screenshots\CODE_";
            const string screenshotExtension = @".png";

            lock (_lock)
            {
                int counter = 1;

                string filePath = screenshotPath + counter.ToString("D3") + screenshotExtension;
                while (File.Exists(filePath))
                {
                    ++counter;
                    filePath = screenshotPath + counter.ToString("D3") + screenshotExtension;
                }

                using (FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    _codeView.SaveAsScreenshot(fileStream);
            }
        }

        private void SaveCpuViewDelayed()
        {
            const string screenshotPath = @"G:\Recordings\WPF Screenshots\CPU_";
            const string screenshotExtension = @".png";

            lock (_lock)
            {
                int counter = 1;

                string filePath = screenshotPath + counter.ToString("D3") + screenshotExtension;
                while (File.Exists(filePath))
                {
                    ++counter;
                    filePath = screenshotPath + counter.ToString("D3") + screenshotExtension;
                }

                using (FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    _cpuView.SaveAsScreenshot(fileStream);
            }
        }

        /// <inheritdoc />
        protected override void OnClosing(CancelEventArgs e)
        {
            _codeView.Close();
            _cpuView.Close();
        }

        private void Backwards_OnClick(object sender, RoutedEventArgs e)
        {
            Model.Backward();
            SaveCpuView();
        }

        private void BackwardsInstruction_OnClick(object sender, RoutedEventArgs e)
        {
            int count = 0;
            do
            {
                if (++count > 8)
                    break;

                Model.Backward();

                if (Model.ReadsIn || Model.WritesOut)
                    break;
            }
            while (Model.Cpu.State.ProcessCycleStep != Step.ReadOpCode || Model.Cpu.State.CycleAdvance);
            SaveCpuView();
        }

        private void Forwards_OnClick(object sender, RoutedEventArgs e)
        {
            Model.Forward();
            SaveCpuView();
        }

        private void ForwardsInstruction_OnClick(object sender, RoutedEventArgs e)
        {
            int count = 0;
            do
            {
                if (++count > 8)
                    break;

                Model.Forward();

                if (Model.ReadsIn || Model.WritesOut)
                    break;
            }
            while (Model.Cpu.State.ProcessCycleStep != Step.IncrementProgramCounter || !Model.Cpu.State.CycleAdvance);
            SaveCpuView();
        }

        private void LoadProgram_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string code = AssemblyCompiler.RemoveCommentsAndWhitespace(Model.Code);
                code = AssemblyCompiler.ExpandSyntax(code);
                code = AssemblyCompiler.SingleLineToSequence(code);
                code = AssemblyCompiler.ReferencesToAddresses(code, out string[] markers);
                code = AssemblyCompiler.OperationsToCodes(code);
                List<byte> byteCode = AssemblyCompiler.CodesToProgram(code);
                Model.LoadNewProgram(byteCode, markers, Model.Code);
            }
            catch
            {
                // ignored
            }
            SaveCpuView();
        }

        private void OpCode_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string code = AssemblyCompiler.RemoveCommentsAndWhitespace(Model.Code);
                code = AssemblyCompiler.ExpandSyntax(code);
                code = AssemblyCompiler.SingleLineToSequence(code);
                code = AssemblyCompiler.ReferencesToAddresses(code, out string[] markers);
                Model.Code = AssemblyCompiler.OperationsToCodes(code);
            }
            catch
            {
                // ignored
            }
            SaveCpuView();
        }

        private void References_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string code = AssemblyCompiler.RemoveCommentsAndWhitespace(Model.Code);
                code = AssemblyCompiler.ExpandSyntax(code);
                code = AssemblyCompiler.SingleLineToSequence(code);
                Model.Code = AssemblyCompiler.ReferencesToAddresses(code, out string[] markers);
            }
            catch
            {
                // ignored
            }
            SaveCpuView();
        }

        private void Sequence_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string code = AssemblyCompiler.RemoveCommentsAndWhitespace(Model.Code);
                code = AssemblyCompiler.ExpandSyntax(code);
                Model.Code = AssemblyCompiler.SingleLineToSequence(code);
            }
            catch
            {
                // ignored
            }
            SaveCpuView();
        }

        private void Expand_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string code = AssemblyCompiler.RemoveCommentsAndWhitespace(Model.Code);
                Model.Code = AssemblyCompiler.ExpandSyntax(code);
            }
            catch
            {
                // ignored
            }
            SaveCpuView();
        }

        private void Comments_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Model.Code = AssemblyCompiler.RemoveCommentsAndWhitespace(Model.Code);
            }
            catch
            {
                // ignored
            }
            SaveCpuView();
        }

        private void CpuView_OnClick(object sender, RoutedEventArgs e)
        {
            _cpuView.Left = Left;
            _cpuView.Top = Top;
        }

        private void CodeView_OnClick(object sender, RoutedEventArgs e)
        {
            _codeView.Left = Left;
            _codeView.Top = Top;
        }

        private void SelectAll_OnClick(object sender, RoutedEventArgs e)
        {
            SetVisibilities(true);
            SaveCpuView();
        }

        private void DeselectAll_OnClick(object sender, RoutedEventArgs e)
        {
            SetVisibilities(false);
            SaveCpuView();
        }

        private void SetVisibilities(bool visible)
        {
            Model.ComponentVisibilities.StepReadOp = visible;
            Model.ComponentVisibilities.StepReadArg = visible;
            Model.ComponentVisibilities.StepProcess = visible;
            Model.ComponentVisibilities.StepIncPc = visible;
            Model.ComponentVisibilities.Memory = visible;
            Model.ComponentVisibilities.Addresses = visible;
            Model.ComponentVisibilities.PcMarker = visible;
            Model.ComponentVisibilities.ReferenceNames = visible;
            Model.ComponentVisibilities.Pc = visible;
            Model.ComponentVisibilities.Alu = visible;
            Model.ComponentVisibilities.Io = visible;
            Model.ComponentVisibilities.OpDesc = visible;
            //Model.ComponentVisibilities.Code = visible;
            Model.ComponentVisibilities.LoadedOp = visible;
            Model.ComponentVisibilities.LoadedArg = visible;
            Model.ComponentVisibilities.InsCount = visible;
            SaveCpuView();
        }

        private void ButtonCpuScreenshot_OnClick(object sender, RoutedEventArgs e)
        {
            SaveCpuView();
        }

        private void ButtonCodeScreenshot_OnClick(object sender, RoutedEventArgs e)
        {
            SaveCodeView();
        }
    }
}
