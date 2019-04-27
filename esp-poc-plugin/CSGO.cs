using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using SlimDX;


namespace esp_poc_plugin
{
    public class CSGO
    {
        public static int dwViewMatrix = 0x4CD6F04;
        public static int dwLocalPlayer = 0xCD4774;
        public static int dwEntityList = 0x4CE54EC;

        private static int GameWidth = 0;
        private static int GameHeight = 0;
        private static IntPtr gameHandle = new IntPtr(0);
        public static IntPtr clientBase = new IntPtr(0);

        public static bool OpenTheGates(string gameName, string moduleName, int gameWidth, int gameHeight)
        {
            GameWidth = gameWidth;
            GameHeight = gameHeight;
            Process[] p = Process.GetProcessesByName(gameName);
            if (p.Length > 0)
            {
                foreach (Module m in GetProcessModules(p[0]))
                {
                    if (m.ModuleName == moduleName)
                    {
                        gameHandle = Native.OpenProcess(Native.DesiredAccess, false, p[0].Id);
                        clientBase = m.BaseAddress;
                        if (gameHandle != IntPtr.Zero && clientBase != IntPtr.Zero)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public static List<Module> GetProcessModules(Process process)
        {
            List<Module> collectedModules = new List<Module>();

            IntPtr[] modulePointers = new IntPtr[0];
            int bytesNeeded = 0;

            // Determine number of modules
            if (!Native.EnumProcessModulesEx(process.Handle, modulePointers, 0, out bytesNeeded, (uint)Native.ModuleFilter.ListModulesAll))
            {
                return collectedModules;
            }

            int totalNumberofModules = bytesNeeded / IntPtr.Size;
            modulePointers = new IntPtr[totalNumberofModules];

            // Collect modules from the process
            if (Native.EnumProcessModulesEx(process.Handle, modulePointers, bytesNeeded, out bytesNeeded, (uint)Native.ModuleFilter.ListModulesAll))
            {
                for (int index = 0; index < totalNumberofModules; index++)
                {
                    StringBuilder moduleFilePath = new StringBuilder(1024);
                    Native.GetModuleFileNameEx(process.Handle, modulePointers[index], moduleFilePath, (uint)(moduleFilePath.Capacity));

                    string moduleName = Path.GetFileName(moduleFilePath.ToString());
                    Native.ModuleInformation moduleInformation = new Native.ModuleInformation();
                    Native.GetModuleInformation(process.Handle, modulePointers[index], out moduleInformation, (uint)(IntPtr.Size * (modulePointers.Length)));

                    // Convert to a normalized module and add it to our list
                    Module module = new Module(moduleName, moduleInformation.lpBaseOfDll, moduleInformation.SizeOfImage);
                    collectedModules.Add(module);
                }
            }

            return collectedModules;
        }

        public static T ReadProcessMemory<T>(uint address) where T : struct
        {
            T retVal = new T();
            ProcessMemoryUtilities.Memory.ProcessMemory.ReadProcessMemory<T>(gameHandle, (IntPtr)address, ref retVal);
            return retVal;
        }
        public static T ReadProcessMemory<T>(IntPtr address) where T : struct
        {
            T retVal = new T();
            ProcessMemoryUtilities.Memory.ProcessMemory.ReadProcessMemory<T>(gameHandle, address, ref retVal);
            return retVal;
        }
        public static T[] ReadProcessMemoryArray<T>(IntPtr address, int length) where T : struct
        {
            T[] retArray = new T[length];
            ProcessMemoryUtilities.Memory.ProcessMemory.ReadProcessMemoryArray<T>(gameHandle, address, retArray);
            return retArray;
        }

        public static Vector3 GetHeadOrigin(uint address)
        {
            byte[] buffer = ReadProcessMemoryArray<byte>((IntPtr)address, 7 * 0x30 + 0x30); // Bone index * struct size + struct size

            return new Vector3(BitConverter.ToSingle(buffer, 7 * 0x30 + 0x0C), BitConverter.ToSingle(buffer, 7 * 0x30 + 0x1C), BitConverter.ToSingle(buffer, 7 * 0x30 + 0x2C));
        }

        public static bool WorldToScreen(Matrix viewMatrix, Vector3 objLoc, out Vector2 objectScreenLocation)
        {
            float w = 0.0f;

            objectScreenLocation.X = viewMatrix.M11 * objLoc.X + viewMatrix.M12 * objLoc.Y + viewMatrix.M13 * objLoc.Z + viewMatrix.M14;
            objectScreenLocation.Y = viewMatrix.M21 * objLoc.X + viewMatrix.M22 * objLoc.Y + viewMatrix.M23 * objLoc.Z + viewMatrix.M24;

            w = viewMatrix.M41 * objLoc.X + viewMatrix.M42 * objLoc.Y + viewMatrix.M43 * objLoc.Z + viewMatrix.M44;

            if (w < 0.01f)
                return false;

            objectScreenLocation.X *= (1.0f / w);
            objectScreenLocation.Y *= (1.0f / w);

            int width = GameWidth;
            int height = GameHeight;

            float x = width / 2;
            float y = height / 2;

            x += 0.5f * objectScreenLocation.X * width + 0.5f;
            y -= 0.5f * objectScreenLocation.Y * height + 0.5f;

            objectScreenLocation.X = x;
            objectScreenLocation.Y = y;

            if (x <= GameWidth + 50 && x >= -50)
            {
                return true;
            }
            return false;
        }

        public static Vector2 WorldToScreen(Matrix viewMatrix, Vector3 objLoc)
        {
            Vector2 objectScreenLocation = new Vector2();

            float w = 0.0f;

            objectScreenLocation.X = viewMatrix.M11 * objLoc.X + viewMatrix.M12 * objLoc.Y + viewMatrix.M13 * objLoc.Z + viewMatrix.M14;
            objectScreenLocation.Y = viewMatrix.M21 * objLoc.X + viewMatrix.M22 * objLoc.Y + viewMatrix.M23 * objLoc.Z + viewMatrix.M24;

            w = viewMatrix.M41 * objLoc.X + viewMatrix.M42 * objLoc.Y + viewMatrix.M43 * objLoc.Z + viewMatrix.M44;

            if (w < 0.01f)
                return new Vector2(0, 0);

            objectScreenLocation.X *= (1.0f / w);
            objectScreenLocation.Y *= (1.0f / w);

            int width = GameWidth;
            int height = GameHeight;

            float x = width / 2;
            float y = height / 2;

            x += 0.5f * objectScreenLocation.X * width + 0.5f;
            y -= 0.5f * objectScreenLocation.Y * height + 0.5f;

            objectScreenLocation.X = x;
            objectScreenLocation.Y = y;

            return objectScreenLocation;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Entity
    {
        [FieldOffset(237)]
        public bool m_bDormant; //0x00ED

        [FieldOffset(244)]
        public int m_iTeamNum; //0x00F4

        [FieldOffset(256)]
        public int m_iHealth; //0x0100

        [FieldOffset(312)]
        public Vector3 m_vecOrigin; //0x0138

        [FieldOffset(757)]
        public bool m_lifeState; //0x02F5

        [FieldOffset(9896)]
        public uint m_dwBoneMatrix; //0x26A8
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    struct EntityListItem
    {
        [FieldOffset(0)]
        public uint _entity; //0x0000
    }
}
