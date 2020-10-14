SELECT
    ID,
    Created,
    UserName,
    Workstation,
    ContextInfo,
    Action,
    TableName,
    ItemId,
    Description
FROM
    Common.Log WITH (NOLOCK)

/*Common.LogReader AdditionalSource*/
