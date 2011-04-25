using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;

namespace InfoStrat.MotionFx
{
    internal class NativeInterop
    {
        [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
        public static extern void MoveMemory(IntPtr dest, IntPtr src, int size);

        public unsafe delegate void MemCpyImpl(byte* src, byte* dest, int len);
        public static MemCpyImpl memcpyimpl = (MemCpyImpl)Delegate.CreateDelegate(
                                        typeof(MemCpyImpl),
                                        typeof(Buffer).GetMethod("memcpyimpl",
                                            BindingFlags.Static | BindingFlags.NonPublic));
    }
}
