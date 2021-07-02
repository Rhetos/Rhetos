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

namespace Rhetos.Dsl
{
    public class ConceptMemberBase
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public bool IsConceptInfo { get; set; }
        public bool IsKey { get; set; }
        public bool IsParentNested { get; set; }
        public int SortOrder1 { get; set; }
        public int SortOrder2 { get; set; }
        public bool IsDerived { get; set; }
        public bool IsStringType { get; set; }
        public bool IsConceptInfoInterface { get; set; }
        public bool IsParsable { get; set; }

        public static void Copy(ConceptMemberBase source, ConceptMemberBase target)
        {
            target.Index = source.Index;
            target.Name = source.Name;
            target.IsConceptInfo = source.IsConceptInfo;
            target.IsKey = source.IsKey;
            target.IsParentNested = source.IsParentNested;
            target.SortOrder1 = source.SortOrder1;
            target.SortOrder2 = source.SortOrder2;
            target.IsDerived = source.IsDerived;
            target.IsStringType = source.IsStringType;
            target.IsConceptInfoInterface = source.IsConceptInfoInterface;
            target.IsParsable = source.IsParsable;
        }
    }
}