using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace CloneHeroReaderClient
{


    class Program
    {
        [DllImport("Kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, UInt32 nSize, ref UInt32 lpNumberOfBytesRead);

        public static void Main(string[] args)
        {
            //Process.Start();
            
        }

        public byte[] Read(IntPtr handle, IntPtr address, UInt32 size, ref UInt32 bytes)
        {
            byte[] buffer = new byte[size];
            ReadProcessMemory(handle, address, buffer, size, ref bytes);
            return buffer;
        }
    }
}
