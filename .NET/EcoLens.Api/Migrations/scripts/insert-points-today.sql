-- ============================================================
-- Add points so they appear in "Today's points" on the leaderboard
-- ============================================================
-- Today's points are summed from PointAwardLogs WHERE AwardedAt
-- is within the current UTC date (today 00:00:00 <= AwardedAt < tomorrow 00:00:00).
--
-- 1) Replace @UserId with your actual user ID (from ApplicationUsers.Id).
-- 2) Replace @Points with the points to add (e.g. 10, 50, 100).
-- 3) Run this script in your database.
-- ============================================================

-- Example: Give user ID 1  some points that count for TODAY (UTC)
DECLARE @UserId INT = 1;        -- Change to your user's Id
DECLARE @Points INT = 50;      -- Points to add
DECLARE @TodayUtc DATE = CAST(GETUTCDATE() AS DATE);

INSERT INTO PointAwardLogs (UserId, Points, AwardedAt, Source, CreatedAt)
VALUES (@UserId, @Points, @TodayUtc, N'Manual', GETUTCDATE());

-- Optional: also increase the user's total points (CurrentPoints)
-- so they appear in "All time" leaderboard and profile.
UPDATE ApplicationUsers
SET CurrentPoints = CurrentPoints + @Points
WHERE Id = @UserId;

-- Verify: list today's point records for the user
-- SELECT * FROM PointAwardLogs WHERE UserId = @UserId AND AwardedAt >= @TodayUtc AND AwardedAt < DATEADD(DAY, 1, @TodayUtc);
