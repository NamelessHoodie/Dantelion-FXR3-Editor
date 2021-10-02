using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FFXPatchTest {
    public class FFXReloader {
        public static async Task Reload(byte[] originalFxrByteArray, byte[] changedFxrByteArray) {
            var originalFxr = originalFxrByteArray;
            var headerAob = originalFxr.Take(16).ToArray();
            var patchedFxr = changedFxrByteArray;

            if (originalFxr.Length != patchedFxr.Length) {
                throw new NotImplementedException("Bad human! No changing file sizes!");
            }

            // Find the fucken game
            var processArray = Process.GetProcessesByName("DarkSoulsIII");
            var process = processArray.Single();

            // IMPORTANT: this scan doesn't reliably find the result because I'm lazy and didn't want to wait minutes
            // for the AOB to found. So the starting pointer might need some tweaking (find the appropiate spot with CE)
            // if this AOB fails to find the FXR header.
            // TODO: find static pointer to optimize this shit
            var ffxScanner = new MemoryScanner(process, new IntPtr(0x7FF433000000), 0xfffffff);
            var ffxPtr = ffxScanner.FindPattern(headerAob, "xxxxxxxxxxxxxxxx");
            if (ffxPtr == IntPtr.Zero) {
                throw new Exception("Could not find FFX AOB");
            }

            // Find the reference to this in-memory FXR file
            // var pointerBytes = BitConverter.GetBytes(ffxPtr.ToInt64());
            // var tableScanner = new MemoryScanner(process, new IntPtr(0x7FF433000000), 0xfffffff);
            // var ffxTablePointer = ffxScanner.FindPattern(pointerBytes, "xxxxxxxx");

            var diffSet = Differ.CreateDiffSet(originalFxr, patchedFxr);
            foreach (var diff in diffSet) {
                var ffxPtrRaw = ffxPtr.ToInt64();
                var offsettedPointer = ffxPtrRaw + diff.Offset;

                Kernel32.VirtualProtectEx(process.Handle, new IntPtr(offsettedPointer), diff.Length, Kernel32.Protection.PAGE_EXECUTE_READWRITE, out var oldProtection);
                Kernel32.WriteProcessMemory(process.Handle, offsettedPointer, diff.Bytes, (int) diff.Length, out var numWrite);
                Kernel32.VirtualProtectEx(process.Handle, new IntPtr(offsettedPointer), diff.Length, oldProtection, out var _);
            }
        }
    }
}

