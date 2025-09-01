using FrooxEngine;
using MonkeyLoader.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ResoniteModLoader
{
    // Why is this public
    public class ExecutionHook : IPlatformConnector
    {

#pragma warning disable CS1591
        public PlatformInterface Platform { get; private set; }
        public int Priority => -10;
        public string PlatformName => "ResoniteModLoader";
        public string Username => null;
        public string PlatformUserId => null;
        public bool IsPlatformNameUnique => false;
        public void SetCurrentStatus(World world, bool isPrivate, int totalWorldCount) { }
        public void ClearCurrentStatus() { }
        public void Update() { }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        public void NotifyOfLocalUser(User user) { }
        public void NotifyOfFile(string file, string name) { }
        public void NotifyOfScreenshot(World world, string file, ScreenshotType type, DateTime time) { }

        public async Task<bool> Initialize(PlatformInterface platformInterface)
        {
            ResoniteMod.GetLoggerFromStackTrace(new(1)).Debug(() => "Initialize() from platformInterface");
            Platform = platformInterface;
            return true;
        }
#pragma warning restore CS1591

#pragma warning disable CA2255
        /// <summary>
        /// One method that can start the static constructor of the mod loader.
        /// </summary>
        [ModuleInitializer]
        public static void Init()
        {
            ResoniteMod.GetLoggerFromStackTrace(new(1)).Debug(() => "Init() from ModuleInitializer");
        }
#pragma warning restore CA2255

    }
}
