using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Clockmaker0.Data;

static class TaskManager
{
    public static void ScheduleTask(Func<Task> callback, [CallerMemberName] string? memberName = null, [CallerLineNumber] int line = 0, [CallerFilePath] string? path = null)
    {
        Dispatcher.UIThread.Invoke(callback).ContinueWith(t => HandleResult(t, memberName, line, path));
    }

    public static void ScheduleAsyncTask(Func<Task> callback, [CallerMemberName] string? memberName = null, [CallerLineNumber] int line = 0, [CallerFilePath] string? path = null)
    {
        Dispatcher.UIThread.InvokeAsync(callback).ContinueWith(t => HandleResult(t, memberName, line, path));
    }

    private static void HandleResult(Task task, string? memberName, int line, string? path)
    {
        if (task.IsFaulted)
        {
            throw new Exception($"Task Faulted from {memberName}. Line: {line}. File: {path}",
                task.Exception.InnerException ?? task.Exception);
        }
    }
}