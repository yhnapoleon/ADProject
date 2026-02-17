-- 为所有用户填充碳减排 TotalCarbonSaved 和积分 CurrentPoints（随机合理范围）
SET NOCOUNT ON;

-- TotalCarbonSaved: 约 5～500 kg，CurrentPoints: 约 50～2000
UPDATE u
SET
    TotalCarbonSaved = ROUND(5.0 + (ABS(CHECKSUM(u.Id)) % 4960) / 10.0, 2),
    CurrentPoints = 50 + (ABS(CHECKSUM(u.Id * 11)) % 1951)
FROM ApplicationUsers u;

PRINT 'Updated TotalCarbonSaved and CurrentPoints for ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + ' users.';
PRINT 'Done.';
