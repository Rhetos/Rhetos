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

using System.Collections.Generic;

namespace Rhetos.Dsl
{
    public interface IConceptMacro
    {
        // TODO: There is a complication and a significant performance issue with generic usage of the CreateNewConcepts method in ConceptMacroUtility.
        // A better pattern would be to add a generic method to this interface, and a specific method to IConceptMacro<>,
        // then convert IConceptMacro<> from interface to an abstract class and implement the generic method by calling the specific method
        // (see a similar implementation at IConceptMapping).
        // Unfortunately, this change is backward incompatible, it should be done on a major upgrade.
        // After this, ConceptMacroUtility can be deleted and its usage simplified.
    }

    public interface IConceptMacro<in TConceptInfo> : IConceptMacro
        where TConceptInfo : IConceptInfo
    {
        /// <summary>
        /// If the function creates a concept that already exists, that concept will be safely ignored.
        /// </summary>
        IEnumerable<IConceptInfo> CreateNewConcepts(TConceptInfo conceptInfo, IDslModel existingConcepts);
    }
}
