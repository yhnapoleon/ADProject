-- 重新计算所有用户的总碳排放
-- 包括：ActivityLogs 的 TotalEmission + TravelLogs 的 CarbonEmission + UtilityBills 的 TotalCarbonEmission

-- 1. 更新所有用户的总碳排放
UPDATE ApplicationUsers
SET TotalCarbonEmission = (
    -- ActivityLogs 的碳排放总和
    ISNULL((SELECT SUM(TotalEmission) FROM ActivityLogs WHERE ActivityLogs.UserId = ApplicationUsers.Id), 0) +
    -- TravelLogs 的碳排放总和
    ISNULL((SELECT SUM(CarbonEmission) FROM TravelLogs WHERE TravelLogs.UserId = ApplicationUsers.Id), 0) +
    -- UtilityBills 的碳排放总和
    ISNULL((SELECT SUM(TotalCarbonEmission) FROM UtilityBills WHERE UtilityBills.UserId = ApplicationUsers.Id), 0)
);

-- 2. 验证更新结果（显示前10个用户）
SELECT TOP 10
    u.Id AS UserId,
    u.Username,
    u.TotalCarbonEmission AS UserTotalCarbonEmission,
    ISNULL((SELECT SUM(TotalEmission) FROM ActivityLogs WHERE UserId = u.Id), 0) AS ActivityEmission,
    ISNULL((SELECT SUM(CarbonEmission) FROM TravelLogs WHERE UserId = u.Id), 0) AS TravelEmission,
    ISNULL((SELECT SUM(TotalCarbonEmission) FROM UtilityBills WHERE UserId = u.Id), 0) AS UtilityEmission,
    (ISNULL((SELECT SUM(TotalEmission) FROM ActivityLogs WHERE UserId = u.Id), 0) +
     ISNULL((SELECT SUM(CarbonEmission) FROM TravelLogs WHERE UserId = u.Id), 0) +
     ISNULL((SELECT SUM(TotalCarbonEmission) FROM UtilityBills WHERE UserId = u.Id), 0)) AS CalculatedTotal
FROM ApplicationUsers u
WHERE u.TotalCarbonEmission > 0 OR 
      EXISTS(SELECT 1 FROM ActivityLogs WHERE UserId = u.Id) OR
      EXISTS(SELECT 1 FROM TravelLogs WHERE UserId = u.Id) OR
      EXISTS(SELECT 1 FROM UtilityBills WHERE UserId = u.Id)
ORDER BY u.TotalCarbonEmission DESC;

-- 3. 特别检查用户42
SELECT 
    'User 42 Details' AS Info,
    u.Id AS UserId,
    u.Username,
    u.TotalCarbonEmission AS UserTotalCarbonEmission,
    (SELECT COUNT(*) FROM TravelLogs WHERE UserId = 42) AS TravelLogCount,
    ISNULL((SELECT SUM(CarbonEmission) FROM TravelLogs WHERE UserId = 42), 0) AS TravelEmissionSum,
    (SELECT COUNT(*) FROM ActivityLogs WHERE UserId = 42) AS ActivityLogCount,
    ISNULL((SELECT SUM(TotalEmission) FROM ActivityLogs WHERE UserId = 42), 0) AS ActivityEmissionSum,
    (SELECT COUNT(*) FROM UtilityBills WHERE UserId = 42) AS UtilityBillCount,
    ISNULL((SELECT SUM(TotalCarbonEmission) FROM UtilityBills WHERE UserId = 42), 0) AS UtilityEmissionSum
FROM ApplicationUsers u
WHERE u.Id = 42;
