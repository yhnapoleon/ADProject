-- 初始化现有用户的总碳排放数据
-- 计算方式：ActivityLogs 的 TotalEmission + TravelLogs 的 CarbonEmission + UtilityBills 的 TotalCarbonEmission

USE [ecolens];
GO

-- 更新所有用户的总碳排放
UPDATE u
SET u.TotalCarbonEmission = 
    ISNULL((
        SELECT SUM(al.TotalEmission)
        FROM ActivityLogs al
        WHERE al.UserId = u.Id
    ), 0) +
    ISNULL((
        SELECT SUM(tl.CarbonEmission)
        FROM TravelLogs tl
        WHERE tl.UserId = u.Id
    ), 0) +
    ISNULL((
        SELECT SUM(ub.TotalCarbonEmission)
        FROM UtilityBills ub
        WHERE ub.UserId = u.Id
    ), 0)
FROM ApplicationUsers u;
GO

-- 验证更新结果
SELECT 
    u.Id,
    u.Username,
    u.TotalCarbonEmission,
    ISNULL((
        SELECT SUM(al.TotalEmission)
        FROM ActivityLogs al
        WHERE al.UserId = u.Id
    ), 0) AS ActivityEmission,
    ISNULL((
        SELECT SUM(tl.CarbonEmission)
        FROM TravelLogs tl
        WHERE tl.UserId = u.Id
    ), 0) AS TravelEmission,
    ISNULL((
        SELECT SUM(ub.TotalCarbonEmission)
        FROM UtilityBills ub
        WHERE ub.UserId = u.Id
    ), 0) AS UtilityEmission
FROM ApplicationUsers u
ORDER BY u.TotalCarbonEmission DESC;
GO
