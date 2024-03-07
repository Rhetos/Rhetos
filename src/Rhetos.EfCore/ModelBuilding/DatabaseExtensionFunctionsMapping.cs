using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using System.ComponentModel.Composition;

namespace Rhetos.EfCore.ModelBuilding
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(InitializationConcept))]
    [ExportMetadata(MefProvider.DependsOn, typeof(DbContextCodeGenerator))]
    public class DatabaseExtensionFunctionsMapping : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            string code =
            @"modelBuilder.HasDbFunction(typeof(DatabaseExtensionFunctions).GetMethod(nameof(DatabaseExtensionFunctions.EqualsCaseInsensitive), new[] { typeof(string), typeof(string) }))
                .HasTranslation(args => new SqlBinaryExpression(ExpressionType.Equal, args[0], args[1], args[0].Type, args[0].TypeMapping));

            modelBuilder.HasDbFunction(typeof(DatabaseExtensionFunctions).GetMethod(nameof(DatabaseExtensionFunctions.NotEqualsCaseInsensitive), new[] { typeof(string), typeof(string) }))
                .HasTranslation(args => new SqlBinaryExpression(ExpressionType.NotEqual, args[0], args[1], args[0].Type, args[0].TypeMapping));

            modelBuilder.HasDbFunction(typeof(DatabaseExtensionFunctions).GetMethod(nameof(DatabaseExtensionFunctions.IsLessThan), new[] { typeof(string), typeof(string) }))
                .HasTranslation(args => new SqlBinaryExpression(ExpressionType.LessThan, args[0], args[1], args[0].Type, args[0].TypeMapping));

            modelBuilder.HasDbFunction(typeof(DatabaseExtensionFunctions).GetMethod(nameof(DatabaseExtensionFunctions.IsLessThanOrEqual), new[] { typeof(string), typeof(string) }))
                .HasTranslation(args => new SqlBinaryExpression(ExpressionType.LessThanOrEqual, args[0], args[1], args[0].Type, args[0].TypeMapping));

            modelBuilder.HasDbFunction(typeof(DatabaseExtensionFunctions).GetMethod(nameof(DatabaseExtensionFunctions.IsGreaterThan), new[] { typeof(string), typeof(string) }))
                .HasTranslation(args => new SqlBinaryExpression(ExpressionType.GreaterThan, args[0], args[1], args[0].Type, args[0].TypeMapping));

            modelBuilder.HasDbFunction(typeof(DatabaseExtensionFunctions).GetMethod(nameof(DatabaseExtensionFunctions.IsGreaterThanOrEqual), new[] { typeof(string), typeof(string) }))
                .HasTranslation(args => new SqlBinaryExpression(ExpressionType.GreaterThanOrEqual, args[0], args[1], args[0].Type, args[0].TypeMapping));

            static SqlExpression AddString(SqlExpression left, SqlExpression right)
            {
                if (left is SqlConstantExpression cl && cl.Value == null)
                    return left;

                if (right is SqlConstantExpression cr && cr.Value == null)
                    return right;

                // The binary expression 'type' parameter is set to null instead of typeof(string), because the string type would
                // result with automatic converting a null string parameter (not a constant expresssion) to an empty string.
                // That would result with StartsWith, Contains and other method returning all records instead of none.
                return new SqlBinaryExpression(ExpressionType.Add, left, right,
                    null, new StringTypeMapping(""nvarchar(max)"", DbType.String));
            }

            modelBuilder.HasDbFunction(typeof(DatabaseExtensionFunctions).GetMethod(nameof(DatabaseExtensionFunctions.StartsWith), new[] { typeof(int), typeof(string) }))
                .HasTranslation(args =>
                {
                    var columnAsStringExpression = new SqlUnaryExpression(ExpressionType.Convert, args[0], typeof(string), new StringTypeMapping(""nvarchar(max)"", DbType.String));
                    var patternExpression = AddString(args[1], new SqlFragmentExpression(""N'%'""));
                    return new LikeExpression(columnAsStringExpression, patternExpression, null, null);
                });

            modelBuilder.HasDbFunction(typeof(DatabaseExtensionFunctions).GetMethod(nameof(DatabaseExtensionFunctions.StartsWithCaseInsensitive), new[] { typeof(string), typeof(string) }))
                .HasTranslation(args =>
                {
                    var patternExpression = AddString(args[1], new SqlFragmentExpression(""N'%'""));
                    return new LikeExpression(args[0], patternExpression, null, null);
                });

            modelBuilder.HasDbFunction(typeof(DatabaseExtensionFunctions).GetMethod(nameof(DatabaseExtensionFunctions.EndsWithCaseInsensitive), new[] { typeof(string), typeof(string) }))
                .HasTranslation(args =>
                {
                    var patternExpression = AddString(new SqlFragmentExpression(""N'%'""), args[1]);
                    return new LikeExpression(args[0], patternExpression, null, null);
                });

            modelBuilder.HasDbFunction(typeof(DatabaseExtensionFunctions).GetMethod(nameof(DatabaseExtensionFunctions.ContainsCaseInsensitive), new[] { typeof(string), typeof(string) }))
                .HasTranslation(args =>
                {
                    var patternExpression = AddString(AddString(new SqlFragmentExpression(""N'%'""), args[1]), new SqlFragmentExpression(""N'%'""));
                    return new LikeExpression(args[0], patternExpression, null, null);
                });

            modelBuilder.HasDbFunction(typeof(DatabaseExtensionFunctions).GetMethod(nameof(DatabaseExtensionFunctions.Like), new[] { typeof(string), typeof(string) }))
                .HasTranslation(args => new LikeExpression(args[0], args[1], null, null));

            modelBuilder.HasDbFunction(typeof(DatabaseExtensionFunctions).GetMethod(nameof(DatabaseExtensionFunctions.CastToString), new[] { typeof(int) }))
                .HasTranslation(args => new SqlUnaryExpression(ExpressionType.Convert, args[0], typeof(string), new StringTypeMapping(""nvarchar(max)"", DbType.String)));

            ";

            codeBuilder.InsertCode(code, DbContextCodeGenerator.EntityFrameworkContextOnModelCreatingTag);
        }
    }
}
