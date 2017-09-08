/*DATAMIGRATION 0246E7EA-F2F2-419B-9710-1149838E8604*/ -- Change the script's code only if it needs to be executed again.

-- Updating existing database metadata to avoid regeneration of all database objects.
-- The concept's key and implementation is changed from DataStructureExtends* to UniqueReference*,
-- but the implementation of the existing database objects did not change.

UPDATE
    Rhetos.AppliedConcept
SET
    ConceptInfoKey = REPLACE(ConceptInfoKey, 'DataStructureExtends', 'UniqueReference'),
    ImplementationType = REPLACE(ImplementationType, 'DataStructureExtends', 'UniqueReference'),
    CreateQuery = REPLACE(CreateQuery, 'DataStructureExtends', 'UniqueReference')
WHERE
    ConceptInfoKey LIKE 'DataStructureExtendsInfo %'
