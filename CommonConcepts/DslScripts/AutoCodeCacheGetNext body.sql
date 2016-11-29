IF @grouping IS NULL SET @grouping = '';

IF @quantity < 1
	BEGIN RAISERROR('Invalid argument: @quantity (%d). The value must be 1 or greater. ', 16, 10, @quantity); RETURN 1; END

DECLARE @newCode INT;

UPDATE
	acc
SET
	@minDigits = MinDigits = CASE WHEN @minDigits > acc.MinDigits THEN @minDigits ELSE acc.MinDigits END,
	@newCode = LastCode = acc.LastCode + @quantity
FROM
	Common.AutoCodeCache acc WITH (ROWLOCK, UPDLOCK, SERIALIZABLE, INDEX(IX_AutoCodeCache_Entity_Property_Grouping_Prefix))
WHERE
	acc.Entity = @entity
	AND acc.Property = @property
	AND acc.Grouping = @grouping
	AND acc.Prefix = @prefix;

IF @@ROWCOUNT = 0
BEGIN
	SET @minDigits = ISNULL(@minDigits, 0);
	SET @newCode = @quantity;

	INSERT INTO
		Common.AutoCodeCache WITH (ROWLOCK, UPDLOCK)
		(ID, Entity, Property, Grouping, Prefix, MinDigits, LastCode)
	VALUES
		(NEWID(), @entity, @property, @grouping, @prefix, @minDigits, @newCode);
END

SELECT MinDigits = @minDigits, NewCode = @newCode;
