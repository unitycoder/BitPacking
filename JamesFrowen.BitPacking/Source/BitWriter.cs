using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace JamesFrowen.BitPacking
{
    /// <summary>
    /// These are common methods for BitWriter, 
    /// <para>This class is for simple methods and types that dont need any optimization or packing, like Boolean.</para>
    /// <para>For other types like Vector3 they should have their own packer class to handle packing and unpacking</para>
    /// </summary>
    public static class BitWriterExtensions
    {
        public static void WriteBool(this BitWriter writer, bool value)
        {
            writer.Write(value ? 1u : 0u, 1);
        }
        public static bool ReadBool(this BitReader reader)
        {
            return reader.Read(1) == 1u;
        }

        /// <summary>
        /// Writes float in range -max to +max, uses first bit to say the sign of value
        /// <para>sign bit: false is positive, true is negative</para>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="signedMaxFloat"></param>
        /// <param name="maxUint"></param>
        /// <param name="bitCount"></param>
        public static void WriteFloatSigned(this BitWriter writer, float value, float signedMaxFloat, uint maxUint, int bitCount)
        {
            // 1 bit for sign
            var bitValueCount = bitCount - 1;

            if (value < 0)
            {
                writer.WriteBool(true);
                value = -value;
            }
            else
            {
                writer.WriteBool(false);
            }

            var uValue = Compression.ScaleToUInt(value, 0, signedMaxFloat, 0, maxUint);
            writer.Write(uValue, bitValueCount);
        }

        public static float ReadFloatSigned(this BitReader reader, float maxFloat, uint maxUint, int bitCount)
        {
            // 1 bit for sign
            var bitValueCount = bitCount - 1;

            var signBit = reader.ReadBool();
            var valueBits = reader.Read(bitValueCount);

            var fValue = Compression.ScaleFromUInt(valueBits, 0, maxFloat, 0, maxUint);

            return signBit ? -fValue : fValue;
        }
    }
    
    public sealed unsafe class BitWriter : IDisposable
    {
        // todo allow this to work with pooling

        IntPtr intPtr;
        ulong* ulongPtr;
        readonly int bufferSize;
        readonly int bufferSizeLong;
        readonly int bufferSizeBits;

        /// <summary>
        /// next bit to write to inside writeByte
        /// </summary>
        int writeBit;

        public int ByteLength => Mathf.CeilToInt(this.writeBit / 8f);

        public BitWriter(int bufferSize)
        {
            // round to up multiple of 8
            bufferSize = ((bufferSize / 8) + 1) * 8;
            this.bufferSize = bufferSize;
            this.bufferSizeBits = bufferSize * 8;
            this.bufferSizeLong = bufferSize / 8;


            this.intPtr = Marshal.AllocHGlobal(bufferSize);
            var voidPtr = this.intPtr.ToPointer();
            this.ulongPtr = (ulong*)voidPtr;

            this.ClearUnmanged(this.ulongPtr, this.bufferSizeLong);
        }
        ~BitWriter()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            if (this.intPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(this.intPtr);
                this.intPtr = IntPtr.Zero;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ThrowIfDisposed()
        {
            if (this.intPtr == IntPtr.Zero) { throw new ObjectDisposedException(nameof(BitWriter)); }
        }

        void ClearUnmanged(ulong* longPtr, int count)
        {
            // clear up to count or bufferSizeLong
            for (var i = 0; i < count || i < this.bufferSizeLong; i++)
                *(longPtr + i) = 0;
        }

        public void Reset()
        {
            this.ThrowIfDisposed();

            this.ClearUnmanged(this.ulongPtr, (this.ByteLength / 8) + 1);
            this.writeBit = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint inValue, int inBits)
        {
            // todo does this check cost performance
            this.ThrowIfDisposed();
            // writing 0 is ok, but do nothing
            // 0 is allowed so that people creating inBits=0 from code dont have to deal with errors
            if (inBits == 0) { return; }

            const int MaxWriteSize = 32;
            if (inBits > MaxWriteSize) { throw new ArgumentException($"inBits should not be greater than {MaxWriteSize}", nameof(inBits)); }

            var endCount = this.writeBit + inBits;
            if (endCount > this.bufferSizeBits) { throw new EndOfStreamException(); }

            var mask = (1ul << inBits) - 1;
            var maskedValue = mask & inValue;
            // writeBit  => n         eg 188
            // remainder => r = n%64  eg 188%64 = 60
            var remainder = this.writeBit & 0b11_1111;

            // left shift by remainder
            //           => << r      eg 4 bits + 60 zeros on right (DCBA000...000)
            var value = maskedValue << remainder;

            // write new value to buffer
            //           =>           eg writes 4 new bits
            *(this.ulongPtr + (this.writeBit >> 6)) |= value;

            // is remainder over 1/2 ulong
            //           => r > 32    eg 60>32 = true
            var isOver32 = (remainder >> 5) == 1;
            if (isOver32)
            {
                // right shift by (64 - remainder)
                // this will remove value already written to first ulong
                //       => (64-r)    eg 64-60 = 4, FEDCBA -> 0000FE
                var v2 = maskedValue >> (64 - remainder);

                // write rest to second ulong
                *(this.ulongPtr + (this.writeBit >> 6) + 1) |= v2;
            }

            this.writeBit = endCount;
        }

        public byte[] ToArray()
        {
            var array = new byte[this.ByteLength];
            this.CopyToArrayMarshal(array, 0);
            return array;
        }

        /// <summary>
        /// Copies unmanged buffer to given array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="offset"></param>
        /// <returns>number of bytes copied</returns>
        public int CopyToArray(byte[] array, int offset)
        {
            this.ThrowIfDisposed();

            fixed (byte* outPtr = &array[offset])
            {
                for (var i = 0; i < ((this.writeBit >> 6) + 1); i++)
                {
                    *(((ulong*)outPtr) + i) = *(this.ulongPtr + i);
                }
            }

            return this.ByteLength;
        }

        /// <summary>
        /// Copies unmanged buffer to given array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="offset"></param>
        /// <returns>number of bytes copied</returns>
        public int CopyToArrayMarshal(byte[] array, int offset)
        {
            this.ThrowIfDisposed();

            var length = this.ByteLength;
            Marshal.Copy(this.intPtr, array, offset, length);

            return length;
        }
    }
}
