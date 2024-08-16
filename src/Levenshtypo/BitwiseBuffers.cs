#if NET8_0_OR_GREATER

using System.Numerics;
using System.Runtime.CompilerServices;
using System;

namespace Levenshtypo;

internal static class BitwiseBuffers
{

    internal interface IBuffer<TSelf, TState>
        where TSelf : IBuffer<TSelf, TState>
        where TState : IBinaryInteger<TState>
    {
        static abstract int MaxError { get; }
        static abstract int MaxVectorLength { get; }
        static abstract TState StateMask { get; }

        static abstract Span<TState> GetBuffer(ref TSelf buffer);
    }

    #region Buffers
    /*
     * 
            int maxVectorLength = 2 * TBuffer.MaxError + 1;
            
            TNumber stateMask = (TNumber.One << (1 + maxVectorLength)) - TNumber.One;

    */
    /* To Generate the buffers
     * 
     *  var sb = new StringBuilder();
     *  for(var i = 1; i <= 30; i++)
     *  {
     *      var numericType = (2 * i + 2) switch
     *      {
     *          <= 8 => "byte",
     *          <= 16 => "ushort",
     *          <= 32 => "uint",
     *          <= 64 => "ulong"
     *      };
     *
     *      sb.AppendLine(
     *          $$"""
     *          [InlineArray({{i + 1}})]
     *          internal struct BufferDistance{{i}} : IBuffer<BufferDistance{{i}}, {{numericType}}>
     *          {
     *              public {{numericType}} _state0;
     *              
     *              public static int MaxError => {{i}};
     *              
     *              public static int MaxVectorLength => {{2 * i + 1}};
     *              
     *              public static {{numericType}} StateMask => 0x{{(1ul << (2 * i + 2)) - 1:X2}};
     *              
     *              public static Span<{{numericType}}> GetBuffer(ref BufferDistance{{i}} @this) => @this;
     *          }
     *  
     *          """);
     *  }
     *  Console.WriteLine(sb);
     * 
     * */

    [InlineArray(2)]
    internal struct BufferDistance1 : IBuffer<BufferDistance1, byte>
    {
        public byte _state0;

        public static int MaxError => 1;

        public static int MaxVectorLength => 3;

        public static byte StateMask => 0x0F;

        public static Span<byte> GetBuffer(ref BufferDistance1 @this) => @this;
    }

    [InlineArray(3)]
    internal struct BufferDistance2 : IBuffer<BufferDistance2, byte>
    {
        public byte _state0;

        public static int MaxError => 2;

        public static int MaxVectorLength => 5;

        public static byte StateMask => 0x3F;

        public static Span<byte> GetBuffer(ref BufferDistance2 @this) => @this;
    }

    [InlineArray(4)]
    internal struct BufferDistance3 : IBuffer<BufferDistance3, byte>
    {
        public byte _state0;

        public static int MaxError => 3;

        public static int MaxVectorLength => 7;

        public static byte StateMask => 0xFF;

        public static Span<byte> GetBuffer(ref BufferDistance3 @this) => @this;
    }

    [InlineArray(5)]
    internal struct BufferDistance4 : IBuffer<BufferDistance4, ushort>
    {
        public ushort _state0;

        public static int MaxError => 4;

        public static int MaxVectorLength => 9;

        public static ushort StateMask => 0x3FF;

        public static Span<ushort> GetBuffer(ref BufferDistance4 @this) => @this;
    }

    [InlineArray(6)]
    internal struct BufferDistance5 : IBuffer<BufferDistance5, ushort>
    {
        public ushort _state0;

        public static int MaxError => 5;

        public static int MaxVectorLength => 11;

        public static ushort StateMask => 0xFFF;

        public static Span<ushort> GetBuffer(ref BufferDistance5 @this) => @this;
    }

    [InlineArray(7)]
    internal struct BufferDistance6 : IBuffer<BufferDistance6, ushort>
    {
        public ushort _state0;

        public static int MaxError => 6;

        public static int MaxVectorLength => 13;

        public static ushort StateMask => 0x3FFF;

        public static Span<ushort> GetBuffer(ref BufferDistance6 @this) => @this;
    }

    [InlineArray(8)]
    internal struct BufferDistance7 : IBuffer<BufferDistance7, ushort>
    {
        public ushort _state0;

        public static int MaxError => 7;

        public static int MaxVectorLength => 15;

        public static ushort StateMask => 0xFFFF;

