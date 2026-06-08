using System;

// The [Flags] attribute tells Unity this enum represents a bitmask
[Flags]
public enum PipeDirection
{
    None = 0,      // 0000
    Up = 1 << 0, // 0001 (1)
    Right = 1 << 1, // 0010 (2)
    Down = 1 << 2, // 0100 (4)
    Left = 1 << 3  // 1000 (8)
}