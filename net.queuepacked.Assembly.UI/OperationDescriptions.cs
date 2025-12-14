using net.queuepacked.Assembly.CPU;

namespace net.queuepacked.Assembly.UI
{
    public static class OperationDescriptions
    {
        private static readonly (string name, string description)[] Descriptions =
        [
            ("No operation","""
                        Advance program counter by one.
                        """),

            ("Read from accumulator","""
                        Write content of the accumulator in the provided address.
                        Advance program counter by two.
                        """),

            ("Write to accumulator","""
                        Write content of the provided address to the accumulator
                        Advance program counter by two.
                        """),

            ("Swap","""
                        Swap contents of accumulator and program counter.
                        """),

            ("Add","""
                        Add content of provided address to the accumulator.
                        Set ALU flag in case of overflow, otherwise reset it.
                        Advance program counter by two.
                        """),

            ("Subtract","""
                        Subtract content of provided address from the accumulator.
                        Set ALU flag in case of underflow, otherwise reset it.
                        Advance program counter by two.
                        """),

            ("Bit shift left","""
                        Shift bits of the accumulator to the left by one.
                        Set ALU flag to the bit that was moved out.
                        Advance program counter by one.
                        """),

            ("Bit shift right","""
                        Shift bits of the accumulator to the right by one.
                        Set ALU flag to the bit that was moved out.
                        Advance program counter by one.
                        """),

            ("Jump","""
                        Set program counter to the provided value.
                        """),

            ("Jump if zero","""
                        If content of accumulator equals zero, set the program counter to the provided value.
                        Otherwise, advance program counter by two.
                        """),

            ("Jump if flag","""
                        If ALU flag is set, set the program counter to the provided value.
                        Otherwise, advance program counter by two.
                        """),

            ("Read from I/O","""
                        Write the current content of the I/O register into the provided address.
                        Advance program counter by two.
                        """),

            ("Write to I/O","""
                        Write the content of the provided address into the I/O register.
                        Advance program counter by two.
                        """)
        ];
        
        public static (string name, string description) GetOperationDetails(Operation operation) => Descriptions[(int)operation];
    }
}
