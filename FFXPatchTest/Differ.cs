using System;
using System.Collections.Generic;

namespace FFXPatchTest {
    sealed class Differ {
        // TODO: what the fuck hoomon
        public static IEnumerable<ByteDiff> CreateDiffSet(byte[] original, byte[] changed) {
            var results = new List<ByteDiff>();
            var currentBuffer = new List<byte>();
            uint currentBufferLength = 0;
            uint startOffset = 0;
            for (uint i = 0; i < original.Length; i++) {
                var originalByte = original[i];
                var changedByte = changed[i];

                if (originalByte != changedByte) {
                    if (currentBufferLength == 0) {
                        startOffset = i;
                    }
                    currentBufferLength++;
                    currentBuffer.Add(changedByte);
                } else {
                    if (currentBufferLength == 0)
                        continue;

                    results.Add(new ByteDiff(startOffset, currentBufferLength, currentBuffer.ToArray()));
                    Console.WriteLine($"Got diff at offset {startOffset} - {currentBufferLength}");
                    currentBufferLength = 0;
                    currentBuffer = new List<byte>();
                }
            }
            return results;
        }
    }

    sealed class ByteDiff
    {
        public ByteDiff(uint offset, uint length, Byte[] bytes)
        {
            Offset = offset;
            Length = length;
            Bytes = bytes;
        }
        public uint Offset { get; set; }
        public uint Length { get; set; }
        public Byte[] Bytes { get; set; }
    }
}