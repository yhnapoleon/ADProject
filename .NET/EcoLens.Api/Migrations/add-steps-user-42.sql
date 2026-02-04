-- 为 ID 42 的用户添加 100000 步数
-- 如果今天已有记录，则增加步数；如果没有，则创建新记录

DECLARE @UserId INT = 42;
DECLARE @StepsToAdd INT = 100000;
DECLARE @Today DATE = CAST(GETUTCDATE() AS DATE);

-- 检查今天是否已有记录
DECLARE @ExistingRecordId INT;
DECLARE @ExistingStepCount INT;
DECLARE @NewStepCount INT;
DECLARE @CarbonOffset DECIMAL(18,4);

SELECT @ExistingRecordId = Id, @ExistingStepCount = StepCount
FROM StepRecords
WHERE UserId = @UserId AND CAST(RecordDate AS DATE) = @Today;

IF @ExistingRecordId IS NOT NULL
BEGIN
    -- 更新现有记录：增加步数
    SET @NewStepCount = @ExistingStepCount + @StepsToAdd;
    SET @CarbonOffset = @NewStepCount * 0.0001;
    
    UPDATE StepRecords
    SET StepCount = @NewStepCount,
        CarbonOffset = @CarbonOffset,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @ExistingRecordId;
    
    PRINT 'Updated existing record: StepCount = ' + CAST(@NewStepCount AS VARCHAR) + ', CarbonOffset = ' + CAST(@CarbonOffset AS VARCHAR);
END
ELSE
BEGIN
    -- 插入新记录
    SET @NewStepCount = @StepsToAdd;
    SET @CarbonOffset = @NewStepCount * 0.0001;
    
    INSERT INTO StepRecords (UserId, StepCount, RecordDate, CarbonOffset, CreatedAt, UpdatedAt)
    VALUES (@UserId, @NewStepCount, @Today, @CarbonOffset, GETUTCDATE(), GETUTCDATE());
    
    PRINT 'Created new record: StepCount = ' + CAST(@NewStepCount AS VARCHAR) + ', CarbonOffset = ' + CAST(@CarbonOffset AS VARCHAR);
END

-- 验证结果
SELECT 
    Id,
    UserId,
    StepCount,
    RecordDate,
    CarbonOffset,
    CreatedAt,
    UpdatedAt
FROM StepRecords
WHERE UserId = @UserId AND CAST(RecordDate AS DATE) = @Today;
