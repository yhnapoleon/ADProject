IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE TABLE [ApplicationUsers] (
        [Id] int NOT NULL IDENTITY,
        [Username] nvarchar(100) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [PasswordHash] nvarchar(512) NOT NULL,
        [Role] int NOT NULL,
        [AvatarUrl] nvarchar(1024) NULL,
        [TotalCarbonSaved] decimal(18,2) NOT NULL,
        [CurrentPoints] int NOT NULL,
        [Region] nvarchar(100) NOT NULL,
        [BirthDate] datetime2 NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ApplicationUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE TABLE [CarbonReferences] (
        [Id] int NOT NULL IDENTITY,
        [LabelName] nvarchar(100) NOT NULL,
        [Category] int NOT NULL,
        [Co2Factor] decimal(18,4) NOT NULL,
        [Unit] nvarchar(50) NOT NULL,
        [Region] nvarchar(100) NULL,
        [Source] nvarchar(50) NOT NULL,
        [ClimatiqActivityId] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CarbonReferences] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE TABLE [DietTemplates] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [TemplateName] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_DietTemplates] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE TABLE [SystemSettings] (
        [Id] int NOT NULL IDENTITY,
        [ConfidenceThreshold] int NOT NULL,
        [VisionModel] nvarchar(100) NOT NULL,
        [WeeklyDigest] bit NOT NULL,
        [MaintenanceMode] bit NOT NULL,
        CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE TABLE [AiInsights] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [Type] int NOT NULL,
        [IsRead] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AiInsights] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AiInsights_ApplicationUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE TABLE [Posts] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [ImageUrls] nvarchar(max) NULL,
        [Type] int NOT NULL,
        [ViewCount] int NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Posts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Posts_ApplicationUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE TABLE [StepRecords] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [StepCount] int NOT NULL,
        [RecordDate] datetime2 NOT NULL,
        [CarbonOffset] decimal(18,4) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_StepRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StepRecords_ApplicationUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE TABLE [TravelLogs] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [TransportMode] int NOT NULL,
        [OriginAddress] nvarchar(500) NOT NULL,
        [OriginLatitude] decimal(10,7) NOT NULL,
        [OriginLongitude] decimal(10,7) NOT NULL,
        [DestinationAddress] nvarchar(500) NOT NULL,
        [DestinationLatitude] decimal(10,7) NOT NULL,
        [DestinationLongitude] decimal(10,7) NOT NULL,
        [DistanceMeters] int NOT NULL,
        [DistanceKilometers] decimal(10,2) NOT NULL,
        [DurationSeconds] int NULL,
        [CarbonEmission] decimal(18,4) NOT NULL,
        [RoutePolyline] nvarchar(max) NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_TravelLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TravelLogs_ApplicationUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE TABLE [UserFollows] (
        [Id] int NOT NULL IDENTITY,
        [FollowerId] int NOT NULL,
        [FolloweeId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UserFollows] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserFollows_ApplicationUsers_FolloweeId] FOREIGN KEY ([FolloweeId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserFollows_ApplicationUsers_FollowerId] FOREIGN KEY ([FollowerId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE TABLE [UtilityBills] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [YearMonth] nvarchar(7) NOT NULL,
        [BillType] int NOT NULL,
        [BillPeriodStart] datetime2 NOT NULL,
        [BillPeriodEnd] datetime2 NOT NULL,
        [ElectricityUsage] decimal(18,4) NULL,
        [ElectricityCost] decimal(18,2) NOT NULL,
        [WaterUsage] decimal(18,4) NULL,
        [WaterCost] decimal(18,2) NOT NULL,
        [GasUsage] decimal(18,4) NULL,
        [GasCost] decimal(18,2) NOT NULL,
        [ElectricityCarbonEmission] decimal(18,4) NOT NULL,
        [WaterCarbonEmission] decimal(18,4) NOT NULL,
        [GasCarbonEmission] decimal(18,4) NOT NULL,
        [TotalCarbonEmission] decimal(18,4) NOT NULL,
        [InputMethod] int NOT NULL,
        [OcrConfidence] decimal(18,4) NULL,
        [OcrRawText] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UtilityBills] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UtilityBills_ApplicationUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE TABLE [ActivityLogs] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [CarbonReferenceId] int NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [TotalEmission] decimal(18,4) NOT NULL,
        [ImageUrl] nvarchar(1024) NULL,
        [DetectedLabel] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ActivityLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ActivityLogs_ApplicationUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ActivityLogs_CarbonReferences_CarbonReferenceId] FOREIGN KEY ([CarbonReferenceId]) REFERENCES [CarbonReferences] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE TABLE [BarcodeReferences] (
        [Id] int NOT NULL IDENTITY,
        [Barcode] nvarchar(50) NOT NULL,
        [ProductName] nvarchar(200) NOT NULL,
        [CarbonReferenceId] int NULL,
        [Category] nvarchar(max) NULL,
        [Brand] nvarchar(100) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BarcodeReferences] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BarcodeReferences_CarbonReferences_CarbonReferenceId] FOREIGN KEY ([CarbonReferenceId]) REFERENCES [CarbonReferences] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE TABLE [DietTemplateItems] (
        [Id] int NOT NULL IDENTITY,
        [DietTemplateId] int NOT NULL,
        [FoodId] int NOT NULL,
        [Quantity] float NOT NULL,
        [Unit] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_DietTemplateItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DietTemplateItems_DietTemplates_DietTemplateId] FOREIGN KEY ([DietTemplateId]) REFERENCES [DietTemplates] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE TABLE [Comments] (
        [Id] int NOT NULL IDENTITY,
        [PostId] int NOT NULL,
        [UserId] int NOT NULL,
        [Content] nvarchar(2000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Comments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Comments_ApplicationUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Comments_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'ClimatiqActivityId', N'Co2Factor', N'CreatedAt', N'LabelName', N'Region', N'Source', N'Unit', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[CarbonReferences]'))
        SET IDENTITY_INSERT [CarbonReferences] ON;
    EXEC(N'INSERT INTO [CarbonReferences] ([Id], [Category], [ClimatiqActivityId], [Co2Factor], [CreatedAt], [LabelName], [Region], [Source], [Unit], [UpdatedAt])
    VALUES (1, 0, NULL, 27.0, ''2026-01-28T04:08:35.1611382Z'', N''Beef'', NULL, N''Local'', N''kgCO2'', ''2026-01-28T04:08:35.1611388Z''),
    (2, 1, NULL, 0.03, ''2026-01-28T04:08:35.1611399Z'', N''Subway'', NULL, N''Local'', N''kgCO2/km'', ''2026-01-28T04:08:35.1611400Z''),
    (3, 2, NULL, 0.5, ''2026-01-28T04:08:35.1611402Z'', N''Electricity'', NULL, N''Local'', N''kgCO2/kWh'', ''2026-01-28T04:08:35.1611402Z''),
    (4, 2, NULL, 0.35, ''2026-01-28T04:08:35.1611404Z'', N''Water'', NULL, N''Local'', N''kgCO2/m3'', ''2026-01-28T04:08:35.1611405Z''),
    (5, 2, NULL, 2.3, ''2026-01-28T04:08:35.1611407Z'', N''Gas'', NULL, N''Local'', N''kgCO2/unit'', ''2026-01-28T04:08:35.1611407Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'ClimatiqActivityId', N'Co2Factor', N'CreatedAt', N'LabelName', N'Region', N'Source', N'Unit', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[CarbonReferences]'))
        SET IDENTITY_INSERT [CarbonReferences] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConfidenceThreshold', N'MaintenanceMode', N'VisionModel', N'WeeklyDigest') AND [object_id] = OBJECT_ID(N'[SystemSettings]'))
        SET IDENTITY_INSERT [SystemSettings] ON;
    EXEC(N'INSERT INTO [SystemSettings] ([Id], [ConfidenceThreshold], [MaintenanceMode], [VisionModel], [WeeklyDigest])
    VALUES (1, 80, CAST(0 AS bit), N''default'', CAST(1 AS bit))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConfidenceThreshold', N'MaintenanceMode', N'VisionModel', N'WeeklyDigest') AND [object_id] = OBJECT_ID(N'[SystemSettings]'))
        SET IDENTITY_INSERT [SystemSettings] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ActivityLogs_CarbonReferenceId] ON [ActivityLogs] ([CarbonReferenceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ActivityLogs_UserId] ON [ActivityLogs] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AiInsights_UserId] ON [AiInsights] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BarcodeReferences_CarbonReferenceId] ON [BarcodeReferences] ([CarbonReferenceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CarbonReferences_LabelName_Category_Region] ON [CarbonReferences] ([LabelName], [Category], [Region]) WHERE [Region] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Comments_PostId] ON [Comments] ([PostId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Comments_UserId] ON [Comments] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DietTemplateItems_DietTemplateId] ON [DietTemplateItems] ([DietTemplateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Posts_UserId] ON [Posts] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StepRecords_UserId] ON [StepRecords] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TravelLogs_UserId] ON [TravelLogs] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserFollows_FolloweeId] ON [UserFollows] ([FolloweeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserFollows_FollowerId_FolloweeId] ON [UserFollows] ([FollowerId], [FolloweeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UtilityBills_UserId] ON [UtilityBills] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128040835_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260128040835_InitialCreate', N'8.0.24');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128073007_AddTreeStateFields'
)
BEGIN
    ALTER TABLE [ApplicationUsers] ADD [CurrentTreeProgress] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128073007_AddTreeStateFields'
)
BEGIN
    ALTER TABLE [ApplicationUsers] ADD [TreesTotalCount] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128073007_AddTreeStateFields'
)
BEGIN
    EXEC(N'UPDATE [CarbonReferences] SET [CreatedAt] = ''2026-01-28T07:30:07.0896397Z'', [UpdatedAt] = ''2026-01-28T07:30:07.0896402Z''
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128073007_AddTreeStateFields'
)
BEGIN
    EXEC(N'UPDATE [CarbonReferences] SET [CreatedAt] = ''2026-01-28T07:30:07.0896412Z'', [UpdatedAt] = ''2026-01-28T07:30:07.0896412Z''
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128073007_AddTreeStateFields'
)
BEGIN
    EXEC(N'UPDATE [CarbonReferences] SET [CreatedAt] = ''2026-01-28T07:30:07.0896415Z'', [UpdatedAt] = ''2026-01-28T07:30:07.0896415Z''
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128073007_AddTreeStateFields'
)
BEGIN
    EXEC(N'UPDATE [CarbonReferences] SET [CreatedAt] = ''2026-01-28T07:30:07.0896417Z'', [UpdatedAt] = ''2026-01-28T07:30:07.0896418Z''
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128073007_AddTreeStateFields'
)
BEGIN
    EXEC(N'UPDATE [CarbonReferences] SET [CreatedAt] = ''2026-01-28T07:30:07.0896420Z'', [UpdatedAt] = ''2026-01-28T07:30:07.0896421Z''
    WHERE [Id] = 5;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128073007_AddTreeStateFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260128073007_AddTreeStateFields', N'8.0.24');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128083515_AddUserNickname'
)
BEGIN

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ApplicationUsers]') AND name = N'Nickname')
    BEGIN
        ALTER TABLE [ApplicationUsers] ADD [Nickname] nvarchar(100) NULL;
    END

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128083515_AddUserNickname'
)
BEGIN

    UPDATE [ApplicationUsers]
    SET [Nickname] = [Username]
    WHERE [Nickname] IS NULL OR LTRIM(RTRIM([Nickname])) = '';

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128083515_AddUserNickname'
)
BEGIN
    EXEC(N'UPDATE [CarbonReferences] SET [CreatedAt] = ''2026-01-28T08:35:14.8219673Z'', [UpdatedAt] = ''2026-01-28T08:35:14.8219680Z''
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128083515_AddUserNickname'
)
BEGIN
    EXEC(N'UPDATE [CarbonReferences] SET [CreatedAt] = ''2026-01-28T08:35:14.8219690Z'', [UpdatedAt] = ''2026-01-28T08:35:14.8219691Z''
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128083515_AddUserNickname'
)
BEGIN
    EXEC(N'UPDATE [CarbonReferences] SET [CreatedAt] = ''2026-01-28T08:35:14.8219693Z'', [UpdatedAt] = ''2026-01-28T08:35:14.8219693Z''
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128083515_AddUserNickname'
)
BEGIN
    EXEC(N'UPDATE [CarbonReferences] SET [CreatedAt] = ''2026-01-28T08:35:14.8219695Z'', [UpdatedAt] = ''2026-01-28T08:35:14.8219695Z''
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128083515_AddUserNickname'
)
BEGIN
    EXEC(N'UPDATE [CarbonReferences] SET [CreatedAt] = ''2026-01-28T08:35:14.8219696Z'', [UpdatedAt] = ''2026-01-28T08:35:14.8219697Z''
    WHERE [Id] = 5;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128083515_AddUserNickname'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260128083515_AddUserNickname', N'8.0.24');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260202090500_AddStepUsageFieldsToApplicationUser'
)
BEGIN
    ALTER TABLE [ApplicationUsers] ADD [LastStepUsageDate] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260202090500_AddStepUsageFieldsToApplicationUser'
)
BEGIN
    ALTER TABLE [ApplicationUsers] ADD [StepsUsedToday] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260202090500_AddStepUsageFieldsToApplicationUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260202090500_AddStepUsageFieldsToApplicationUser', N'8.0.24');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209082557_AddPointAwardLogsEf'
)
BEGIN
    CREATE TABLE [PointAwardLogs] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Points] int NOT NULL,
        [AwardedAt] datetime2 NOT NULL,
        [Source] nvarchar(32) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PointAwardLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PointAwardLogs_ApplicationUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [ApplicationUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209082557_AddPointAwardLogsEf'
)
BEGIN
    CREATE INDEX [IX_PointAwardLogs_UserId_AwardedAt] ON [PointAwardLogs] ([UserId], [AwardedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209082557_AddPointAwardLogsEf'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260209082557_AddPointAwardLogsEf', N'8.0.24');
END;
GO

COMMIT;
GO

