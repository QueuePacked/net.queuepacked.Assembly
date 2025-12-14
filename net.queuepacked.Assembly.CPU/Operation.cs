using System.Diagnostics.CodeAnalysis;

namespace net.queuepacked.Assembly.CPU
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum Operation : byte
    {
        NOP = 0,

        RAC = 1,
        WAC = 2,
        SWP = 3,

        ADD = 4,
        SUB = 5,
        BSL = 6,
        BSR = 7,

        JMP = 8,
        JPZ = 9,
        JPF = 10,

        RIO = 11,
        WIO = 12
    }
}