/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    public class ParallelJob
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

        public ParallelJob(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger(nameof(ParallelJob));
        }

        public ParallelJob AddTask(string id, Action action, IEnumerable<string> dependencies = null)
        {
            if (_tasks.Any(task => task.Id == id))
                throw new InvalidOperationException($"Task with id '{id}' has already been added to the job.");

            _tasks.Add(new JobTask(id, action, dependencies ?? Enumerable.Empty<string>()));
            return this;
        }

        public void RunAllTasks(int maxDegreeOfParallelism = -1, CancellationToken cancellationToken = default)
        {
            var runningTasks = new Dictionary<string, Task>();
            var completedTasks = new Dictionary<string, Task>();
            var anyFaulted = false;

            while (completedTasks.Count < _tasks.Count)
            {
                var maxNewTasksAllowed = maxDegreeOfParallelism > 0
                    ? maxDegreeOfParallelism - runningTasks.Count
                    : _tasks.Count;

                // start new eligible tasks
                if (maxNewTasksAllowed > 0 && !anyFaulted)
                {
                    var eligibleTasks = _tasks.Where(task =>
                            !runningTasks.ContainsKey(task.Id)
                            && !completedTasks.ContainsKey(task.Id)
                            && task.Dependencies.All(dependency => completedTasks.ContainsKey(dependency)))
                        .Take(maxNewTasksAllowed)
                        .ToList();

                    if (runningTasks.Count == 0 && eligibleTasks.Count == 0)
                    {
                        var invalidTasks = _tasks.Where(task => !completedTasks.ContainsKey(task.Id));
                        var invalidTaskReasons = invalidTasks
                            .Select(task => $"task '{task.Id}' requires {task.DependenciesInfo()}");
                        throw new InvalidOperationException($"Unable to resolve required task dependencies ({string.Join("; ", invalidTaskReasons)}).");
                    }

                    foreach (var eligibleTask in eligibleTasks)
                        runningTasks.Add(eligibleTask.Id, Task.Run(() => RunSingleTask(eligibleTask), cancellationToken));
                }

                Task.WaitAny(runningTasks.Values.ToArray(), cancellationToken);

                // collect some of the completed tasks and process them
                // due to race condition further processing might miss some completed tasks - they will be handled in the next iteration
                var newlyCompletedTasks = runningTasks
                    .Where(a => a.Value.IsCompleted)
                    .ToList();

                foreach (var task in newlyCompletedTasks)
                {
                    completedTasks.Add(task.Key, task.Value);
                    runningTasks.Remove(task.Key);
                }

                anyFaulted = completedTasks.Values.Any(task => task.IsFaulted);
                if (anyFaulted && runningTasks.Count == 0)
                    break;
            }

            ThrowIfAnyTaskErrors(completedTasks);
        }

        private void ThrowIfAnyTaskErrors(Dictionary<string, Task> completedTasks)
        {
            var errors = completedTasks.Values
                .Where(task => task.IsFaulted)
                .Select(task => task.Exception?.InnerException ?? task.Exception)
                .ToList();

            if (errors.Count == 1)
                ExceptionsUtility.Rethrow(errors.Single());
            else if (errors.Count > 1)
                throw new AggregateException(errors);
        }

        private void RunSingleTask(JobTask task)
        {
            _logger.Trace(() => $"Starting task '{task.Id}', dependencies: {task.DependenciesInfo()}.");
            task.Action();
        }
    }
}
