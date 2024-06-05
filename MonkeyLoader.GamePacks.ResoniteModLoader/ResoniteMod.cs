using MonkeyLoader;
using MonkeyLoader.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace ResoniteModLoader
{
    /// <summary>
    /// Contains members that only the <see cref="ModLoader"/> or the Mod itself are intended to access.
    /// </summary>
    public abstract class ResoniteMod : ResoniteModBase
    {
        private readonly Lazy<ModConfiguration?> _configuration;

        /// <inheritdoc/>
        protected override ModConfiguration? Configuration => _configuration.Value;

        /// <inheritdoc/>
        protected ResoniteMod()
        {
            _configuration = new(() =>
            {
                if (BuildConfigurationDefinition() is ModConfigurationDefinition definition)
                    return Config.LoadSection(new ModConfiguration(definition));

                return null;
            });
        }

        /// <summary>
        /// Logs the given object as a line in the log if debug logging is enabled.
        /// </summary>
        /// <param name="message">The object to log.</param>
        public static void Debug(object message)
            => GetLoggerFromStackTrace(new(1)).Debug(() => message);

        /// <summary>
        /// Logs the given objects as lines in the log if debug logging is enabled.
        /// </summary>
        /// <param name="messages">The objects to log.</param>
        public static void Debug(params object[] messages)
            => GetLoggerFromStackTrace(new(1)).Debug(Wrap(messages));

        /// <summary>
        /// Logs an object as a line in the log based on the value produced by the given function if debug logging is enabled..
        /// <para/>
        /// This is more efficient than passing an <see cref="object"/> or a <see cref="string"/> directly,
        /// as it won't be generated if debug logging is disabled.
        /// </summary>
        /// <param name="messageProducer">The function generating the object to log.</param>
        public static void DebugFunc(Func<object> messageProducer)
            => GetLoggerFromStackTrace(new(1)).Debug(messageProducer);

        /// <summary>
        /// Logs the given object as an error line in the log.
        /// </summary>
        /// <param name="message">The object to log.</param>
        public static void Error(object message)
            => GetLoggerFromStackTrace(new(1)).Error(Wrap(message));

        /// <summary>
        /// Logs the given objects as error lines in the log.
        /// </summary>
        /// <param name="messages">The objects to log.</param>
        public static void Error(params object[] messages)
            => GetLoggerFromStackTrace(new(1)).Error(Wrap(messages));

        /// <summary>
        /// Gets whether debug logging is enabled.
        /// </summary>
        /// <returns><c>true</c> if debug logging is enabled.</returns>
        public static bool IsDebugEnabled()
            => GetLoggerFromStackTrace(new(1)).Level >= LoggingLevel.Debug;

        /// <summary>
        /// Logs the given object as a regular line in the log.
        /// </summary>
        /// <param name="message">The object to log.</param>
        public static void Msg(object message)
            => GetLoggerFromStackTrace(new(1)).Info(Wrap(message));

        /// <summary>
        /// Logs the given objects as regular lines in the log.
        /// </summary>
        /// <param name="messages">The objects to log.</param>
        public static void Msg(params object[] messages)
            => GetLoggerFromStackTrace(new(1)).Info(Wrap(messages));

        /// <summary>
        /// Logs the given object as a warning line in the log.
        /// </summary>
        /// <param name="message">The object to log.</param>
        public static void Warn(object message)
            => GetLoggerFromStackTrace(new(1)).Warn(Wrap(message));

        /// <summary>
        /// Logs the given objects as warning lines in the log.
        /// </summary>
        /// <param name="messages">The objects to log.</param>
        public static void Warn(params object[] messages)
            => GetLoggerFromStackTrace(new(1)).Warn(Wrap(messages));

        /// <summary>
        /// Define this mod's configuration via a builder
        /// </summary>
        /// <param name="builder">A builder you can use to define the mod's configuration</param>
        public virtual void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
        { }

        /// <summary>
        /// Defines handling of incompatible configuration versions
        /// </summary>
        /// <param name="serializedVersion">Configuration version read from the config file</param>
        /// <param name="definedVersion">Configuration version defined in the mod code</param>
        /// <returns></returns>
        public virtual IncompatibleConfigurationHandlingOption HandleIncompatibleConfigurationVersions(Version serializedVersion, Version definedVersion)
            => IncompatibleConfigurationHandlingOption.ERROR;

        /// <summary>
        /// Called once immediately after ResoniteModLoader begins execution
        /// </summary>
        public virtual void OnEngineInit()
        { }

        /// <inheritdoc/>
        public override bool Run()
        {
            try
            {
                OnEngineInit();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(() => ex.Format($"Error while intitializing RML Mod {Name}:"));
                return false;
            }
        }

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

        /// <summary>
        /// Build the defined configuration for this mod.
        /// </summary>
        /// <returns>This mod's configuration definition.</returns>
        internal ModConfigurationDefinition? BuildConfigurationDefinition()
        {
            ModConfigurationDefinitionBuilder builder = new(this);
            builder.ProcessAttributes();
            DefineConfiguration(builder);
            return builder.Build();
        }

        private static Func<object> Wrap(object message) => () => message;

        private static IEnumerable<Func<object>> Wrap(IEnumerable<object> messages)
            => messages.Select(Wrap);
    }
}