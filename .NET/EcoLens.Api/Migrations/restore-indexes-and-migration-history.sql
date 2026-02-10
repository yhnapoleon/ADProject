-- 1) 恢复被删掉的 3 个索引
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UtilityBills_UserId' AND object_id = OBJECT_ID('UtilityBills'))
    CREATE INDEX [IX_UtilityBills_UserId] ON [UtilityBills] ([UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TravelLogs_UserId' AND object_id = OBJECT_ID('TravelLogs'))
    CREATE INDEX [IX_TravelLogs_UserId] ON [TravelLogs] ([UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ActivityLogs_UserId' AND object_id = OBJECT_ID('ActivityLogs'))
    CREATE INDEX [IX_ActivityLogs_UserId] ON [ActivityLogs] ([UserId]);

-- 2) 把该迁移标记为已应用
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260209082557_AddPointAwardLogsEf')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260209082557_AddPointAwardLogsEf', N'8.0.23');

PRINT 'Done.';
