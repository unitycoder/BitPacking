﻿/*
MIT License

Copyright (c) 2021 James Frowen

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using UnityEngine;

namespace JamesFrowen.BitPacking
{
    public sealed class Vector2Packer
    {
        readonly FloatPacker xPacker;
        readonly FloatPacker yPacker;

        public Vector2Packer(Vector2 max, Vector2 precision)
        {
            this.xPacker = new FloatPacker(max.x, precision.x);
            this.yPacker = new FloatPacker(max.y, precision.y);
        }

        public void Pack(NetworkWriter writer, Vector2 value)
        {
            this.xPacker.Pack(writer, value.x);
            this.yPacker.Pack(writer, value.y);
        }

        public Vector2 Unpack(NetworkReader reader)
        {
            Vector2 value = default;
            value.x = this.xPacker.Unpack(reader);
            value.y = this.yPacker.Unpack(reader);
            return value;
        }
    }
}
