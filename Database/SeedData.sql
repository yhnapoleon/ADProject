-- ============================================================
-- EcoLens Seed Data / Demo Data (for local run & submission)
-- 仅包含演示用测试账号与必要参考数据，不含真实敏感数据。
-- 执行顺序：先运行 Schema.sql，再运行本文件。
-- ============================================================

SET NOCOUNT ON;
GO

-- 1) 测试用户 (Test User)
-- 账号: demo@ecolens.local  密码: Demo123!
-- PasswordHash = SHA256("Demo123!") 的十六进制
IF NOT EXISTS (SELECT 1 FROM [ApplicationUsers] WHERE [Email] = N'demo@ecolens.local')
BEGIN
    DECLARE @now DATETIME2 = SYSUTCDATETIME();
    INSERT INTO [ApplicationUsers] (
        [Username], [Email], [PasswordHash], [Role], [AvatarUrl],
        [TotalCarbonSaved], [TotalCarbonEmission], [CurrentPoints], [Region], [BirthDate],
        [IsActive], [CreatedAt], [UpdatedAt],
        [Nickname], [ContinuousNeutralDays], [CurrentTreeProgress], [StepsUsedToday], [TreesTotalCount]
    )
    VALUES (
        N'demo',
        N'demo@ecolens.local',
        N'588C55F3CE2B8569B153C5ABBF13F9F74308B88A20017CC699B835CC93195D16',
        0,
        NULL,
        0, 0, 0, N'Singapore', '1990-01-01',
        1, @now, @now,
        N'Demo User', 0, 0, 0, 0
    );
END
GO

-- 2) 若 Schema 中未包含 CarbonReferences 初始数据，可取消下面注释并执行（与 EF InitialCreate 一致）
/*
INSERT INTO [CarbonReferences] ([LabelName],[Category],[Co2Factor],[Unit],[Region],[Source],[ClimatiqActivityId],[CreatedAt],[UpdatedAt])
VALUES
 (N'Beef', 0, 27.0, N'kgCO2', NULL, N'Local', NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
 (N'Subway', 1, 0.03, N'kgCO2/km', NULL, N'Local', NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
 (N'Electricity', 2, 0.5, N'kgCO2/kWh', NULL, N'Local', NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
 (N'Water', 2, 0.35, N'kgCO2/m3', NULL, N'Local', NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
 (N'Gas', 2, 2.3, N'kgCO2/unit', NULL, N'Local', NULL, SYSUTCDATETIME(), SYSUTCDATETIME());
*/

-- 3) 若 Schema 中未包含 SystemSettings，可取消下面注释
/*
IF NOT EXISTS (SELECT 1 FROM [SystemSettings] WHERE [Id] = 1)
INSERT INTO [SystemSettings] ([Id],[ConfidenceThreshold],[VisionModel],[WeeklyDigest],[MaintenanceMode])
VALUES (1, 80, N'default', 1, 0);
*/

PRINT N'SeedData.sql completed.';
GO
