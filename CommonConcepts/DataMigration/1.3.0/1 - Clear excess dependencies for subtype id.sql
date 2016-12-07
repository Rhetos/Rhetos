/*DATAMIGRATION B3D3CD35-D41F-4E70-A490-94CBD12226FF*/

DELETE
	acdo
FROM
    Rhetos.AppliedConceptDependsOn acdo
    INNER JOIN Rhetos.AppliedConcept acDependent ON acDependent.ID = acdo.DependentID
    INNER JOIN Rhetos.AppliedConcept acDependsOn ON acDependsOn.ID = acdo.DependsOnID
WHERE
    acDependent.ConceptInfoKey like 'SqlObjectInfo %'
    and acDependent.CreateQuery like 'ALTER TABLE%BINARY(4)%PERSISTED NOT NULL%'
    and (acDependsOn.ConceptInfoKey not like 'DataStructureInfo %' and acDependsOn.ConceptInfoKey not like 'ModuleInfo %')
