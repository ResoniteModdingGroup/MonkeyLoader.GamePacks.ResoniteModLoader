using MonkeyLoader.Logging;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace ResoniteModLoader;

internal static class Util
{
    /// <summary>
    /// Get the Logger for the mod from a stack trace.
    /// </summary>
    /// <param name="stackTrace">A stack trace captured by the callee</param>
    /// <returns>The executing mod's Logger, or ModLoader.Logger if none found</returns>
    internal static Logger GetLoggerFromStackTrace(StackTrace stackTrace)
    {
        for (int i = 0; i < stackTrace.FrameCount; i++)
        {
            Assembly? assembly = stackTrace.GetFrame(i)?.GetMethod()?.DeclaringType?.Assembly;

            if (assembly != null)
            {
                if (RmlMod.AssemblyLookupMap.TryGetValue(assembly, out var mod))
                {
                    return mod.Logger;
                }
            }
        }
        return ModLoader.Logger;
    }
}