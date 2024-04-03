CREATE PROCEDURE Common.AutoCodeCacheUpdate
	@entity NVARCHAR(256),
	@property NVARCHAR(256),
	@grouping NVARCHAR(256),
	@prefix NVARCHAR(256),
	@minDigits INT,
	@providedCode INT -- Update the existing cache with the new code value, explicitly provided instead of generated. Either @quantity or @providedCode must be set.
AS
IF @grouping IS NULL SET @grouping = '';

UPDATE
	acc
SET
	MinDigits = CASE WHEN @providedCode > acc.LastCode THEN @minDigits ELSE acc.MinDigits END,
	LastCode = CASE WHEN @providedCode > acc.LastCode THEN @providedCode ELSE acc.LastCode END
FROM
	Common.AutoCodeCache acc WITH (ROWLOCK, UPDLOCK, INDEX(IX_AutoCodeCache_Entity_Property_Grouping_Prefix))
WHERE
	acc.Entity = @entity
	AND acc.Property = @property
	AND acc.Grouping = @grouping
	AND acc.Prefix = @prefix;

IF @@ROWCOUNT = 0
BEGIN
	INSERT INTO
		Common.AutoCodeCache WITH (ROWLOCK, UPDLOCK)
		(ID, Entity, Property, Grouping, Prefix, MinDigits, LastCode)
	VALUES
		(NEWID(), @entity, @property, @grouping, @prefix, @minDigits, @providedCode);
END
