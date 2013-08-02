/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System.Globalization;
using System.Diagnostics.Contracts;

namespace Rhetos.Dsl
{
    [global::System.Serializable]
    public class DslSyntaxException : FrameworkException
    {
        public DslSyntaxException() { }
        public DslSyntaxException(string message) : base(message) { }
        public DslSyntaxException(string message, Exception inner) : base(message, inner) { }
        
        public DslSyntaxException(IConceptInfo concept, string additionalMessage) : base(concept.GetUserDescription() + ": " + additionalMessage) { }
        public DslSyntaxException(IConceptInfo concept, string additionalMessage, Exception inner) : base(concept.GetUserDescription() + ": " + additionalMessage, inner) { }

        protected DslSyntaxException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
