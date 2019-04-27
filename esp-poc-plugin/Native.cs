using System;
using System.Text;

using System.Runtime.InteropServices;


namespace esp_poc_plugin
{
    public class Native
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct ModuleInformation
        {
            public IntPtr lpBaseOfDll;
            public uint SizeOfImage;
            public IntPtr EntryPoint;
        }

        internal enum ModuleFilter
        {
            ListModulesDefault = 0x0,
            ListModules32Bit = 0x01,
            ListModules64Bit = 0x02,
            ListModulesAll = 0x03,
        }

        [DllImport("psapi.dll")]
        public static extern bool EnumProcessModulesEx(IntPtr hProcess, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U4)] [In][Out] IntPtr[] lphModule, int cb, [MarshalAs(UnmanagedType.U4)] out int lpcbNeeded, uint dwFilterFlag);

        [DllImport("psapi.dll")]
        public static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, [In] [MarshalAs(UnmanagedType.U4)] uint nSize);

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out ModuleInformation lpmodinfo, uint cb);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        const int PROCESS_WM_READ = 0x0010;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_OPERATION = 0x0008;
        public const int DesiredAccess = (PROCESS_WM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION);
    }

    public class Module
    {
        public Module(string moduleName, IntPtr baseAddress, uint size)
        {
            this.ModuleName = moduleName;
            this.BaseAddress = baseAddress;
            this.Size = size;
        }

        public string ModuleName { get; set; }
        public IntPtr BaseAddress { get; set; }
        public uint Size { get; set; }
    }
}



