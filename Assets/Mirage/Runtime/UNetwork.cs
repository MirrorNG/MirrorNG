using System.Runtime.InteropServices;

namespace Mirage
{
    // invoke type for Rpc
    public enum MirageInvokeType
    {
        ServerRpc,
        ClientRpc
    }

    public enum Version
    {
        Current = 1
    }

    /// <summary>
    /// Converts between uint and float without allocations
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct UIntFloat
    {
        [FieldOffset(0)]
        public float floatValue;

        [FieldOffset(0)]
        public uint intValue;
    }

    /// <summary>
    /// Converts between ulong and double without allocations
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct UIntDouble
    {
        [FieldOffset(0)]
        public double doubleValue;

        [FieldOffset(0)]
        public ulong longValue;
    }

    /// <summary>
    /// Converts between ulong and decimal without allocations
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct UIntDecimal
    {
        [FieldOffset(0)]
        public ulong longValue1;

        [FieldOffset(8)]
        public ulong longValue2;

        [FieldOffset(0)]
        public decimal decimalValue;
    }
}
