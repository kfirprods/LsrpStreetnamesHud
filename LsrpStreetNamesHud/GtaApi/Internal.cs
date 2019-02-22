/*
 * This is a C# version of the open source SA-MP UDF AHK
 * Source: https://github.com/SAMP-UDF/SAMP-UDF-for-AutoHotKey/blob/master/SAMP.ahk
 * Translation by Doakes
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace LsrpStreetNamesHud.GtaApi
{
    internal class Internal
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int dwSize,
            ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int nSize,
            ref int lpNumberOfBytesWritten);

        [DllImport("kernel32")]
        public static extern IntPtr CreateRemoteThread(
          IntPtr hProcess,
          IntPtr lpThreadAttributes,
          uint dwStackSize,
          IntPtr lpStartAddress, // raw Pointer into remote process
          IntPtr lpParameter,
          uint dwCreationFlags,
          out uint lpThreadId
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize,
            uint flNewProtect, out uint lpflOldProtect);

        [Flags]
        public enum FreeType
        {
            Decommit = 0x4000,
            Release = 0x8000,
        }
        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        // Page protection consts
        const uint PAGE_EXECUTE_READWRITE = 0x40;


        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress,
            int dwSize, FreeType dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            int dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        static IntPtr[] pParam = new IntPtr[5];

        public static IntPtr pInjectFunc = IntPtr.Zero,
            hGTA = IntPtr.Zero,
            pMemory = IntPtr.Zero,
            dwSAMP = IntPtr.Zero;

        public static int sampVersion = 0;
        static int dwGTAPID = 0;

        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern int EnumProcessModules(IntPtr hProcess, [Out] IntPtr lphModule, uint cb, out uint lpcbNeeded);

        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, uint nSize);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct InputInfo
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5340, ArraySubType = UnmanagedType.I1)]
            public char[] pad5340;
            public int f5340;
        }

        static readonly Dictionary<string, int> TypeSizes = new Dictionary<string, int>
        {
            { "Int", 4 },
            { "UInt", 4 },
            { "Char", 1 },
            { "UChar", 1 },
            { "Short", 2 },
            { "UShort", 2 },
        };

        public static void writeRaw(IntPtr hProcess, IntPtr dwAddress, byte[] pBuffer, int dwLen)
        {
            int lpNumberOfBytesWritten = 0;
            WriteProcessMemory(hProcess, dwAddress, pBuffer, dwLen, ref lpNumberOfBytesWritten);
        }

        public static byte[] readMem(IntPtr hProcess, IntPtr dwAddress, int dwLen)
        {
            byte[] dwRead = new byte[dwLen];
            int bytesRead = 0;
            var success = ReadProcessMemory(hProcess, dwAddress, dwRead, dwLen, ref bytesRead);
            if (!success)
            {
                // TODO: Propagate error to user
            }

            return dwRead;
        }

        public static float readFloat(IntPtr hProcess, IntPtr dwAddress)
        {
            var bytes = readMem(hProcess, dwAddress, 4);
            return BitConverter.ToSingle(bytes, 0);
        }

        public static int readDWORD(IntPtr hProcess, IntPtr dwAddress)
        {
            byte[] bytes = readMem(hProcess, dwAddress, 4);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static void writeString(IntPtr hProcess, IntPtr dwAddress, string wString)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(wString);
            int bytesWritten = 0;
            WriteProcessMemory(hProcess, dwAddress, buffer, buffer.Length, ref bytesWritten);
        }

        private static void NumPut(int num, ref byte[] arr, int startPos)
        {
            byte[] buff = BitConverter.GetBytes(num);
            for (int i = startPos, j = 0; i < startPos + buff.Length; i++, j++)
            {
                arr[i] = buff[j];
            }
        }

        private static void NumPut(uint num, ref byte[] arr, int startPos)
        {
            byte[] buff = BitConverter.GetBytes(num);
            for (int i = startPos, j = 0; i < startPos + buff.Length; i++, j++)
            {
                arr[i] = buff[j];
            }
        }

        private static void NumPut(char num, ref byte[] arr, int startPos)
        {
            byte[] buff = BitConverter.GetBytes(num);
            for (int i = startPos, j = 0; i < startPos + buff.Length; i++, j++)
            {
                arr[i] = buff[j];
            }
        }

        private static void NumPut(byte num, ref byte[] arr, int startPos)
        {
            arr[startPos] = num;
        }

        private static void NumPut(short num, ref byte[] arr, int startPos)
        {
            byte[] buff = BitConverter.GetBytes(num);
            for (int i = startPos, j = 0; i < startPos + buff.Length; i++, j++)
            {
                arr[i] = buff[j];
            }
        }


        private static void NumPut(ushort num, ref byte[] arr, int startPos)
        {
            byte[] buff = BitConverter.GetBytes(num);
            for (int i = startPos, j = 0; i < startPos + buff.Length; i++, j++)
            {
                arr[i] = buff[j];
            }
        }

        public static void callWithParams(IntPtr hProcess, IntPtr dwFunc, Parameter[] aParams, bool bCleanupStack = true)
        {
            int validParams = 0;

            // i * PUSh + CALL + RETN
            int i = aParams.Length;
            int dwLen = i * 5 + 5 + 1;

            if (bCleanupStack)
                dwLen += 3;

            byte[] injectData = new byte[i * 5 + 5 + 3 + 1];

            int i_ = 0;
            for (i = aParams.Length - 1; i >= 0; i--)
            {
                if (aParams[i].Name != "")
                {
                    IntPtr dwMemAddress = IntPtr.Zero;
                    if (aParams[i].Name == "p")
                    {
                        dwMemAddress = (IntPtr)aParams[i].Value;
                    }
                    else if (aParams[i].Name == "i")
                    {
                        dwMemAddress = new IntPtr((int)aParams[i].Value);
                    }
                    else if (aParams[i].Name == "s")
                    {
                        if (i_ > 2)
                            return;

                        dwMemAddress = pParam[i_];
                        writeString(hProcess, dwMemAddress, (string)aParams[i].Value);
                        i_++;
                    }
                    else
                    {
                        return;
                    }
                    NumPut((byte)0x68, ref injectData, validParams * 5);
                    NumPut(dwMemAddress.ToInt32(), ref injectData, validParams * 5 + 1);
                    validParams++;
                }
            }

            IntPtr offset = IntPtr.Subtract(dwFunc, IntPtr.Add(pInjectFunc, validParams * 5 + 5).ToInt32());
            NumPut((byte)0xE8, ref injectData, validParams * 5);
            NumPut(offset.ToInt32(), ref injectData, validParams * 5 + 1);

            if (bCleanupStack)
            {
                NumPut((ushort)0xC483, ref injectData, validParams * 5 + 5);
                NumPut((byte)(validParams * 4), ref injectData, validParams * 5 + 7);

                NumPut((byte)0xC3, ref injectData, validParams * 5 + 8);
            }
            else
            {
                NumPut((byte)0xC3, ref injectData, validParams * 5 + 5);
            }

            writeRaw(hGTA, pInjectFunc, injectData, dwLen);

            uint threadID = 0;
            IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0,
                pInjectFunc, IntPtr.Zero, 0, out threadID);

            WaitForSingleObject(hThread, 0xFFFFFFFF);

            CloseHandle(hThread);
        }

        private static bool refreshSAMP()
        {
            dwSAMP = getModuleBaseAddress("samp.dll", hGTA);
            var versionByte = readMem(hGTA, dwSAMP + 0x1036, 1)[0];
            sampVersion = versionByte == 0xD8 ? 1 : (versionByte == 0xA8 ? 2 : (versionByte == 0x78 ? 3 : 0));
            sampVersion -= 1;

            return true;
        }

        private static bool _handleCheckResult;
        private static DateTime _lastHandleCheck = DateTime.MinValue;

        /// <param name="gtaProcessId">Passing the PID of the GTA process will significantly improve the performance of this function</param>
        public static bool checkHandles(int? gtaProcessId = null)
        {
            // Due to the high frequency of checkHandles invocation, we cache positive results for up to 15 seconds
            if (_handleCheckResult && (DateTime.Now - _lastHandleCheck).TotalSeconds < 15)
            {
                return true;
            }

             _handleCheckResult = refreshGTA(gtaProcessId) &&
                refreshMemory() && 
                refreshSAMP();
            _lastHandleCheck = DateTime.Now;

            return _handleCheckResult;
        }

        private static void resetPointers()
        {
            hGTA = IntPtr.Zero;
            dwGTAPID = 0;
            pMemory = IntPtr.Zero;
            dwSAMP = IntPtr.Zero;
        }

        private static bool refreshGTA(int? gtaProcessId)
        {
            var newPid = gtaProcessId ?? getPID("GTA:SA:MP");

            if (newPid < 0)
            {
                if (hGTA != IntPtr.Zero)
                {
                    VirtualFreeEx(hGTA, pMemory, 0, FreeType.Release);
                }

                resetPointers();
                return false;
            }

            if (hGTA == IntPtr.Zero || dwGTAPID != newPid)
            {
                hGTA = OpenProcess(0x1F0FFF, false, newPid);

                dwGTAPID = newPid;
                pMemory = IntPtr.Zero;
            }

            return true;
        }

        private static bool refreshMemory()
        {
            pMemory = VirtualAllocEx(hGTA, IntPtr.Zero, 6144,
                AllocationType.Commit | AllocationType.Reserve,
                MemoryProtection.ExecuteReadWrite);

            for (int i = 0; i < pParam.Length; i++)
            {
                pParam[i] = pMemory + i * 1024;
            }
            pInjectFunc = pParam[pParam.Length - 1] + 1024;

            return true;
        }

        private static IntPtr getModuleBaseAddress(string moduleName, IntPtr hProcess)
        {

            // Setting up the variable for the second argument for EnumProcessModules
            IntPtr[] hMods = new IntPtr[1024];

            GCHandle gch = GCHandle.Alloc(hMods, GCHandleType.Pinned); // Don't forget to free this later
            IntPtr pModules = gch.AddrOfPinnedObject();

            // Setting up the rest of the parameters for EnumProcessModules
            uint uiSize = (uint)(Marshal.SizeOf(typeof(IntPtr)) * (hMods.Length));
            uint cbNeeded = 0;

            if (EnumProcessModules(hProcess, pModules, uiSize, out cbNeeded) == 1)
            {
                Int32 uiTotalNumberofModules = (Int32)(cbNeeded / (Marshal.SizeOf(typeof(IntPtr))));

                for (int i = 0; i < (int)uiTotalNumberofModules; i++)
                {
                    StringBuilder strbld = new StringBuilder(1024);
                    GetModuleFileNameEx(hProcess, hMods[i], strbld, (uint)(strbld.Capacity));

                    if (System.IO.Path.GetFileName(strbld.ToString()).Equals(moduleName))
                        return hMods[i];
                }
            }

            // Must free the GCHandle object
            gch.Free();

            // TODO: Raise module not found error, then catch it in parent
            return IntPtr.Zero;
        }

        private static int getPID(string windowTitle)
        {
            var processes = Process.GetProcesses().Where(p => !string.IsNullOrEmpty(p.MainWindowTitle)).ToList();
            foreach (var process in processes)
            {
                var id = process.Id;
                var title = process.MainWindowTitle;

                if (title.Equals(windowTitle))
                    return id;
            }

            return -1;
        }
    }

    class Parameter
    {
        public string Name;
        public object Value;

        public Parameter(string name, object value)
        {
            this.Name = name;
            this.Value = value;
        }
    }
}
