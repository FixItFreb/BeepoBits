using System;

public static class Utilities
{
    public static bool IsMultipleOfFour(int num)
    {
        return num == (num & ~0x3);
    }

    public static int GetStringAlignedSize(int size)
    {
        return (size + 4) & ~0x3;
    }
    
    public static int GetBufferAlignedSize(int size)
    {
        var offset = size & ~0x3;
        return (offset == size) ? size : (offset + 4);
    }
}