using System.Collections.Generic;
using System.Threading;

using SlimDX;

namespace esp_poc_plugin
{
    public class EspPocPlugin : CSGO
    {
        public bool Initialize(int width, int height)
        {
            IsInitialized = true;
            return OpenTheGates("csgo", "client_panorama.dll", width, height);
        }

        public void StartThread()
        {
            IsRunning = true;

            Thread monkaThread = new Thread(new ThreadStart(MonkaThread));
            monkaThread.IsBackground = true;
            monkaThread.Start();
        }

        public void StopThread()
        {
            IsRunning = false;
        }

        private void MonkaThread()
        {
            List<int[]> boxList = new List<int[]>();

            while (IsRunning)
            {
                boxList.Clear();
                Matrix viewMatrix = ReadProcessMemory<Matrix>(clientBase + dwViewMatrix);

                uint _localPlayer = ReadProcessMemory<uint>(clientBase + dwLocalPlayer);
                Entity localPlayer = ReadProcessMemory<Entity>(_localPlayer);

                EntityListItem[] entityList = ReadProcessMemoryArray<EntityListItem>(clientBase + dwEntityList, 64);

                for (int i = 0; i < entityList.Length; i++)
                {
                    if (entityList[i]._entity == 0)
                    {
                        continue;
                    }
                    Entity entity = ReadProcessMemory<Entity>(entityList[i]._entity);

                    if (entity.m_iTeamNum == localPlayer.m_iTeamNum || entity.m_iHealth <= 0 || entity.m_lifeState || entity.m_bDormant)
                    {
                        continue;
                    }

                    Vector2 entityScreenLocation;
                    if (WorldToScreen(viewMatrix, entity.m_vecOrigin, out entityScreenLocation))
                    {
                        Vector3 entityHeadOrigin = new Vector3(entity.m_vecOrigin.X, entity.m_vecOrigin.Y, ReadProcessMemory<float>(entity.m_dwBoneMatrix + 7 * 0x30 + 0x2C)); // bone Index * sctruct size + Z-offset

                        Vector2 headScreenLocation = WorldToScreen(viewMatrix, entityHeadOrigin);
                        int width = (int)((entityScreenLocation.Y - headScreenLocation.Y) * 0.6);
                        int[] boxData = new int[5] { (int)headScreenLocation.X - width / 2, (int)headScreenLocation.Y, (int)(entityScreenLocation.Y - headScreenLocation.Y), width, 75 }; //headX, headY, height, width, health
                        boxList.Add(boxData);
                    }
                }
                BoxArray = boxList.ToArray();

                Thread.Sleep(1);
            }
        }

        public int[][] BoxArray { get; private set; } = new int[0][];

        public bool IsRunning { get; private set; } = false;

        public bool IsInitialized { get; private set; } = false;
    }
}
