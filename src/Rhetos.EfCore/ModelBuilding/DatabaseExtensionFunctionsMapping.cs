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

            modelBuilder.HasDbFunction(typeof(DatabaseExtensionFunctions).GetMethod(nameof(DatabaseExtensionFunctions.StartsWith), new[] { typeof(int), typeof(string) }))
                .HasTranslation(args =>
                {
                    var columnAsStringExpression = new SqlUnaryExpression(ExpressionType.Convert, args[0], typeof(string), new StringTypeMapping(""nvarchar(max)"", DbType.String));
                    var valueExpression = args[1];
                    var patternExpression = new SqlBinaryExpression(ExpressionType.Add,
                        valueExpression, new SqlFragmentExpression(""N'%'""),
                        typeof(string), new StringTypeMapping(""nvarchar(max)"", DbType.String));
                    return new LikeExpression(columnAsStringExpression, patternExpression, null, null);
                });

            modelBuilder.HasDbFunction(typeof(DatabaseExtensionFunctions).GetMethod(nameof(DatabaseExtensionFunctions.StartsWithCaseInsensitive), new[] { typeof(string), typeof(string) }))
                .HasTranslation(args =>
                {
                    var patternExpression = new SqlBinaryExpression(ExpressionType.Add,
                        args[1], new SqlFragmentExpression(""N'%'""),
                        typeof(string), new StringTypeMapping(""nvarchar(max)"", DbType.String));
                    return new LikeExpression(args[0], patternExpression, null, null);
                });

            modelBuilder.HasDbFunction(typeof(DatabaseExtensionFunctions).GetMethod(nameof(DatabaseExtensionFunctions.EndsWithCaseInsensitive), new[] { typeof(string), typeof(string) }))
                .HasTranslation(args =>
                {
                    var patternExpression = new SqlBinaryExpression(ExpressionType.Add,
                        new SqlFragmentExpression(""N'%'""), args[1],
                        typeof(string), new StringTypeMapping(""nvarchar(max)"", DbType.String));
                    return new LikeExpression(args[0], patternExpression, null, null);
                });

            modelBuilder.HasDbFunction(typeof(DatabaseExtensionFunctions).GetMethod(nameof(DatabaseExtensionFunctions.ContainsCaseInsensitive), new[] { typeof(string), typeof(string) }))
                .HasTranslation(args =>
                {
                    var patternExpression = new SqlBinaryExpression(ExpressionType.Add,
                        new SqlFragmentExpression(""N'%'""), args[1],
                        typeof(string), new StringTypeMapping(""nvarchar(max)"", DbType.String));

                    patternExpression = new SqlBinaryExpression(ExpressionType.Add,
                        patternExpression, new SqlFragmentExpression(""N'%'""),
                        typeof(string), new StringTypeMapping(""nvarchar(max)"", DbType.String));

                    return new LikeExpression(args[0], patternExpression, null, null);
                });

            modelBuilder.HasDbFunction(typeof(DatabaseExtensionFunctions).GetMethod(nameof(DatabaseExtensionFunctions.Like), new[] { typeof(string), typeof(string) }))
                .HasTranslation(args => new LikeExpression(args[0], args[1], null, null));

            modelBuilder.HasDbFunction(typeof(DatabaseExtensionFunctions).GetMethod(nameof(DatabaseExtensionFunctions.CastToString), new[] { typeof(int) }))
                .HasTranslation(args => new SqlUnaryExpression(ExpressionType.Convert, args[0], typeof(string), new StringTypeMapping(""nvarchar(max)"", DbType.String)));

            ";

            codeBuilder.InsertCode(code, DbContextCodeGenerator.EntityFrameworkOnModelCreatingTag);
        }
    }
}
