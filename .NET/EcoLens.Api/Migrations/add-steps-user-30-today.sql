-- 为 ID 30 用户新增今日步数 2000000 步
DECLARE @UserId INT = 30;
DECLARE @StepsToAdd INT = 2000000;
DECLARE @Today DATE = CAST(GETUTCDATE() AS DATE);

DECLARE @ExistingRecordId INT;
DECLARE @ExistingStepCount INT;
DECLARE @NewStepCount INT;
DECLARE @CarbonOffset DECIMAL(18,4);

SELECT @ExistingRecordId = Id, @ExistingStepCount = StepCount
FROM StepRecords
WHERE UserId = @UserId AND CAST(RecordDate AS DATE) = @Today;

IF @ExistingRecordId IS NOT NULL
BEGIN
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
    SET @NewStepCount = @StepsToAdd;
    SET @CarbonOffset = @NewStepCount * 0.0001;

    INSERT INTO StepRecords (UserId, StepCount, RecordDate, CarbonOffset, CreatedAt, UpdatedAt)
    VALUES (@UserId, @NewStepCount, @Today, @CarbonOffset, GETUTCDATE(), GETUTCDATE());

    PRINT 'Created new record: StepCount = ' + CAST(@NewStepCount AS VARCHAR) + ', CarbonOffset = ' + CAST(@CarbonOffset AS VARCHAR);
END

SELECT Id, UserId, StepCount, RecordDate, CarbonOffset, CreatedAt, UpdatedAt
FROM StepRecords
WHERE UserId = @UserId AND CAST(RecordDate AS DATE) = @Today;
