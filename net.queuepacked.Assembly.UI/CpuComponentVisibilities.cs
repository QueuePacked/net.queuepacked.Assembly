using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace net.queuepacked.Assembly.UI;

public class CpuComponentVisibilities : INotifyPropertyChanged
{
    private bool _stepReadOp;
    private bool _stepReadArg;
    private bool _stepProcess;
    private bool _stepIncPc;
    private bool _memory;
    private bool _addresses;
    private bool _pc;
    private bool _alu;
    private bool _io;
    private bool _opDesc;
    private bool _code;
    private bool _loadedOp;
    private bool _loadedArg;
    private bool _insCount;
    private bool _pcMarker;
    private bool _referenceNames;

    public bool StepReadOp
    {
        get => _stepReadOp;
        set
        {
            if (value == _stepReadOp) return;
            _stepReadOp = value;
            OnPropertyChanged();
        }
    }

    public bool StepReadArg
    {
        get => _stepReadArg;
        set
        {
            if (value == _stepReadArg) return;
            _stepReadArg = value;
            OnPropertyChanged();
        }
    }

    public bool StepProcess
    {
        get => _stepProcess;
        set
        {
            if (value == _stepProcess) return;
            _stepProcess = value;
            OnPropertyChanged();
        }
    }

    public bool StepIncPc
    {
        get => _stepIncPc;
        set
        {
            if (value == _stepIncPc) return;
            _stepIncPc = value;
            OnPropertyChanged();
        }
    }

    public bool Memory
    {
        get => _memory;
        set
        {
            if (value == _memory) return;
            _memory = value;
            OnPropertyChanged();
        }
    }

    public bool Addresses
    {
        get => _addresses;
        set
        {
            if (value == _addresses) return;
            _addresses = value;
            OnPropertyChanged();
        }
    }

    public bool Pc
    {
        get => _pc;
        set
        {
            if (value == _pc) return;
            _pc = value;
            OnPropertyChanged();
        }
    }

    public bool PcMarker
    {
        get => _pcMarker;
        set
        {
            if (value == _pcMarker) return;
            _pcMarker = value;
            OnPropertyChanged();
        }
    }

    public bool ReferenceNames
    {
        get => _referenceNames;
        set
        {
            if (value == _referenceNames) return;
            _referenceNames = value;
            OnPropertyChanged();
        }
    }

    public bool Alu
    {
        get => _alu;
        set
        {
            if (value == _alu) return;
            _alu = value;
            OnPropertyChanged();
        }
    }

    public bool Io
    {
        get => _io;
        set
        {
            if (value == _io) return;
            _io = value;
            OnPropertyChanged();
        }
    }

    public bool Code
    {
        get => _code;
        set
        {
            if (value == _code) return;
            _code = value;
            OnPropertyChanged();
        }
    }

    public bool OpDesc
    {
        get => _opDesc;
        set
        {
            if (value == _opDesc) return;
            _opDesc = value;
            OnPropertyChanged();
        }
    }

    public bool LoadedOp
    {
        get => _loadedOp;
        set
        {
            if (value == _loadedOp) return;
            _loadedOp = value;
            OnPropertyChanged();
        }
    }

    public bool LoadedArg
    {
        get => _loadedArg;
        set
        {
            if (value == _loadedArg) return;
            _loadedArg = value;
            OnPropertyChanged();
        }
    }

    public bool InsCount
    {
        get => _insCount;
        set
        {
            if (value == _insCount) return;
            _insCount = value;
            OnPropertyChanged();
        }
    }


    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}