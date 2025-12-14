namespace net.queuepacked.Assembly.UI;

public class MemoryItem
{
    public bool Valid { get; }
    public byte Value { get; }
    public byte Address { get; }
    public bool IsPc { get; }
    public bool IsLimiter { get; }
    public bool Offset { get; }
    public bool NoOffset => !Offset;
    public string Marker { get; }
    public CpuComponentVisibilities ComponentVisibilities { get; }

    public MemoryItem(bool valid, byte value, byte address, bool isPc, bool isLimiter, bool offset, string marker, CpuComponentVisibilities componentVisibilities)
    {
        Valid = valid;
        Value = value;
        Address = address;
        IsPc = isPc;
        IsLimiter = isLimiter;
        Offset = offset;
        Marker = marker;
        ComponentVisibilities = componentVisibilities;
    }
}