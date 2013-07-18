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
using Rhetos;
using Rhetos.Compiler;
using Rhetos.Dsl;
using System.IO;
using System.ComponentModel.DataAnnotations;

namespace Rhetos.MvcGenerator
{
    internal class InitialCodeGenerator : IMvcGeneratorPlugin
    {
        private const string CodeSnippet =
@"
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
" + MvcGeneratorTags.Using + @"

/*
    If additional DataAnnotation attributes wants to be used for specific Model here generated, create class as following:

    [MetadataTypeAttribute(typeof(MyModel.AdditionalAttributes))]{
    public partial class MyModel
    {
        internal sealed class AdditionalAttributes
        {
            private AdditionalAttributes() { }

            ...
            List of properties along with additional DataAnnotation attributes.
            Example:
            [Display(Name = ""Last Name"", Order = 1, Prompt = ""Enter Last Name"")]
            public string LastName { get; set; }
            ...

        }
    }

*/
namespace Rhetos.Mvc.Model
{

    public class BaseMvcModel
    {
         public Guid ID { get; set; }
    }

    " + MvcGeneratorTags.NamespaceMembers + @"

    public class MinValueIntegerAttribute : ValidationAttribute
    {
        public string MinValue { get; set; }

        public override bool IsValid(object value)
        {
            return Convert.ToInt32(value) >= Convert.ToInt32(MinValue);
        }
    }

    public class MinValueDecimalAttribute : ValidationAttribute
    {
        public string MinValue { get; set; }

        public override bool IsValid(object value)
        {
            return  Convert.ToDecimal(value) >= Convert.ToDecimal(MinValue);
        }
    }

    public class MinValueDateAttribute : ValidationAttribute
    {
        public string MinValue { get; set; }

        public override bool IsValid(object value)
        {
            return Convert.ToDateTime(value) >= Convert.ToDateTime(MinValue);
        }
    }

    public class MaxValueIntegerAttribute : ValidationAttribute
    {
        public string MaxValue { get; set; }

        public override bool IsValid(object value)
        {
            return Convert.ToInt32(value) <= Convert.ToInt32(MaxValue);
        }
    }

    public class MaxValueDecimalAttribute : ValidationAttribute
    {
        public string MaxValue { get; set; }

        public override bool IsValid(object value)
        {
            return Convert.ToDecimal(value) <= Convert.ToDecimal(MaxValue);
        }
    }

    public class MaxValueDateAttribute : ValidationAttribute
    {
        public string MaxValue { get; set; }

        public override bool IsValid(object value)
        {
            return Convert.ToDateTime(value) <= Convert.ToDateTime(MaxValue);
        }
    }


	public interface IModel
	{
    " + MvcGeneratorTags.InterfaceMembers + @"
	}

    " + MvcGeneratorTags.ImplementationMembers + @"

}
";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            codeBuilder.InsertCode(CodeSnippet);

            codeBuilder.AddReferencesFromDependency(typeof(Guid));
            codeBuilder.AddReferencesFromDependency(typeof(System.Linq.Enumerable));
            codeBuilder.AddReferencesFromDependency(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute));
            codeBuilder.AddReferencesFromDependency(typeof(System.Web.Mvc.ModelValidator));
        }

        private static readonly string _rootPath = AppDomain.CurrentDomain.BaseDirectory;
    }
}