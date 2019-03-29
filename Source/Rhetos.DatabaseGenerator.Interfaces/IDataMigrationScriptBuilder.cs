using Rhetos.Compiler;
using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.DatabaseGenerator
{
    public interface  IDataMigrationScriptBuilder
    {
        void AddBeforeDataMigrationScript(string script);

        void AddAfterDataMigrationScript(string script);

        void InsertCode<T>(string code, Tag<T> tag, T conceptInfo) where T : IConceptInfo;

        bool TagExists(string tag);
    }
}
