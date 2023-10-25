@entity NVARCHAR(256),
@property NVARCHAR(256),
@grouping NVARCHAR(256),
@prefix NVARCHAR(256),
@minDigits INT,
@providedCode INT -- Update the existing cache with the new code value, explicitly provided instead of generated. Either @quantity or @providedCode must be set.