        public static Span<ushort> GetBuffer(ref BufferDistance7 @this) => @this;
    }

    [InlineArray(9)]
    internal struct BufferDistance8 : IBuffer<BufferDistance8, uint>
    {
        public uint _state0;

        public static int MaxError => 8;

        public static int MaxVectorLength => 17;

        public static uint StateMask => 0x3FFFF;

        public static Span<uint> GetBuffer(ref BufferDistance8 @this) => @this;
    }

    [InlineArray(10)]
    internal struct BufferDistance9 : IBuffer<BufferDistance9, uint>
    {
        public uint _state0;

        public static int MaxError => 9;

        public static int MaxVectorLength => 19;

        public static uint StateMask => 0xFFFFF;

        public static Span<uint> GetBuffer(ref BufferDistance9 @this) => @this;
    }

    [InlineArray(11)]
    internal struct BufferDistance10 : IBuffer<BufferDistance10, uint>
    {
        public uint _state0;

        public static int MaxError => 10;

        public static int MaxVectorLength => 21;

        public static uint StateMask => 0x3FFFFF;

        public static Span<uint> GetBuffer(ref BufferDistance10 @this) => @this;
    }

    [InlineArray(12)]
    internal struct BufferDistance11 : IBuffer<BufferDistance11, uint>
    {
        public uint _state0;

        public static int MaxError => 11;

        public static int MaxVectorLength => 23;

        public static uint StateMask => 0xFFFFFF;

        public static Span<uint> GetBuffer(ref BufferDistance11 @this) => @this;
    }

    [InlineArray(13)]
    internal struct BufferDistance12 : IBuffer<BufferDistance12, uint>
    {
        public uint _state0;

        public static int MaxError => 12;

        public static int MaxVectorLength => 25;

        public static uint StateMask => 0x3FFFFFF;

        public static Span<uint> GetBuffer(ref BufferDistance12 @this) => @this;
    }

    [InlineArray(14)]
    internal struct BufferDistance13 : IBuffer<BufferDistance13, uint>
    {
        public uint _state0;

        public static int MaxError => 13;

        public static int MaxVectorLength => 27;

        public static uint StateMask => 0xFFFFFFF;

        public static Span<uint> GetBuffer(ref BufferDistance13 @this) => @this;
    }

    [InlineArray(15)]
    internal struct BufferDistance14 : IBuffer<BufferDistance14, uint>
    {
        public uint _state0;

        public static int MaxError => 14;

        public static int MaxVectorLength => 29;

        public static uint StateMask => 0x3FFFFFFF;

        public static Span<uint> GetBuffer(ref BufferDistance14 @this) => @this;
    }

    [InlineArray(16)]
    internal struct BufferDistance15 : IBuffer<BufferDistance15, uint>
    {
        public uint _state0;

        public static int MaxError => 15;

        public static int MaxVectorLength => 31;

        public static uint StateMask => 0xFFFFFFFF;

        public static Span<uint> GetBuffer(ref BufferDistance15 @this) => @this;
    }

    [InlineArray(17)]
    internal struct BufferDistance16 : IBuffer<BufferDistance16, ulong>
    {
        public ulong _state0;

        public static int MaxError => 16;

        public static int MaxVectorLength => 33;

        public static ulong StateMask => 0x3FFFFFFFF;

        public static Span<ulong> GetBuffer(ref BufferDistance16 @this) => @this;
    }

    [InlineArray(18)]
    internal struct BufferDistance17 : IBuffer<BufferDistance17, ulong>
    {
        public ulong _state0;

        public static int MaxError => 17;

        public static int MaxVectorLength => 35;

        public static ulong StateMask => 0xFFFFFFFFF;

        public static Span<ulong> GetBuffer(ref BufferDistance17 @this) => @this;
    }

    [InlineArray(19)]
    internal struct BufferDistance18 : IBuffer<BufferDistance18, ulong>
    {
        public ulong _state0;

        public static int MaxError => 18;

        public static int MaxVectorLength => 37;

        public static ulong StateMask => 0x3FFFFFFFFF;

        public static Span<ulong> GetBuffer(ref BufferDistance18 @this) => @this;
    }

    [InlineArray(20)]
    internal struct BufferDistance19 : IBuffer<BufferDistance19, ulong>
    {
        public ulong _state0;

        public static int MaxError => 19;

        public static int MaxVectorLength => 39;

        public static ulong StateMask => 0xFFFFFFFFFF;

