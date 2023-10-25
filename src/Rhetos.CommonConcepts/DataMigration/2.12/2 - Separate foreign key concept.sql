/*DATAMIGRATION 654F5AA3-4DB4-4C3A-8AF7-52CA3BBC5E2C*/ -- Change the script's code only if it needs to be executed again.

/*
Updating FK constraint metadata to match the new version of ReferencePropertyConstraintDatabaseDefinition,
in order to avoid unnecessary changes in database structure during deployment.
*/

UPDATE
    Rhetos.AppliedConcept
SET
    InfoType = 'Rhetos.Dsl.DefaultConcepts.ReferencePropertyDbConstraintInfo, Rhetos.Dsl.DefaultConcepts, Version=2.12.0.0, Culture=neutral, PublicKeyToken=null',
    ConceptInfoKey = REPLACE(ConceptInfoKey, 'PropertyInfo ', 'ReferencePropertyDbConstraintInfo '),
    CreateQuery = REPLACE(CreateQuery, '/*ReferencePropertyInfo FK options', '/*ReferencePropertyDbConstraintInfo FK options')
WHERE
    ImplementationType LIKE 'Rhetos.DatabaseGenerator.DefaultConcepts.ReferencePropertyConstraintDatabaseDefinition%';

DELETE d
FROM
    Rhetos.AppliedConceptDependsOn d
    INNER JOIN Rhetos.AppliedConcept c ON c.ID = d.DependsOnID
WHERE
    c.ImplementationType LIKE 'Rhetos.DatabaseGenerator.DefaultConcepts.ReferencePropertyConstraintDatabaseDefinition%';
