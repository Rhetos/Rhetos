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

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    public class DelayedAction : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _loggingTask;
        private bool disposedValue;

        public DelayedAction(TimeSpan delay, Action action)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            _loggingTask = Task.Delay(delay, cancellationToken)
                .ContinueWith(delayTask =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                        action();
                }, TaskScheduler.Current);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (!_loggingTask.IsCompleted)
                    {
                        _cancellationTokenSource.Cancel(true);
                        _loggingTask.Wait(); // Making sure that the task and referenced objects can be cleanly disposed.
                    }
                    _loggingTask.Dispose();
                    _cancellationTokenSource.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
