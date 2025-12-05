using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ResoniteModLoader
{
    /// <summary>
    /// Dummy execution hook class for compatibility.
    /// </summary>
    /// <remarks>
    /// Why is this public now? :C
    /// </remarks>
    public class ExecutionHook : IPlatformConnector
    {
#pragma warning disable CS1591
        public PlatformInterface Platform { get; private set; } = null!;
        public int Priority => -10;
        public string PlatformName => "ResoniteModLoader";
        public string Username => null!;
        public string PlatformUserId => null!;
        public bool IsPlatformNameUnique => false;

        public void SetCurrentStatus(World world, bool isPrivate, int totalWorldCount)
        { }

        public void ClearCurrentStatus()
        { }

        public void Update()
        { }

        public void Dispose()
            => GC.SuppressFinalize(this);

        public void NotifyOfLocalUser(User user)
        { }

        public void NotifyOfFile(string file, string name)
        { }

        public void NotifyOfScreenshot(World world, string file, ScreenshotType type, DateTime time)
        { }

        public Task<bool> Initialize(PlatformInterface platformInterface)
        {
            ModLoader.Logger.Debug(() => "Initialize() from platformInterface");
            Platform = platformInterface;
            return Task.FromResult(true);
        }

        // [ModuleInitializer]
        public static void Init()
        { }
    }
}