using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FFXPatchTest {
    // Fucken stolen from somewhere idk anymore
    public sealed class MemoryScanner {
        private readonly IntPtr _vAddress;
        private readonly Process _vProcess;
        private readonly ulong _vSize;

        private byte[] _vDumpedRegion;

        public MemoryScanner(Process proc, IntPtr addr, ulong size) {
            _vProcess = proc;
            _vAddress = addr;
            _vSize = size;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            ulong dwSize,
            out int lpNumberOfBytesRead
        );

        private bool DumpMemory() {
            try {
                // Checks to ensure we have valid data.
                if (_vProcess == null)
                    return false;
                if (_vProcess.HasExited)
                    return false;
                if (_vAddress == IntPtr.Zero)
                    return false;
                if (_vSize == 0)
                    return false;

                _vDumpedRegion = new byte[_vSize];

                int nBytesRead;

                var ret = ReadProcessMemory(
                    _vProcess.Handle, _vAddress, _vDumpedRegion, _vSize, out nBytesRead
                );

                return nBytesRead > 0;
            } catch (Exception) {
                return false;
            }
        }

        private bool MaskCheck(int nOffset, IEnumerable<byte> btPattern, string strMask) {
            // Loop the pattern and compare to the mask and dump.
            return !btPattern.Where((t, x) =>
                strMask[x] != '?' && strMask[x] == 'x' && t != _vDumpedRegion[nOffset + x]).Any();

            // The loop was successful so we found the pattern.
        }

        public IntPtr FindPattern(byte[] btPattern, string strMask) {
            try {
                // Dump the memory region if we have not dumped it yet.
                if (_vDumpedRegion == null || _vDumpedRegion.Length == 0)
                    if (!DumpMemory())
                        return IntPtr.Zero;

                // Ensure the mask and pattern lengths match.
                if (strMask.Length != btPattern.Length)
                    return IntPtr.Zero;

                // Loop the region and look for the pattern.
                for (var x = 0; x < _vDumpedRegion.Length; x++)
                    if (MaskCheck(x, btPattern, strMask)) // The pattern was found, return it.
                        return new IntPtr((long) _vAddress + x);

                // Pattern was not found.
                return IntPtr.Zero;
            } catch (Exception) {
                return IntPtr.Zero;
            }
        }

        public void ResetRegion() {
            _vDumpedRegion = null;
        }
    }
}