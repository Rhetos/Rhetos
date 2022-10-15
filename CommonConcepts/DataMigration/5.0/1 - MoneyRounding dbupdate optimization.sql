/*DATAMIGRATION 59F0C660-0B72-407E-9E66-ED6174D6457D*/ -- Change the script's code only if it needs to be executed again.

-- This script modifies metadata for the 'money' properties in database in order to optimize dbupdate from Rhetos v1-v4 to Rhetos v5:
-- For each money property it creates a dependency to 'MoneyRoundingInfo' concept as it would be created by Rhetos 5.
-- This dependency does not affect database structure, but if Rhetos 5 creates it would refresh (drop and create)
-- the money column, just in case it affects the colum creation, as a regular robust Rhetos behavior.
-- By manually adding the dependency we can skip the column refresh.

SELECT
    *,
    MoneyRoundingConceptInfoKey = 'MoneyRoundingInfo'
        + SUBSTRING(m.ConceptInfoKey, 13, CHARINDEX('.', m.ConceptInfoKey, CHARINDEX('.', m.ConceptInfoKey) + 1) - 13)
INTO
    #moneyProperties
FROM
    Rhetos.AppliedConcept m
WHERE
    m.InfoType LIKE 'Rhetos.Dsl.DefaultConcepts.MoneyPropertyInfo,%'
    AND m.ImplementationType LIKE 'Rhetos.DatabaseGenerator.DefaultConcepts.MoneyPropertyDatabaseDefinition,%';

SELECT DISTINCT
    ID = NEWID(),
    InfoType = 'Rhetos.Dsl.DefaultConcepts.MoneyRoundingInfo, Rhetos.Dsl.DefaultConcepts, Version=5.4.0.0, Culture=neutral, PublicKeyToken=null',
    ImplementationType = 'Rhetos.DatabaseGenerator.NullImplementation, Rhetos.DatabaseGenerator, Version=5.4.0.0, Culture=neutral, PublicKeyToken=null',
    SerializedInfo = NULL,
    CreateQuery = '',
    ConceptImplementationVersion = NULL,
    RemoveQuery = '',
    ConceptInfoKey = m.MoneyRoundingConceptInfoKey
INTO
    #moneyRounding
FROM
    (
        SELECT DISTINCT -- Multiple money properties on a single entity will create a single MoneyRoundingInfo.
            MoneyRoundingConceptInfoKey
        FROM
            #moneyProperties
    ) m
    LEFT JOIN Rhetos.AppliedConcept existingRounding
        ON existingRounding.ConceptInfoKey = m.MoneyRoundingConceptInfoKey
        AND existingRounding.ImplementationType LIKE 'Rhetos.DatabaseGenerator.NullImplementation,%'
WHERE
    existingRounding.ID IS NULL;

INSERT Rhetos.AppliedConcept (ID, InfoType, ImplementationType, SerializedInfo, CreateQuery, ConceptImplementationVersion, RemoveQuery, ConceptInfoKey)
SELECT ID, InfoType, ImplementationType, SerializedInfo, CreateQuery, ConceptImplementationVersion, RemoveQuery, ConceptInfoKey
FROM #moneyRounding;

INSERT Rhetos.AppliedConceptDependsOn (ID, DependentID, DependsOnID)
SELECT NEWID(), m.ID, r.ID
FROM
    #moneyProperties m
    INNER JOIN #moneyRounding r ON r.ConceptInfoKey = m.MoneyRoundingConceptInfoKey
