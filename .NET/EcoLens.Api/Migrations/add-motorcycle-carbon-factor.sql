-- 若之前只有 ElectricBike，添加 Motorcycle（烧油的摩托车）碳因子，与 TransportMode.Motorcycle 对应
IF NOT EXISTS (SELECT * FROM [CarbonReferences] WHERE [LabelName] = 'Motorcycle')
BEGIN
    INSERT INTO [CarbonReferences] ([LabelName], [Category], [Co2Factor], [Unit], [Source], [CreatedAt], [UpdatedAt])
    VALUES ('Motorcycle', 1, 0.12, 'kgCO2/km', 'Local', GETUTCDATE(), GETUTCDATE());
    PRINT 'Added Motorcycle carbon factor';
END
ELSE
BEGIN
    PRINT 'Motorcycle carbon factor already exists';
END
