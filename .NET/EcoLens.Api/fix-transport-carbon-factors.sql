-- 添加缺失的出行碳排放因子
-- 这些数据应该在迁移中，但由于迁移被清理，需要手动添加

-- 检查并添加缺失的出行碳排放因子
-- 注意：使用 IF NOT EXISTS 避免重复插入

-- 1. Walking (步行) - 0 排放
IF NOT EXISTS (SELECT * FROM [CarbonReferences] WHERE [LabelName] = 'Walking')
BEGIN
    INSERT INTO [CarbonReferences] ([LabelName], [Category], [Co2Factor], [Unit], [Source], [CreatedAt], [UpdatedAt])
    VALUES ('Walking', 1, 0.0, 'kgCO2/km', 'Local', GETUTCDATE(), GETUTCDATE());
    PRINT 'Added Walking carbon factor';
END
ELSE
BEGIN
    PRINT 'Walking carbon factor already exists';
END

-- 2. Bicycle (自行车) - 0 排放
IF NOT EXISTS (SELECT * FROM [CarbonReferences] WHERE [LabelName] = 'Bicycle')
BEGIN
    INSERT INTO [CarbonReferences] ([LabelName], [Category], [Co2Factor], [Unit], [Source], [CreatedAt], [UpdatedAt])
    VALUES ('Bicycle', 1, 0.0, 'kgCO2/km', 'Local', GETUTCDATE(), GETUTCDATE());
    PRINT 'Added Bicycle carbon factor';
END
ELSE
BEGIN
    PRINT 'Bicycle carbon factor already exists';
END

-- 3. ElectricBike (电动车)
IF NOT EXISTS (SELECT * FROM [CarbonReferences] WHERE [LabelName] = 'ElectricBike')
BEGIN
    INSERT INTO [CarbonReferences] ([LabelName], [Category], [Co2Factor], [Unit], [Source], [CreatedAt], [UpdatedAt])
    VALUES ('ElectricBike', 1, 0.02, 'kgCO2/km', 'Local', GETUTCDATE(), GETUTCDATE());
    PRINT 'Added ElectricBike carbon factor';
END
ELSE
BEGIN
    PRINT 'ElectricBike carbon factor already exists';
END

-- 4. Bus (公交车)
IF NOT EXISTS (SELECT * FROM [CarbonReferences] WHERE [LabelName] = 'Bus')
BEGIN
    INSERT INTO [CarbonReferences] ([LabelName], [Category], [Co2Factor], [Unit], [Source], [CreatedAt], [UpdatedAt])
    VALUES ('Bus', 1, 0.05, 'kgCO2/km', 'Local', GETUTCDATE(), GETUTCDATE());
    PRINT 'Added Bus carbon factor';
END
ELSE
BEGIN
    PRINT 'Bus carbon factor already exists';
END

-- 5. Taxi (出租车/网约车)
IF NOT EXISTS (SELECT * FROM [CarbonReferences] WHERE [LabelName] = 'Taxi')
BEGIN
    INSERT INTO [CarbonReferences] ([LabelName], [Category], [Co2Factor], [Unit], [Source], [CreatedAt], [UpdatedAt])
    VALUES ('Taxi', 1, 0.2, 'kgCO2/km', 'Local', GETUTCDATE(), GETUTCDATE());
    PRINT 'Added Taxi carbon factor';
END
ELSE
BEGIN
    PRINT 'Taxi carbon factor already exists';
END

-- 6. CarGasoline (私家车-汽油)
IF NOT EXISTS (SELECT * FROM [CarbonReferences] WHERE [LabelName] = 'CarGasoline')
BEGIN
    INSERT INTO [CarbonReferences] ([LabelName], [Category], [Co2Factor], [Unit], [Source], [CreatedAt], [UpdatedAt])
    VALUES ('CarGasoline', 1, 0.2, 'kgCO2/km', 'Local', GETUTCDATE(), GETUTCDATE());
    PRINT 'Added CarGasoline carbon factor';
END
ELSE
BEGIN
    PRINT 'CarGasoline carbon factor already exists';
END

-- 7. CarElectric (私家车-电动车)
IF NOT EXISTS (SELECT * FROM [CarbonReferences] WHERE [LabelName] = 'CarElectric')
BEGIN
    INSERT INTO [CarbonReferences] ([LabelName], [Category], [Co2Factor], [Unit], [Source], [CreatedAt], [UpdatedAt])
    VALUES ('CarElectric', 1, 0.05, 'kgCO2/km', 'Local', GETUTCDATE(), GETUTCDATE());
    PRINT 'Added CarElectric carbon factor';
END
ELSE
BEGIN
    PRINT 'CarElectric carbon factor already exists';
END

-- 8. Ship (轮船)
IF NOT EXISTS (SELECT * FROM [CarbonReferences] WHERE [LabelName] = 'Ship')
BEGIN
    INSERT INTO [CarbonReferences] ([LabelName], [Category], [Co2Factor], [Unit], [Source], [CreatedAt], [UpdatedAt])
    VALUES ('Ship', 1, 0.03, 'kgCO2/km', 'Local', GETUTCDATE(), GETUTCDATE());
    PRINT 'Added Ship carbon factor';
END
ELSE
BEGIN
    PRINT 'Ship carbon factor already exists';
END

-- 9. Plane (飞机)
IF NOT EXISTS (SELECT * FROM [CarbonReferences] WHERE [LabelName] = 'Plane')
BEGIN
    INSERT INTO [CarbonReferences] ([LabelName], [Category], [Co2Factor], [Unit], [Source], [CreatedAt], [UpdatedAt])
    VALUES ('Plane', 1, 0.25, 'kgCO2/km', 'Local', GETUTCDATE(), GETUTCDATE());
    PRINT 'Added Plane carbon factor';
END
ELSE
BEGIN
    PRINT 'Plane carbon factor already exists';
END

PRINT 'All transport carbon factors have been checked and added!';
