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
using System.Diagnostics.Contracts;
using System.ComponentModel.Composition;
using Rhetos.Utilities;
using System.Linq.Expressions;
using System.Reflection;

namespace Rhetos.Dsl
{
    // No need for ExportAttribute, this class is registered manually.
    public class DslModelIndexByReference : IDslModelIndex
    {
        /// <summary>
        /// Keys are: 1. Referenced concept key, 2. Returned concept type, 3. Reference property name.
        /// </summary>
        private class ConceptsIndex : Dictionary<string, Dictionary<Type, Dictionary<string, List<IConceptInfo>>>>
        {
            // TODO: A better dictionary key would be: 1. value tuple (concept key, property name), 2. concept type. It has 1 level less, and allows simpler Get method for multiple concept types.
            public void Add(string referencedConceptKey, Type conceptType, string referenceName, IConceptInfo concept)
            {
                Dictionary<Type, Dictionary<string, List<IConceptInfo>>> index2;
                if (!TryGetValue(referencedConceptKey, out index2))
                {
                    index2 = new Dictionary<Type, Dictionary<string, List<IConceptInfo>>>();
                    Add(referencedConceptKey, index2);
                }

                Dictionary<string, List<IConceptInfo>> index3;
                if (!index2.TryGetValue(conceptType, out index3))
                {
                    index3 = new Dictionary<string, List<IConceptInfo>>();
                    index2.Add(conceptType, index3);
                }

                List<IConceptInfo> index4;
                if (!index3.TryGetValue(referenceName, out index4))
                {
                    index4 = new List<IConceptInfo>();
                    index3.Add(referenceName, index4);
                }

                index4.Add(concept);
            }

            private static IConceptInfo[] EmptyConceptsArray = Array.Empty<IConceptInfo>();

            public IEnumerable<IConceptInfo> Get(string referencedConceptKey, IEnumerable<Type> conceptTypes, string referenceName)
            {
                IEnumerable<IConceptInfo> returnConcepts = null;

                Dictionary<Type, Dictionary<string, List<IConceptInfo>>> index2;
                if (TryGetValue(referencedConceptKey, out index2))
                {
                    Dictionary<string, List<IConceptInfo>> index3;
                    foreach (var conceptType in conceptTypes)
                        if (index2.TryGetValue(conceptType, out index3))
                        {
                            List<IConceptInfo> index4;
                            if (index3.TryGetValue(referenceName, out index4))
                            {
                                if (returnConcepts == null)
                                    returnConcepts = index4;
                                else
                                    returnConcepts = returnConcepts.Concat(index4);
                            }
                        }
                }

                return returnConcepts ?? EmptyConceptsArray;
            }
        }

        private readonly ConceptsIndex _conceptsIndex = new ConceptsIndex();

        /// <summary>
        /// The <see cref="MultiDictionary{TKey,TValue}.Get"/> method returns all previously registered derivations
        /// or implementations (by <see cref="Add"/>) of the given base class or an index,
        /// including the supertype itself (if registered).
        /// The supertype may be an interface or a class, except the <see cref="Object"/> type.
        /// </summary>
        private class Subtypes : MultiDictionary<Type, Type>
        {
            private readonly HashSet<Type> _typesAdded = new HashSet<Type>();

            public void Add(Type subtype)
            {
                if (!_typesAdded.Contains(subtype))
                {
                    _typesAdded.Add(subtype);

                    Add(subtype, subtype);

                    foreach (var interfaceType in subtype.GetInterfaces())
                        Add(interfaceType, subtype);

                    var baseType = subtype.BaseType;
                    while (baseType != null && baseType != typeof(object))
                    {
                        Add(baseType, subtype);
                        baseType = baseType.BaseType;
                    }
                }
            }
        }

        private readonly Subtypes _subtypes = new Subtypes();

        public void Add(IConceptInfo concept)
        {
            Type conceptType = concept.GetType();

            _subtypes.Add(conceptType);

            foreach (ConceptMember member in ConceptMembers.Get(concept))
                if (member.IsConceptInfo)
                {
                    string referencedConceptKey = ((IConceptInfo)member.GetValue(concept)).GetKey();
                    _conceptsIndex.Add(referencedConceptKey, conceptType, member.Name, concept);
                }
        }

        public IEnumerable<IConceptInfo> FindByReference(Type conceptType, bool includeDerivations, string referenceName, string referencedConceptKey)
        {
            if (includeDerivations)
                return _conceptsIndex.Get(referencedConceptKey, _subtypes.Get(conceptType), referenceName);
            else
                return _conceptsIndex.Get(referencedConceptKey, new[] { conceptType }, referenceName);
        }
    }

    public static class DslModelIndexByReferenceExtensions
    {
        public static IEnumerable<T> FindByReference<T>(this IDslModel dslModel, Expression<Func<T, object>> referenceProperty, IConceptInfo referencedConcept) where T : IConceptInfo
        {
            var propertyName = GetPropertyName<T>(referenceProperty, checkValueType: referencedConcept);
            return dslModel.GetIndex<DslModelIndexByReference>().FindByReference(typeof(T), true, propertyName, referencedConcept.GetKey()).Cast<T>();
        }

        public static IEnumerable<IConceptInfo> FindByReference(this IDslModel dslModel, Type conceptType, bool includeDerivations, string referenceName, string referencedConceptKey)
        {
            return dslModel.GetIndex<DslModelIndexByReference>().FindByReference(conceptType, includeDerivations, referenceName, referencedConceptKey);
        }

        private static string GetPropertyName<T>(Expression<Func<T, object>> referenceProperty, IConceptInfo checkValueType)
        {
            var memberExpression = referenceProperty.Body as MemberExpression;
            if (memberExpression == null)
                throw new FrameworkException("Invalid FindByReference method argument: referenceProperty. The argument should be a lambda expression selecting a property of the class "
                    + typeof(T).Name + ". For example: \"conceptInfo => conceptInfo.SomeProperty\".");

            var property = memberExpression.Member as PropertyInfo;
            if (property == null || memberExpression.Expression.NodeType != ExpressionType.Parameter)
                throw new FrameworkException("Invalid FindByReference method argument: referenceProperty. The argument should be a lambda expression selecting a property of the class "
                    + typeof(T).Name + ". For example: \"conceptInfo => conceptInfo.SomeProperty\".");

            if (!typeof(IConceptInfo).IsAssignableFrom(property.PropertyType))
                throw new FrameworkException("Invalid FindByReference method argument: referenceProperty. The selected property should be of type that implements IConceptInfo."
                    + " Property '" + property.Name + "' is of type '" + property.PropertyType.Name + "'.");

            if (!property.PropertyType.IsAssignableFrom(checkValueType.GetType()))
                throw new FrameworkException("Invalid FindByReference method arguments: The selected property " + property.Name
                    + " type '" + property.PropertyType.Name
                    + "' does not match the given referenced concept '" + checkValueType.GetUserDescription() +"'.");

            return property.Name;
        }
    }
}
