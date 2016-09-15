
		-- This procedure returns the next available code based on the given code format and the existing records in the database.
		-- Supported format allows any prefix with a generated numerical suffix.

		-- Possible format types with examples (CodeFormat => NewCode):

		-- A) If the given format ends with at least one "+", the new code will have the given prefix and the plus signs will be replaced with the next available number.
		--    If multiple plus signs are at the end, they define minimum len of suffix part (i.e. next available number prefixed with required number of "0").
		-- Examples:
		-- "ab+" => "ab1"
		-- "ab+" => "ab2"
		-- "ab+" => "ab3"
		-- "ab++++" => "ab0004"
		-- "c+" => "c1"
		-- "+" => "1"
		-- "+" => "2"
		-- Note: new code will maintain the length of the existing codes. For example, if the existing records are those above (last one for "ab" prefix "ab0004"):
		-- "ab+" => "ab0005"

		-- B) If the format doesn't end with "+", it is assumes the new code is explicitly defined.
		-- Examples:
		-- "123" => "123"
		-- "abc" => "abc"
		-- "" => ""

		-- C) If an unsupported format is given, the procedure will raise an error:
		-- Examples:
		-- "+123"

		-- D) If the format is not set (NULL), a simple counter will be used ("+").

		-- Filter parameter:
		-- Filter is used in a case when the code is not unique in the table/view, but is unique within a certain group.
		-- For example, if the table contains column Family, and the codes are generated starting from 1 for each family,
		-- then when inserting a record in Family "X" the procedure should be called with the filter "Family = 'X'".


        SET NOCOUNT ON
        SET ANSI_WARNINGS OFF

        IF @Filter = '' SET @Filter = NULL
        IF @CodeFormat IS NULL SET @CodeFormat = '+'


        --===================================================================
        -- If not using automatically generated code, return the given explicit code:

        IF CHARINDEX( '+', @CodeFormat ) = 0
        BEGIN
            SET @NewCode = @CodeFormat
            RETURN
        END


        --===================================================================
        -- Extract the prefix and calculate minimum length of numeric suffix 
        --		(defined by number of "+") at the end.
        
		DECLARE @MinSuffixLength INT
		SET @MinSuffixLength = LEN( @CodeFormat ) - CHARINDEX( '+', @CodeFormat) + 1
		
        DECLARE @Prefix NVARCHAR(256)
        SET @Prefix = LEFT( @CodeFormat, LEN( @CodeFormat ) - @MinSuffixLength )


        IF RIGHT( @CodeFormat, @MinSuffixLength ) <> REPLICATE( '+', @MinSuffixLength )
        BEGIN
            RAISERROR ( 'Invalid code is entered: The value must end with at least only one "+" character, at the end of the code (AutoCode). No "+" are allowed before those at the end.', 16, 10 )
            RETURN 50000
        END


        --===================================================================
        -- Find out the maximal numerical suffix in the existing records:

        CREATE TABLE #tmp_MaxSuffix
        (
            Num INT,
            Length INT
        )

        DECLARE @QueryizedPrefix NVARCHAR(2000)
        SET @QueryizedPrefix = '''' + REPLACE( @Prefix, '''', '''''' ) + ''''

        DECLARE @Query NVARCHAR(2000)

        SET @Query =
        'INSERT INTO
            #tmp_MaxSuffix (Num, Length)
        SELECT TOP 1
            Num = CONVERT( INT, SUBSTRING( ['+@ColumnName+'], LEN( '+@QueryizedPrefix+' )+1, 256 ) ),
            Length = LEN( ['+@ColumnName+'] ) - LEN( '+@QueryizedPrefix+' )
        FROM
            '+@TableOrView+'
        WHERE
            ['+@ColumnName+'] LIKE '+@QueryizedPrefix+' + ''%''
            AND ISNUMERIC(SUBSTRING( ['+@ColumnName+'], LEN( '+@QueryizedPrefix+' )+1, 256 )) = 1 
            AND CHARINDEX(''.'', SUBSTRING( ['+@ColumnName+'], LEN( '+@QueryizedPrefix+' )+1, 256 )) = 0
            AND CHARINDEX(''e'', SUBSTRING( ['+@ColumnName+'], LEN( '+@QueryizedPrefix+' )+1, 256 )) = 0'

        + ISNULL( '
            AND ( ' + @Filter + ' )', '' ) + '
        ORDER BY
            -- Find maximal numeric suffix:
            CONVERT( INT, SUBSTRING( ['+@ColumnName+'], LEN( '+@QueryizedPrefix+' )+1, 256 ) ) DESC,
            -- If there are more than one suffixes with same value, take the longest code:
            LEN( ['+@ColumnName+'] ) - LEN( '+@QueryizedPrefix+' ) DESC'


        EXECUTE (@Query)

        DECLARE @MaxSuffixNum INT
        DECLARE @MaxSuffixLength INT

        SELECT
            @MaxSuffixNum = Num,
            @MaxSuffixLength = Length
        FROM
            #tmp_MaxSuffix

        DROP TABLE #tmp_MaxSuffix

        -- If there are no old codes, numbering will start from 1 (single digit)
        IF @MaxSuffixNum IS NULL
        BEGIN
            SET @MaxSuffixNum = 0
            SET @MaxSuffixLength = 1
        END

        --===================================================================
        -- Compute next available code:
        
        IF @MaxSuffixLength < @MinSuffixLength
        BEGIN
			SET @MaxSuffixLength = @MinSuffixLength
        END
        

        DECLARE @NewNum INT
        SET @NewNum = @MaxSuffixNum+1

        DECLARE @NewNumFixLen NVARCHAR(256)
        SET @NewNumFixLen = CAST(@NewNum AS NVARCHAR(256))

        IF LEN(@NewNumFixLen) < @MaxSuffixLength
            SET @NewNumFixLen = REPLICATE ('0', @MaxSuffixLength - LEN(@NewNumFixLen)) + @NewNumFixLen

        SET @NewCode = @Prefix + @NewNumFixLen