/* Original Code by Josip Medved <jmedved@jmedved.com> * www.medo64.com * MIT License */

//2022-12-01: Compatible with .NET 6 and 7
//2012-11-24: Suppressing bogus CA5122 warning (http://connect.microsoft.com/VisualStudio/feedback/details/729254/bogus-ca5122-warning-about-p-invoke-declarations-should-not-be-safe-critical)
//2010-10-07: Added IsOtherInstanceRunning method
//2008-11-14: Reworked code to use SafeHandle
//2008-04-11: Cleaned code to match FxCop 1.36 beta 2 (SpecifyMarshalingForPInvokeStringArguments, NestedTypesShouldNotBeVisible)
//2008-04-10: NewInstanceEventArgs is not nested class anymore
//2008-01-26: AutoExit parameter changed to NoAutoExit
//2008-01-08: Main method is now called Attach
//2008-01-06: System.Environment.Exit returns E_ABORT (0x80004004)
//2008-01-03: Added Resources
//2007-12-29: New version

using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using JetBrains.Annotations;

namespace Clockmaker0.Data.Medo;

/// <summary>
/// Handles detection and communication of programs multiple instances.
/// This class is thread safe.
/// </summary>
public static class HighlanderValidator
{

    private static Mutex? _mtxFirstInstance;
    private static Thread? _thread;
    private static readonly Lock SyncRoot = new();


    /// <summary>
    /// Returns true if this application is not already started.
    /// Another instance is contacted via named pipe.
    /// </summary>
    /// <param name="noAutoExit">If true, application will exit after informing another instance.</param>
    /// <exception cref="InvalidOperationException">API call failed.</exception>
    public static bool Challenge(bool noAutoExit = false)
    {
        lock (SyncRoot)
        {
            bool isFirstInstance = false;
            try
            {
                _mtxFirstInstance = new Mutex(initiallyOwned: true, @"Global\" + MutexName, out isFirstInstance);
                if (!isFirstInstance)
                {
                    //we need to contact previous instance
                    SingleInstanceArguments contentObject = new()
                    {
                        CommandLine = Environment.CommandLine,
                        CommandLineArgs = Environment.GetCommandLineArgs(),
                    };
                    byte[] contentBytes = JsonSerializer.SerializeToUtf8Bytes(contentObject);
                    using NamedPipeClientStream clientPipe = new(".", MutexName, PipeDirection.Out, PipeOptions.CurrentUserOnly | PipeOptions.WriteThrough);
                    clientPipe.Connect();
                    clientPipe.Write(contentBytes, 0, contentBytes.Length);
                }
                else
                {
                    //there is no application already running.
                    _thread = new Thread(Run)
                    {
                        Name = typeof(HighlanderValidator).FullName,
                        IsBackground = true
                    };
                    _thread.Start();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning(ex.Message + ex.StackTrace);
            }

            if (isFirstInstance || noAutoExit)
            {
                return isFirstInstance;
            }

            Trace.TraceInformation("Exit due to another instance running." + " [" + nameof(HighlanderValidator) + "]");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Environment.Exit(unchecked((int)0x80004004));  // E_ABORT(0x80004004)
            }
            else
            {
                Environment.Exit(114);  // EALREADY(114)
            }

            return isFirstInstance;
        }
    }

    private static string? _mutexName;
    private static string MutexName
    {
        get
        {
            lock (SyncRoot)
            {
                if (_mutexName != null)
                {
                    return _mutexName;
                }

                Assembly? assembly = Assembly.GetEntryAssembly();

                StringBuilder sbMutextName = new();
                string? assName = assembly?.GetName().Name;
                if (assName != null)
                {
                    sbMutextName.Append(assName, 0, Math.Min(assName.Length, 31));
                    sbMutextName.Append('.');
                }

                StringBuilder sbHash = new();
                sbHash.AppendLine(Environment.MachineName);
                sbHash.AppendLine(Environment.UserName);
                if (assembly != null)
                {
                    sbHash.AppendLine(assembly.FullName);
                    sbHash.AppendLine(assembly.Location);
                }
                else
                {
                    string[] args = Environment.GetCommandLineArgs();
                    if (args.Length > 0) { sbHash.AppendLine(args[0]); }
                }
                foreach (byte b in SHA256.HashData(Encoding.UTF8.GetBytes(sbHash.ToString())))
                {
                    if (sbMutextName.Length == 63) { sbMutextName.AppendFormat("{0:X1}", b >> 4); }  // just take the first nubble
                    if (sbMutextName.Length == 64) { break; }
                    sbMutextName.AppendFormat("{0:X2}", b);
                }
                _mutexName = sbMutextName.ToString();
                return _mutexName;
            }
        }
    }

    /// <summary>
    /// Occurs in first instance when new instance is detected.
    /// </summary>
    public static event EventHandler<NewInstanceEventArgs>? ChallengerDetected;


    /// <summary>
    /// Thread function.
    /// </summary>
    private static void Run()
    {
        using NamedPipeServerStream serverPipe = new(MutexName, PipeDirection.In, maxNumberOfServerInstances: 1, PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly | PipeOptions.WriteThrough);
        while (_mtxFirstInstance != null)
        {
            try
            {
                if (!serverPipe.IsConnected)
                {
                    serverPipe.WaitForConnection();
                }
                SingleInstanceArguments? contentObject = JsonSerializer.Deserialize<SingleInstanceArguments>(serverPipe);
                serverPipe.Disconnect();
                if (contentObject != null)
                {
                    ChallengerDetected?.Invoke(null, new NewInstanceEventArgs(contentObject.CommandLine, contentObject.CommandLineArgs));
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning(ex.Message + " [" + nameof(HighlanderValidator) + "]");
                Thread.Sleep(100);
            }
        }
    }


    [Serializable]
    private sealed record SingleInstanceArguments
    {  // just a storage
        [JsonInclude]
        public required string CommandLine;

        [JsonInclude]
        public required string[] CommandLineArgs;
    }
}


/// <summary>
/// Arguments for newly detected application instance.
/// </summary>
public sealed class NewInstanceEventArgs : EventArgs
{
    /// <summary>
    /// Creates new instance.
    /// </summary>
    /// <param name="commandLine">Command line.</param>
    /// <param name="commandLineArgs">String array containing the command line arguments in the same format as Environment.GetCommandLineArgs.</param>
    internal NewInstanceEventArgs(string commandLine, string[] commandLineArgs)
    {
        CommandLine = commandLine;
        _commandLineArgs = new string[commandLineArgs.Length];
        Array.Copy(commandLineArgs, _commandLineArgs, _commandLineArgs.Length);
    }

    /// <summary>
    /// Gets the command line.
    /// </summary>
    public string CommandLine { [UsedImplicitly] get; }

    private readonly string[] _commandLineArgs;
    /// <summary>
    /// Returns a string array containing the command line arguments.
    /// </summary>
    public string[] GetCommandLineArgs()
    {
        string[] argCopy = new string[_commandLineArgs.Length];
        Array.Copy(_commandLineArgs, argCopy, argCopy.Length);
        return argCopy;
    }

    /// <summary>
    /// Gets a string array containing the command line arguments without the name of exectuable.
    /// </summary>
    public string[] Args
    {
        get
        {
            string[] argCopy = new string[_commandLineArgs.Length - 1];
            Array.Copy(_commandLineArgs, 1, argCopy, 0, argCopy.Length);
            return argCopy;
        }
    }

}
