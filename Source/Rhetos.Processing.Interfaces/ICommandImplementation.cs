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

namespace Rhetos.Processing
{
    /// <summary>
    /// Represents a processing engine command.
    /// A Rhetos app will usually exposed the commands through some web API implementation.
    /// </summary>
    public interface ICommandImplementation
    {
        object Execute(object parameter);
    }

    public interface ICommandImplementation<in TCommandParameter, out TCommandResult> : ICommandImplementation
        where TCommandParameter : class, ICommandInfo // Arguments and results are classes to avoid edge cases in serialization.
        where TCommandResult : class
    {
        TCommandResult Execute(TCommandParameter parameter);

        object ICommandImplementation.Execute(object parameter)
        {
            return Execute((TCommandParameter)parameter);
        }
    }
}
