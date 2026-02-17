-- 检查用户42的出行记录和总碳排放
DECLARE @UserId INT = 42;

-- 1. 查看用户的总碳排放
SELECT 
    Id AS UserId,
    Username,
    TotalCarbonEmission,
    TotalCarbonSaved
FROM ApplicationUsers
WHERE Id = @UserId;

-- 2. 查看用户的出行记录
SELECT 
    Id,
    TransportMode,
    OriginAddress,
    DestinationAddress,
    DistanceKilometers,
    CarbonEmission,
    CreatedAt
FROM TravelLogs
WHERE UserId = @UserId
ORDER BY CreatedAt DESC;

-- 3. 计算出行记录的碳排放总和
SELECT 
    'TravelLogs Total' AS Source,
    SUM(CarbonEmission) AS TotalEmission
FROM TravelLogs
WHERE UserId = @UserId;

-- 4. 计算活动记录的碳排放总和
SELECT 
    'ActivityLogs Total' AS Source,
    SUM(TotalEmission) AS TotalEmission
FROM ActivityLogs
WHERE UserId = @UserId;

-- 5. 计算水电账单的碳排放总和
SELECT 
    'UtilityBills Total' AS Source,
    SUM(TotalCarbonEmission) AS TotalEmission
FROM UtilityBills
WHERE UserId = @UserId;

-- 6. 计算应该的总碳排放（三个来源的总和）
SELECT 
    'Expected Total' AS Source,
    (SELECT ISNULL(SUM(CarbonEmission), 0) FROM TravelLogs WHERE UserId = @UserId) +
    (SELECT ISNULL(SUM(TotalEmission), 0) FROM ActivityLogs WHERE UserId = @UserId) +
    (SELECT ISNULL(SUM(TotalCarbonEmission), 0) FROM UtilityBills WHERE UserId = @UserId) AS TotalEmission;
