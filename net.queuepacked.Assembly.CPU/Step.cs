namespace net.queuepacked.Assembly.CPU
{
    public enum Step : byte
    {
        ReadOpCode,
        ReadOptionalArgument,
        ProcessOperation,
        IncrementProgramCounter
    }
}