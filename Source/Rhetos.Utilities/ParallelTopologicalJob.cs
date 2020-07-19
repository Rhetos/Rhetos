using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rhetos.Logging;

namespace Rhetos.Utilities
{
    public class ParallelTopologicalJob
    {
        private class JobTask
        {
            public string Id { get; }
            public Action Action { get; }
            public List<string> Dependencies { get; }

            public JobTask(string id, Action action, IEnumerable<string> dependencies)
            {
                Id = id;
                Action = action;
                Dependencies = dependencies.ToList();
            }

            public string DependenciesInfo() =>
                string.Join(", ", Dependencies.Select(dependency => $"'{dependency}'"));
        }

        private readonly List<JobTask> _tasks = new List<JobTask>();
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;

        public ParallelTopologicalJob(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger(nameof(ParallelTopologicalJob));
            _performanceLogger = logProvider.GetLogger("Performance." + nameof(ParallelTopologicalJob));
        }

        public ParallelTopologicalJob AddTask(string id, Action action, IEnumerable<string> dependencies = null)
        {
            if (_tasks.Any(task => task.Id == id))
                throw new InvalidOperationException($"Task with id '{id}' has already been added to the job.");

            _tasks.Add(new JobTask(id, action, dependencies ?? Enumerable.Empty<string>()));
            return this;
        }

        public void RunAllTasks(int maxDegreeOfParallelism = -1, CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var startedTasks = new Dictionary<string, Task>();
            var completedTasks = new ConcurrentBag<string>();

            while (true)
            {
                Task.Delay(1, cancellationToken).Wait(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                // capture the state of completed tasks for further processing
                var resolvedDependencies = new HashSet<string>(completedTasks);

                // check for faulted tasks, if any, continue until all started are completed
                if (startedTasks.Values.Any(task => task.IsFaulted))
                {
                    if (startedTasks.Values.All(task => task.IsCompleted))
                    {
                        _logger.Error(() => $"Aborted further execution due to task errors.");
                        break;
                    }
                    continue;
                }

                // are we all done?
                if (resolvedDependencies.Count == _tasks.Count)
                    break;

                if (maxDegreeOfParallelism > 0 && startedTasks.Values.Count(task => !task.IsCompleted) >= maxDegreeOfParallelism)
                    continue;

                var firstAvailable = _tasks
                    .FirstOrDefault(task => !startedTasks.ContainsKey(task.Id) && task.Dependencies.All(dependency => resolvedDependencies.Contains(dependency)));

                if (firstAvailable == null)
                {
                    // no new tasks are available, but all started are completed
                    if (resolvedDependencies.Count == startedTasks.Count)
                    {
                        var invalidTasks = _tasks.Where(task => !startedTasks.ContainsKey(task.Id));
                        var invalidTaskReasons = invalidTasks
                            .Select(task => $"task '{task.Id}' requires {task.DependenciesInfo()}");
                        throw new InvalidOperationException($"Unable to resolve required task dependencies ({string.Join("; ", invalidTaskReasons)}).");
                    }
                }
                else
                {
                    var newTask = Task.Run(() => RunSingleTask(firstAvailable, completedTasks));
                    startedTasks.Add(firstAvailable.Id, newTask);
                }
            }

            ThrowIfAnyTaskErrors(startedTasks);
            _performanceLogger.Write(sw, () => $"Executed {_tasks.Count} tasks.");
        }

        private void ThrowIfAnyTaskErrors(Dictionary<string, Task> completedTasks)
        {
            var errors = completedTasks.Values
                .Where(task => task.IsFaulted)
                .Select(task => task.Exception?.InnerException ?? task.Exception)
                .ToList();

            if (errors.Any())
                throw new AggregateException(errors);
        }

        private void RunSingleTask(JobTask task, ConcurrentBag<string> completedTasks)
        {
            _logger.Trace(() => $"Starting '{task.Id}', dependencies: {task.DependenciesInfo()}.");
            try
            {
                var sw = Stopwatch.StartNew();
                task.Action();
                _performanceLogger.Write(sw, () => $"Task '{task.Id}' completed.");
            }
            catch (Exception e)
            {
                _logger.Error(() => $"Error during invocation of task '{task.Id}': {e}.");
                throw;
            }
            _logger.Trace(() => $"'{task.Id}' completed.");
            completedTasks.Add(task.Id);
        }
    }
}
