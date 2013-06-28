DECLARE
  cnt INT;
BEGIN

  SELECT COUNT(*) INTO cnt FROM user_tables WHERE table_name = upper('DslScript');
  IF cnt=0 THEN EXECUTE IMMEDIATE
		'CREATE TABLE DslScript
		(
			ID RAW(16) DEFAULT SYS_GUID() NOT NULL,
			CONSTRAINT PK_DslScript PRIMARY KEY (ID),
			Name nvarchar2(256),
			Dsl NCLOB,
			LastModified date DEFAULT SYSDATE NOT NULL,
			AppliedBy nvarchar2(256) DEFAULT upper(SYS_CONTEXT(''USERENV'', ''OS_USER'')) NOT NULL,
			Client nvarchar2(256) DEFAULT upper(SYS_CONTEXT(''USERENV'',''HOST'')) NOT NULL,
			Server nvarchar2(256) DEFAULT upper(SYS_CONTEXT(''USERENV'',''SERVER_HOST'')) NOT NULL
		)';
  END IF;

  SELECT COUNT(*) INTO cnt FROM user_tables WHERE table_name = upper('DataMigrationScript');
  IF cnt=0 THEN EXECUTE IMMEDIATE
		'CREATE TABLE DataMigrationScript
		(
			ID RAW(16) DEFAULT SYS_GUID() NOT NULL,
			CONSTRAINT PK_DataMigrationScript PRIMARY KEY (ID),
			Tag nvarchar2(256) NOT NULL,
			Path nvarchar2(256) NOT NULL,
			Content NCLOB,
			DateExecuted date DEFAULT SYSDATE NOT NULL,
			ExecutedBy nvarchar2(256) DEFAULT upper(SYS_CONTEXT(''USERENV'', ''OS_USER'')) NULL,
			Client nvarchar2(256) DEFAULT upper(SYS_CONTEXT(''USERENV'',''HOST'')) NULL,
			Server nvarchar2(256) DEFAULT upper(SYS_CONTEXT(''USERENV'',''SERVER_HOST'')) NULL,
			OrderExecuted NUMBER(10) NOT NULL
		)';
  END IF;

  SELECT COUNT(*) INTO cnt FROM user_indexes WHERE index_name = upper('IX_DataMigrationScript_Tag');
  IF cnt=0 THEN EXECUTE IMMEDIATE
		'CREATE UNIQUE INDEX IX_DataMigrationScript_Tag ON DataMigrationScript (Tag)';
  END IF;

  SELECT COUNT(*) INTO cnt FROM user_sequences WHERE sequence_name = upper('SeqDataMigrationScript');
  IF cnt=0 THEN EXECUTE IMMEDIATE
		'CREATE SEQUENCE SeqDataMigrationScript
		   START WITH 1
		   INCREMENT BY 1
		   NOCYCLE';
  END IF;

  SELECT COUNT(*) INTO cnt FROM user_triggers WHERE trigger_name = upper('DataMigrationScriptSeq');
  IF cnt=0 THEN EXECUTE IMMEDIATE
		'CREATE OR REPLACE TRIGGER DataMigrationScriptSeq
		BEFORE INSERT ON DataMigrationScript
		FOR EACH ROW
		BEGIN
		   SELECT SeqDataMigrationScript.NEXTVAL
		   INTO :NEW.OrderExecuted
		   FROM Dual;
		END;';
  END IF;

  SELECT COUNT(*) INTO cnt FROM user_tables WHERE table_name = upper('AppliedConcept');
  IF cnt=0 THEN EXECUTE IMMEDIATE
		'CREATE TABLE AppliedConcept
		(
			ID RAW(16) DEFAULT SYS_GUID() NOT NULL,
			CONSTRAINT PK_AppliedConcept PRIMARY KEY (ID),
			InfoType NCLOB NOT NULL,
			ImplementationType NCLOB NOT NULL,
			SerializedInfo NCLOB NOT NULL,
			LastModified date DEFAULT SYSDATE NOT NULL,
			AppliedBy nvarchar2(256) DEFAULT upper(SYS_CONTEXT(''USERENV'', ''OS_USER'')) NULL,
			Client nvarchar2(256) DEFAULT upper(SYS_CONTEXT(''USERENV'',''HOST'')) NULL,
			Server nvarchar2(256) DEFAULT upper(SYS_CONTEXT(''USERENV'',''SERVER_HOST'')) NULL,
			ModificationOrder NUMBER(10) NOT NULL,
			CreateQuery NCLOB,
			DependsOn NCLOB,
			ConceptImplementationVersion nvarchar2(256) NOT NULL
		)';
  END IF;

  SELECT COUNT(*) INTO cnt FROM user_sequences WHERE sequence_name = upper('SeqAppliedConcept');
  IF cnt=0 THEN EXECUTE IMMEDIATE
		'CREATE SEQUENCE SeqAppliedConcept
		   START WITH 1
		   INCREMENT BY 1
		   NOCYCLE';
  END IF;

  SELECT COUNT(*) INTO cnt FROM user_triggers WHERE trigger_name = upper('AppliedConceptSeq');
  IF cnt=0 THEN EXECUTE IMMEDIATE
		'CREATE OR REPLACE TRIGGER AppliedConceptSeq
		BEFORE INSERT ON AppliedConcept
		FOR EACH ROW
		BEGIN
		   SELECT SeqAppliedConcept.NEXTVAL
		   INTO :NEW.ModificationOrder
		   FROM Dual;
		END;';
  END IF;
  
  SELECT COUNT(*) INTO cnt FROM user_tables WHERE table_name = upper('AppliedConceptDependsOn');
  IF cnt=0 THEN EXECUTE IMMEDIATE
		'CREATE TABLE AppliedConceptDependsOn
		(
			ID RAW(16) DEFAULT SYS_GUID() NOT NULL,
			CONSTRAINT PK_AppliedConceptDependsOn PRIMARY KEY (ID),
			DependentID RAW(16) NOT NULL,
			CONSTRAINT FK_AppliedConceptDependent FOREIGN KEY (DependentID) REFERENCES Rhetos.AppliedConcept (ID) ON DELETE CASCADE,
			DependsOnID RAW(16)  NOT NULL,
			CONSTRAINT FK_AppliedConceptDependsOn FOREIGN KEY (DependsOnID) REFERENCES Rhetos.AppliedConcept (ID),
			CONSTRAINT UQ_AppliedConceptDependency UNIQUE (DependentID, DependsOnID)
		)';
  END IF;
  
  SELECT COUNT(*) INTO cnt FROM user_tab_columns WHERE TABLE_NAME = upper('AppliedConcept') AND COLUMN_NAME = upper('DependsOn');
  IF cnt>0 THEN EXECUTE IMMEDIATE
    'ALTER TABLE AppliedConcept DROP COLUMN DependsOn';
  END IF;

  SELECT COUNT(*) INTO cnt FROM user_tab_columns WHERE TABLE_NAME = upper('AppliedConcept') AND COLUMN_NAME = upper('RemoveQuery');
  IF cnt=0 THEN
    EXECUTE IMMEDIATE 'ALTER TABLE Rhetos.AppliedConcept ADD RemoveQuery NCLOB DEFAULT ''/*UNKNOWN*/''';
    EXECUTE IMMEDIATE 'ALTER TABLE Rhetos.AppliedConcept MODIFY RemoveQuery DEFAULT NULL';
  END IF;

  SELECT COUNT(*) INTO cnt FROM user_tab_columns WHERE TABLE_NAME = upper('AppliedConcept') AND COLUMN_NAME = upper('ConceptInfoKey');
    IF cnt=0 THEN
    EXECUTE IMMEDIATE 'ALTER TABLE Rhetos.AppliedConcept ADD ConceptInfoKey NCLOB DEFAULT ''/*UNKNOWN*/'' NOT NULL';
    EXECUTE IMMEDIATE 'ALTER TABLE Rhetos.AppliedConcept MODIFY ConceptInfoKey DEFAULT NULL';
  END IF;

  SELECT COUNT(*) INTO cnt FROM user_indexes WHERE index_name = upper('IX_DataMigrationScript_Tag');
  IF cnt>0 THEN
		EXECUTE IMMEDIATE 'DROP INDEX Rhetos.IX_DataMigrationScript_Tag';
  END IF;
  
  SELECT COUNT(*) INTO cnt FROM user_indexes WHERE index_name = upper('IX_DataMigrationScript_Tag_CI');
  IF cnt=0 THEN
    EXECUTE IMMEDIATE 'CREATE UNIQUE INDEX IX_DataMigrationScript_Tag_CI ON DataMigrationScript (NLSSORT(Tag, ''NLS_SORT=GENERIC_M_CI''))';
  END IF;

END;
