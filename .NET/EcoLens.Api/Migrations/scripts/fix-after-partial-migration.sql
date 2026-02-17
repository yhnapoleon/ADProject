-- ============================================================
-- Run this ONCE after the migration failed at "Add PassengerCount".
-- Your DB already has: PointAwardLogs table + index.
-- The migration had already dropped 3 indexes before it failed.
-- This script: re-creates those indexes and marks the migration as applied.
-- ============================================================

-- 1) Re-create the indexes that were dropped (ignore if already exists)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UtilityBills_UserId' AND object_id = OBJECT_ID('UtilityBills'))
    CREATE INDEX [IX_UtilityBills_UserId] ON [UtilityBills] ([UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TravelLogs_UserId' AND object_id = OBJECT_ID('TravelLogs'))
    CREATE INDEX [IX_TravelLogs_UserId] ON [TravelLogs] ([UserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ActivityLogs_UserId' AND object_id = OBJECT_ID('ActivityLogs'))
    CREATE INDEX [IX_ActivityLogs_UserId] ON [ActivityLogs] ([UserId]);

-- 2) Mark the migration as applied so "dotnet ef database update" won't run it again
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260209082557_AddPointAwardLogsEf')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260209082557_AddPointAwardLogsEf', N'8.0.23');
