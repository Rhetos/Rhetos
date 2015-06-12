@entity NVARCHAR(256),
@property NVARCHAR(256),
@grouping NVARCHAR(256),
@prefix NVARCHAR(256),
@minDigits INT,
@quantity INT -- Number of generated codes (i.e., the number of inserted items). Must be 1 or greater. For @quantity greater than 1, @newCode will contain the last generated codes. The generated codes are from @newCode-@quantity+1 to @newCode.
