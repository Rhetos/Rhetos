SELECT
    ID,
    LogID,
    TableName,
    ItemId,
    Relation
FROM
    Common.LogRelatedItem WITH (NOLOCK)

/*Common.LogRelatedItemReader AdditionalSource*/
