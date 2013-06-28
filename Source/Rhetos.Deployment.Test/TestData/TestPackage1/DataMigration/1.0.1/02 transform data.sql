UPDATE Test.RegularTable
SET
	Description = 'Desc - ' + Name
WHERE
	Name NOT LIKE '%2'