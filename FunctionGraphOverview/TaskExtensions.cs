using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace FunctionGraphOverview
{
    internal static class TaskExtensions
    {
        /// <summary>
        /// Observes the task so that unhandled exceptions are logged to the
        /// Output Window instead of silently swallowed or crashing the process.
        /// </summary>
        public static void FireAndForget(this Task task)
        {
            task.ContinueWith(
                t =>
                {
                    ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        LogService.Log(t.Exception);
                    });
                },
                TaskContinuationOptions.OnlyOnFaulted
            );
        }
    }
}
