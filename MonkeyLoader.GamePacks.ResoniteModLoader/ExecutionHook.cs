using FrooxEngine;
using MonkeyLoader.Logging;
using MonkeyLoader.Meta;
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
            ModLoader.Logger.Debug(() => "Initialize() from platformInterface");
            Platform = platformInterface;
            return true;
        }
#pragma warning restore CS1591

#pragma warning disable CA2255
        [ModuleInitializer]
        public static void Init()
        {
            //ModLoader.Logger.Debug(() => "Init() from ModuleInitializer"); // throws for some reason
        }
#pragma warning restore CA2255

    }
}
