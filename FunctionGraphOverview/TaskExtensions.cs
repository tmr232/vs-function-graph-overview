using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FunctionGraphOverview
{
    internal static class TaskExtensions
    {
        /// <summary>
        /// Observes the task so that unhandled exceptions are logged instead of
        /// silently swallowed or crashing the process.
        /// </summary>
        public static void FireAndForget(this Task task)
        {
            task.ContinueWith(
                t =>
                    Debug.WriteLine(
                        $"[FunctionGraphOverview] Fire-and-forget task faulted: {t.Exception}"
                    ),
                TaskContinuationOptions.OnlyOnFaulted
            );
        }
    }
}