        public static Span<ulong> GetBuffer(ref BufferDistance19 @this) => @this;
    }

    [InlineArray(21)]
    internal struct BufferDistance20 : IBuffer<BufferDistance20, ulong>
    {
        public ulong _state0;

        public static int MaxError => 20;

        public static int MaxVectorLength => 41;

        public static ulong StateMask => 0x3FFFFFFFFFF;

        public static Span<ulong> GetBuffer(ref BufferDistance20 @this) => @this;
    }

    [InlineArray(22)]
    internal struct BufferDistance21 : IBuffer<BufferDistance21, ulong>
    {
        public ulong _state0;

        public static int MaxError => 21;

        public static int MaxVectorLength => 43;

        public static ulong StateMask => 0xFFFFFFFFFFF;

        public static Span<ulong> GetBuffer(ref BufferDistance21 @this) => @this;
    }

    [InlineArray(23)]
    internal struct BufferDistance22 : IBuffer<BufferDistance22, ulong>
    {
        public ulong _state0;

        public static int MaxError => 22;

        public static int MaxVectorLength => 45;

        public static ulong StateMask => 0x3FFFFFFFFFFF;

        public static Span<ulong> GetBuffer(ref BufferDistance22 @this) => @this;
    }

    [InlineArray(24)]
    internal struct BufferDistance23 : IBuffer<BufferDistance23, ulong>
    {
        public ulong _state0;

        public static int MaxError => 23;

        public static int MaxVectorLength => 47;

        public static ulong StateMask => 0xFFFFFFFFFFFF;

        public static Span<ulong> GetBuffer(ref BufferDistance23 @this) => @this;
    }

    [InlineArray(25)]
    internal struct BufferDistance24 : IBuffer<BufferDistance24, ulong>
    {
        public ulong _state0;

        public static int MaxError => 24;

        public static int MaxVectorLength => 49;

        public static ulong StateMask => 0x3FFFFFFFFFFFF;

        public static Span<ulong> GetBuffer(ref BufferDistance24 @this) => @this;
    }

    [InlineArray(26)]
    internal struct BufferDistance25 : IBuffer<BufferDistance25, ulong>
    {
        public ulong _state0;

        public static int MaxError => 25;

        public static int MaxVectorLength => 51;

        public static ulong StateMask => 0xFFFFFFFFFFFFF;

        public static Span<ulong> GetBuffer(ref BufferDistance25 @this) => @this;
    }

    [InlineArray(27)]
    internal struct BufferDistance26 : IBuffer<BufferDistance26, ulong>
    {
        public ulong _state0;

        public static int MaxError => 26;

        public static int MaxVectorLength => 53;

        public static ulong StateMask => 0x3FFFFFFFFFFFFF;

        public static Span<ulong> GetBuffer(ref BufferDistance26 @this) => @this;
    }

    [InlineArray(28)]
    internal struct BufferDistance27 : IBuffer<BufferDistance27, ulong>
    {
        public ulong _state0;

        public static int MaxError => 27;

        public static int MaxVectorLength => 55;

        public static ulong StateMask => 0xFFFFFFFFFFFFFF;

        public static Span<ulong> GetBuffer(ref BufferDistance27 @this) => @this;
    }

    [InlineArray(29)]
    internal struct BufferDistance28 : IBuffer<BufferDistance28, ulong>
    {
        public ulong _state0;

        public static int MaxError => 28;

        public static int MaxVectorLength => 57;

        public static ulong StateMask => 0x3FFFFFFFFFFFFFF;

        public static Span<ulong> GetBuffer(ref BufferDistance28 @this) => @this;
    }

    [InlineArray(30)]
    internal struct BufferDistance29 : IBuffer<BufferDistance29, ulong>
    {
        public ulong _state0;

        public static int MaxError => 29;

        public static int MaxVectorLength => 59;

        public static ulong StateMask => 0xFFFFFFFFFFFFFFF;

        public static Span<ulong> GetBuffer(ref BufferDistance29 @this) => @this;
    }

    [InlineArray(31)]
    internal struct BufferDistance30 : IBuffer<BufferDistance30, ulong>
    {
        public ulong _state0;

        public static int MaxError => 30;

        public static int MaxVectorLength => 61;

        public static ulong StateMask => 0x3FFFFFFFFFFFFFFF;

        public static Span<ulong> GetBuffer(ref BufferDistance30 @this) => @this;
    }

    #endregion
}


#endif