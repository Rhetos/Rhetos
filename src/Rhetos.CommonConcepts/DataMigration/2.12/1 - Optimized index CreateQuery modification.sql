/*DATAMIGRATION F0F1C9B6-058D-41DF-8514-DF9D7BD2D701*/ -- Change the script's code only if it needs to be executed again.

/*
This data-migration script manually adds a new option in the CreateQuery for indexes.
This will prevent Rhetos from automatically changing CreateQuery to the new version,
in order to avoid re-creating the indexes that might take long time on large databases.
*/

SELECT
    NewCreateQuery = CASE WHEN CHARINDEX('CLUSTERED ' + Tag1, CreateQuery) <> 0
        THEN REPLACE(CreateQuery, 'CLUSTERED ' + Tag1, Tag0 + ' CLUSTERED ' + Tag1)
        ELSE REPLACE(CreateQuery, Tag1, Tag0 + ' ' + Tag1)
        END,
    *
INTO #updates
FROM
    (
        SELECT
            Tag0 = '/*SqlIndexMultipleInfo Options0 ' + KeyProperties + '*/',
            Tag1 = '/*SqlIndexMultipleInfo Options1 ' + KeyProperties + '*/',
            *
        FROM
            (
                SELECT
                    ID,
                    CreateQuery,
                    KeyProperties = SUBSTRING(ConceptInfoKey, 22, LEN(ConceptInfoKey))
                FROM
                    Rhetos.AppliedConcept
                WHERE
                    ConceptInfoKey LIKE 'SqlIndexMultipleInfo %'
            ) x
    ) x
WHERE
    CreateQuery NOT LIKE '%' + Tag0 + '%'

UPDATE
    ac
SET
    CreateQuery = #updates.NewCreateQuery
FROM
    Rhetos.AppliedConcept ac
    INNER JOIN #updates ON #updates.ID = ac.ID;

UPDATE
    Rhetos.AppliedConcept
SET
    InfoType = REPLACE(InfoType, 'UniqueMultiplePropertiesInfo', 'SqlIndexMultipleInfo'),
    SerializedInfo = REPLACE(SerializedInfo, 'UniqueMultiplePropertiesInfo', 'SqlIndexMultipleInfo')
WHERE
    ImplementationType LIKE 'Rhetos.DatabaseGenerator.DefaultConcepts.SqlIndexMultipleDatabaseDefinition%';

DELETE
    d
FROM
    Rhetos.AppliedConceptDependsOn d
    INNER JOIN Rhetos.AppliedConcept c ON c.ID = d.DependsOnID
WHERE
    c.InfoType LIKE 'Rhetos.Dsl.DefaultConcepts.UniqueMultiplePropertiesInfo%'
    and c.CreateQuery = '';
