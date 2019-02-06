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
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Rhetos.Dsl
{
    public static class ConceptInfoHelper
    {
        private static ConditionalWeakTable<IConceptInfo, string> KeyCache = new ConditionalWeakTable<IConceptInfo, string>();

        /// <summary>
        /// Returns a string that <b>uniquely describes the concept instance</b>.
        /// The string contains concept's base class type and a list of concept's key properties.
        /// </summary>
        /// <remarks>
        /// If the concept inherits another concept, the base class type will be used instead of
        /// actual concept's type to achieve normalized form. That way, the resulting string
        /// can be used in scenarios such as resolving references to other concepts where
        /// a reference can be of the base class type, but referencing inherited type.
        /// </remarks>
        public static string GetKey(this IConceptInfo ci)
        {
            if (ci == null)
                throw new ArgumentNullException();

            return KeyCache.GetValue(ci, CreateKey);
        }

        private static string CreateKey(IConceptInfo ci)
        {
            return BaseConceptInfoType(ci).Name + " " + SerializeMembers(ci, SerializationOptions.KeyMembers, true);
        }
        public static string GetShortDescription(this IConceptInfo ci)
        {
            return ci.GetType().Name + " " + SerializeMembers(ci, SerializationOptions.KeyMembers);
        }

        /// <summary>
        /// Returns a string that describes the concept instance in a user-friendly manner.
        /// The string contains concept's keyword and a list of concept's key properties.
        /// </summary>
        /// <remarks>
        /// This description in not unique because different concepts might have same keyword.
        /// </remarks>
        public static string GetUserDescription(this IConceptInfo ci)
        {
            return GetKeywordOrTypeName(ci) + " " + SerializeMembers(ci, SerializationOptions.KeyMembers); ;
        }

        /// <summary>
        /// Returns a string with a dot-separated list of concept's key properties.
        /// </summary>
        public static string GetKeyProperties(this IConceptInfo ci)
        {
            return SerializeMembers(ci, SerializationOptions.KeyMembers, exceptionOnNullMember: true);
        }

        /// <summary>
        /// Returns a string that fully describes the concept instance.
        /// The string contains concept's type name and all concept's properties.
        /// </summary>
        public static string GetFullDescription(this IConceptInfo ci)
        {
            return ci.GetType().FullName + " " + SerializeMembers(ci, SerializationOptions.AllMembers);
        }

        /// <summary>
        /// Return value is null if IConceptInfo implementations does not have ConceptKeyword attribute. Such classes are usually used as a base class for other concepts.
        /// </summary>
        public static string GetKeyword(this IConceptInfo ci)
        {
            return GetKeyword(ci.GetType());
        }

        /// <summary>
        /// Return value is null if IConceptInfo implementations does not have ConceptKeyword attribute. Such classes are usually used as a base class for other concepts.
        /// </summary>
        public static string GetKeyword(Type conceptInfoType)
        {
            return conceptInfoType
                .GetCustomAttributes(typeof(ConceptKeywordAttribute), false)
                .Select(keywordAttribute => ((ConceptKeywordAttribute)keywordAttribute).Keyword)
                .SingleOrDefault();
        }

        public static string GetKeywordOrTypeName(this IConceptInfo ci)
        {
            return ci.GetKeyword() ?? ci.GetType().Name;
        }

        public static string GetKeywordOrTypeName(Type conceptInfoType)
        {
            return GetKeyword(conceptInfoType) ?? conceptInfoType.Name;
        }

        /// <summary>
        /// Returns a list of concepts that this concept directly depends on.
        /// </summary>
        public static IEnumerable<IConceptInfo> GetDirectDependencies(this IConceptInfo conceptInfo)
        {
            return (from member in ConceptMembers.Get(conceptInfo)
                    where member.IsConceptInfo
                    select (IConceptInfo)member.GetValue(conceptInfo)).Distinct().ToList();
        }

        /// <summary>
        /// Returns a list of concepts that this concept depends on directly or indirectly.
        /// </summary>
        public static IEnumerable<IConceptInfo> GetAllDependencies(this IConceptInfo conceptInfo)
        {
            var dependencies = new List<IConceptInfo>();
            AddAllDependencies(conceptInfo, dependencies);
            return dependencies;
        }

        /// <summary>
        /// Use only for generating an error details. Returns the concept's description ignoring possible null reference errors.
        /// </summary>
        public static string GetErrorDescription(this IConceptInfo ci)
        {
            if (ci == null)
                return "<null>";
            var report = new StringBuilder();
            report.Append(ci.GetType().FullName);
            foreach (var member in ConceptMembers.Get(ci))
            {
                report.Append(" " + member.Name + "=");
                var memberValue = member.GetValue(ci);
                try
                {
                    if (memberValue == null)
                        report.Append("<null>");
                    else if (member.IsConceptInfo)
                        report.Append(SerializeMembers((IConceptInfo)memberValue, SerializationOptions.KeyMembers, exceptionOnNullMember: false));
                    else
                        report.Append(memberValue.ToString());
                }
                catch (Exception ex)
                {
                    report.Append("<" + ex.GetType().Name + ">");
                }
            }
            return report.ToString();
        }


        private static void AddAllDependencies(IConceptInfo conceptInfo, ICollection<IConceptInfo> dependencies)
        {
            foreach (var member in ConceptMembers.Get(conceptInfo))
                if (member.IsConceptInfo)
                {
                    var dependency = (IConceptInfo)member.GetValue(conceptInfo);
                    if (!dependencies.Contains(dependency))
                    {
                        dependencies.Add(dependency);
                        AddAllDependencies(dependency, dependencies);
                    }
                }
        }

        private enum SerializationOptions
        {
            KeyMembers,
            AllMembers
        };

        private static string SafeDelimit(string text)
        {
            bool clean = text.All(c => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_');
            if (clean && text.Length > 0)
                return text;
            string quote = (text.Contains('\'') && !text.Contains('\"')) ? "\"" : "\'";
            return quote + text.Replace(quote, quote + quote) + quote;
        }

        public static Type BaseConceptInfoType(this IConceptInfo ci)
        {
            Type t = ci.GetType();
            while (typeof(IConceptInfo).IsAssignableFrom(t.BaseType) && t.BaseType.IsClass)
                t = t.BaseType;
            return t;
        }

        private static Dictionary<Type, Func<IConceptInfo, SerializationOptions, bool, string>> _serializeMemebersCompiled = new Dictionary<Type, Func<IConceptInfo, SerializationOptions, bool, string>>();

        private static string SerializeMembers(IConceptInfo ci, SerializationOptions serializationOptions, bool exceptionOnNullMember = false)
        {
            Func<IConceptInfo, SerializationOptions, bool, string> func = null;
            if (!_serializeMemebersCompiled.TryGetValue(ci.GetType(), out func))
            {
                func = CreateSerializeMembersFunction(ci.GetType());
                _serializeMemebersCompiled.Add(ci.GetType(), func);
            }
            return func(ci, serializationOptions, exceptionOnNullMember);
        }

        #region Serialization expression builder

        private static Func<IConceptInfo, SerializationOptions, bool, string> CreateSerializeMembersFunction(Type conceptType)
        {
            var conceptParamExpr = Expression.Parameter(typeof(IConceptInfo), "concept");
            var serializationOptionsParameExpr = Expression.Parameter(typeof(SerializationOptions), "serializationOptions");
            var exceptionOnNullMemberParamExpr = Expression.Parameter(typeof(bool), "exceptionOnNullMember");
            var conceptExpression = Expression.Convert(conceptParamExpr, conceptType);

            var serializeMembersLambdaExpression = Expression.Lambda<Func<IConceptInfo, SerializationOptions, bool, string>>(
                Expression.Block(
                    GenerateValidationExpression(conceptExpression, conceptType, serializationOptionsParameExpr, exceptionOnNullMemberParamExpr),
                    GenerateSerializeMembersExpression(conceptExpression, conceptType, serializationOptionsParameExpr, exceptionOnNullMemberParamExpr)
                    ),
                conceptParamExpr,
                serializationOptionsParameExpr,
                exceptionOnNullMemberParamExpr);
            return serializeMembersLambdaExpression.Compile();
        }

        private static Expression GenerateConcatenationExpression(List<Expression> stringEValuationExpressions)
        {
            if (stringEValuationExpressions.Count == 1)
                return Expression.Call(typeof(string).GetMethod("Concat", new[] { typeof(string) }), stringEValuationExpressions[0]);
            if (stringEValuationExpressions.Count == 2)
                return Expression.Call(typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) }), stringEValuationExpressions[0], stringEValuationExpressions[1]);
            else if (stringEValuationExpressions.Count == 3)
                return Expression.Call(typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string), typeof(string) }), stringEValuationExpressions[0], stringEValuationExpressions[1], stringEValuationExpressions[2]);
            else if (stringEValuationExpressions.Count == 4)
                return Expression.Call(typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string), typeof(string), typeof(string) }), stringEValuationExpressions[0], stringEValuationExpressions[1], stringEValuationExpressions[2], stringEValuationExpressions[3]);
            else
                return Expression.Call(typeof(string).GetMethod("Concat", new[] { typeof(string[]) }), Expression.NewArrayInit(typeof(string), stringEValuationExpressions));
        }

        private static Expression GenerateValidationExpression(
            Expression conceptExpression, Type type,
            ParameterExpression serializationOptionsParameExpr,
            ParameterExpression exceptionOnNullMemberParamExpr)
        {
            var keyMembersValidationExpression = new List<Expression>();
            var nonKeyMemebersMembersValidationExpression = new List<Expression>();

            foreach (var conceptMember in ConceptMembers.Get(type))
            {
                var memberValidationExpression = Expression.IfThen(
                        Expression.Equal(Expression.PropertyOrField(conceptExpression, conceptMember.Name), Expression.Constant(null)),
                        Expression.Call(typeof(ConceptInfoHelper).GetMethod("ThrowMemberNullExceptionForConcept", BindingFlags.Static | BindingFlags.NonPublic), conceptExpression, Expression.Constant(conceptMember.Name))
                        );

                if (conceptMember.IsKey)
                    keyMembersValidationExpression.Add(memberValidationExpression);
                else
                    nonKeyMemebersMembersValidationExpression.Add(memberValidationExpression);
            }

            var conceptValidationExpression = Expression.IfThen(
                        exceptionOnNullMemberParamExpr,
                        Expression.Block(keyMembersValidationExpression)
                        );

            if (nonKeyMemebersMembersValidationExpression.Count != 0)
            {
                conceptValidationExpression = Expression.IfThen(
                        exceptionOnNullMemberParamExpr,
                        Expression.Block(
                            Expression.Block(keyMembersValidationExpression),
                            conceptValidationExpression
                        )
                    );
            }

            return conceptValidationExpression;
        }

        private static Expression GenerateSerializeMembersExpression(
            Expression conceptExpression, Type type,
            ParameterExpression serializationOptionsParameExpr,
            ParameterExpression exceptionOnNullMemberParamExpr)
        {
            var keyMembersEvaluationExpression = new List<Expression>();
            var otherMembersEvaluationExpression = new List<Expression>();
            var firstMember = true;

            foreach (var conceptMember in ConceptMembers.Get(type))
            {
                if (conceptMember.IsKey)
                {
                    if (firstMember)
                        firstMember = false;
                    else
                        keyMembersEvaluationExpression.Add(Expression.Constant("."));
                    keyMembersEvaluationExpression.AddRange(GenerateSerializeMemberExpression(conceptMember, type, conceptExpression, exceptionOnNullMemberParamExpr));
                }
                else
                {
                    if (firstMember)
                        firstMember = false;
                    else
                        otherMembersEvaluationExpression.Add(Expression.Constant(" "));
                    otherMembersEvaluationExpression.AddRange(GenerateSerializeMemberExpression(conceptMember, type, conceptExpression, exceptionOnNullMemberParamExpr));
                }
            }

            return Expression.Condition(
                Expression.Equal(serializationOptionsParameExpr, Expression.Constant(SerializationOptions.KeyMembers)),
                GenerateConcatenationExpression(keyMembersEvaluationExpression),
                GenerateConcatenationExpression(keyMembersEvaluationExpression.Union(otherMembersEvaluationExpression).ToList())
            );
        }

        private static List<Expression> GenerateSerializeMemberExpression(
            ConceptMember conceptMember,
            Type type,
            Expression conceptExpression,
            ParameterExpression exceptionOnNullMemberParamExpr)
        {
            var memberEvaluationExpressionList = new List<Expression>();
            if (conceptMember.IsConceptInfo)
            {
                if (conceptMember.ValueType == typeof(IConceptInfo))
                {
                    memberEvaluationExpressionList.Add(Expression.Call(
                        typeof(ConceptInfoHelper).GetMethod("BaseConceptInfoTypeName", BindingFlags.Static | BindingFlags.NonPublic),
                        Expression.PropertyOrField(conceptExpression, conceptMember.Name)
                        ));
                    memberEvaluationExpressionList.Add(Expression.Constant(":"));
                }

                memberEvaluationExpressionList.Add(
                    Expression.Condition(
                        Expression.Equal(Expression.PropertyOrField(conceptExpression, conceptMember.Name), Expression.Constant(null)),
                        Expression.Constant("<null>"),
                        Expression.Call(
                            typeof(ConceptInfoHelper).GetMethod("SerializeMembers", BindingFlags.Static | BindingFlags.NonPublic),
                            Expression.PropertyOrField(conceptExpression, conceptMember.Name),
                            Expression.Constant(SerializationOptions.KeyMembers), exceptionOnNullMemberParamExpr
                            )
                        )
                    );
            }
            else if (conceptMember.ValueType == typeof(string))
            {
                memberEvaluationExpressionList.Add(Expression.Condition(
                        Expression.Equal(Expression.PropertyOrField(conceptExpression, conceptMember.Name), Expression.Constant(null)),
                        Expression.Constant("<null>"),
                        Expression.Call(
                            typeof(ConceptInfoHelper).GetMethod("SafeDelimit", BindingFlags.Static | BindingFlags.NonPublic),
                            Expression.PropertyOrField(conceptExpression, conceptMember.Name)
                            )
                        ));
            }
            else
            {
                throw new FrameworkException(string.Format(
                    "IConceptInfo member {0} of type {1} in {2} is not supported.",
                    conceptMember.Name, conceptMember.ValueType.Name, type.Name));
            }

            return memberEvaluationExpressionList;
        }

        private static string BaseConceptInfoTypeName(this IConceptInfo ci)
        {
            return BaseConceptInfoType(ci).Name;
        }

        private static DslSyntaxException ThrowMemberNullExceptionForConcept(IConceptInfo ci, string memeberName)
        {
            throw new DslSyntaxException(ci, string.Format(
                            "{0}'s property {1} is null. Info: {2}.",
                            ci.GetType().Name, memeberName, ci.GetErrorDescription())
                        );
        }

        #endregion
    }
}