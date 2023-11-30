﻿/*
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

using System.Collections.Generic;

namespace Rhetos.Dsl
{
    public interface IConceptMacro
    {
        IEnumerable<IConceptInfo> CreateNewConcepts(IConceptInfo conceptInfo, IDslModel existingConcepts);
    }

    public interface IConceptMacro<in TConceptInfo> : IConceptMacro
        where TConceptInfo : IConceptInfo
    {
        /// <summary>
        /// If the function creates a concept that already exists, that concept will be safely ignored.
        /// </summary>
        IEnumerable<IConceptInfo> CreateNewConcepts(TConceptInfo conceptInfo, IDslModel existingConcepts);

        IEnumerable<IConceptInfo> IConceptMacro.CreateNewConcepts(IConceptInfo conceptInfo, IDslModel existingConcepts)
        {
            return CreateNewConcepts((TConceptInfo)conceptInfo, existingConcepts);
        }
    }
}
