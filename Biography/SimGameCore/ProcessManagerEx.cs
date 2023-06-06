using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Biography.SimGameCore
{
    public static class ProcessManagerExHooks
    {
        public static ProcessManagerEx managerEx;

        public static void HookOn(RainWorld rainWorld)
        {
            try
            {
                On.ProcessManager.Update += ProcessManager_Update;
                managerEx = new ProcessManagerEx(rainWorld.processManager);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static void ProcessManager_Update(On.ProcessManager.orig_Update orig, ProcessManager self, float deltaTime)
        {
            try
            {
                orig.Invoke(self, deltaTime);
                if (managerEx != null)
                    managerEx.Update(self, deltaTime);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    public class ProcessManagerEx
    {
        public ProcessManager ProcessManagerRef;
        public SimGame simGame;

        public bool shouldUpdateSideLoopProcess;

        public ProcessManagerEx(ProcessManager processManager)
        {
            ProcessManagerRef = processManager;
        }

        public void Update(ProcessManager manager, float deltaTime)
        {
            if (!shouldUpdateSideLoopProcess)
                return;
            try
            {
                simGame.RawUpdate(deltaTime);
            }
            catch (Exception ex)
            {
                BiographyPlugin.Log($"Error occur when {simGame.ID} trying to raw update,details in exceptionLog");
                Debug.LogException(ex);
            }
        }

        public void RequestNewSimGame()
        {
            shouldUpdateSideLoopProcess = true;
            if (simGame != null)
                ClearOutSideProcesses();
            simGame = new SimGame(ProcessManagerRef);
        }

        public void ClearOutSideProcesses()
        {
            shouldUpdateSideLoopProcess = false;
            simGame.ShutDownProcess();
            simGame = null;
        }
    }
}
