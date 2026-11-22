using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace BoardSimulator.Runtime
{
    [SupportedOSPlatform("windows")]
    internal sealed class WindowsWorkerJob : IDisposable
    {
        private const uint KillOnJobClose = 0x00002000;
        private const int ExtendedLimitInformationClass = 9;

        private readonly SafeFileHandle _handle;

        public WindowsWorkerJob()
        {
            _handle = CreateJobObjectW(IntPtr.Zero, null);

            if (_handle.IsInvalid)
            {
                int error = Marshal.GetLastWin32Error();
                _handle.Dispose();
                throw new Win32Exception(error);
            }

            try
            {
                ExtendedLimitInformation limits = new();
                limits.BasicLimitInformation.LimitFlags = KillOnJobClose;

                if (!SetInformationJobObject(
                    _handle,
                    ExtendedLimitInformationClass,
                    ref limits,
                    (uint)Marshal.SizeOf<ExtendedLimitInformation>()))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            catch
            {
                _handle.Dispose();
                throw;
            }
        }

        public void AddProcess(Process process)
        {
            if (!AssignProcessToJobObject(_handle, process.SafeHandle))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public void Dispose()
        {
            _handle.Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BasicLimitInformation
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IoCounters
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ExtendedLimitInformation
        {
            public BasicLimitInformation BasicLimitInformation;
            public IoCounters IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern SafeFileHandle CreateJobObjectW(
            IntPtr attributes,
            [MarshalAs(UnmanagedType.LPWStr)] string? name);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetInformationJobObject(
            SafeFileHandle job,
            int informationClass,
            ref ExtendedLimitInformation information,
            uint informationLength);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AssignProcessToJobObject(
            SafeFileHandle job,
            SafeProcessHandle process);
    }
}
