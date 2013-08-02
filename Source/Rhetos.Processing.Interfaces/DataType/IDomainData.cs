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

namespace Rhetos.Processing
{
    public interface IDomainData : ICommandData
    {
        T GetData<T>(Type type) where T : class;
        void SetData<T>(T data) where T : class;
    }

    public static class DomainDataHelper
    {
        public static T GetData<T>(this IDomainData data) where T : class
        {
            return data.GetData<T>(typeof(T));
        }
    }
}
