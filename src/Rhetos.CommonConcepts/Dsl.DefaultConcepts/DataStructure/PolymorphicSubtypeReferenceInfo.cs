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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhetos.Dsl;
using System.ComponentModel.Composition;
using Rhetos.Compiler;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// For each subtype, the polymorphic supertype has a reference to the subtype data structure.
    /// Each record has only one of those references set (based on it's subtype), all the others are null.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    public class PolymorphicSubtypeReferenceInfo : ReferencePropertyInfo
    {
    }
}
