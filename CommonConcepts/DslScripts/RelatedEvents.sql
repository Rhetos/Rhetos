SELECT
    ID = lri.ID,
    Created = l.Created,
    UserName = l.UserName,
    Workstation = l.Workstation,
    ContextInfo = l.ContextInfo,
    Action = l.Action,
    TableName = l.TableName,
    ItemId = l.ItemId,
    Description = l.Description,
    LogID = l.ID,
    lri.Relation,
    lri.TableName AS RelatedToTable,
    lri.ItemId AS RelatedToItem
FROM
    Common.LogRelatedItemNoLock lri
    INNER JOIN Common.LogNoLock l ON l.ID = lri.LogID

UNION ALL

SELECT
    ID = l.ID,
    Created = l.Created,
    UserName = l.UserName,
    Workstation = l.Workstation,
    ContextInfo = l.ContextInfo,
    Action = l.Action,
    TableName = l.TableName,
    ItemId = l.ItemId,
    Description = l.Description,
    LogID = l.ID,
    N'' AS Relation,
    l.TableName AS RelatedToTable,
    l.ItemId AS RelatedToItem
FROM
    Common.LogNoLock l
