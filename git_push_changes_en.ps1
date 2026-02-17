# Git commit and push script
# For committing leaderboard points feature changes (all comments translated to English)

cd "d:\ADProject-main\ADProject-main"

# Add modified files
git add .NET/EcoLens.Api/Controllers/LeaderboardController.cs
git add .NET/EcoLens.Api/Controllers/StepController.cs
git add .NET/EcoLens.Api/Data/ApplicationDbContext.cs
git add .NET/EcoLens.Api/Migrations/ApplicationDbContextModelSnapshot.cs
git add .NET/EcoLens.Api/Migrations/20260209100000_AddPointAwardLogs.cs
git add .NET/EcoLens.Api/Models/PointAwardLog.cs
git add .NET/EcoLens.Api/Services/IPointService.cs
git add .NET/EcoLens.Api/Services/PointService.cs
git add web/src/pages/Leaderboard.tsx

# Show staged status
Write-Host "`nStaged files:" -ForegroundColor Green
git status --short

# Commit changes
$commitMessage = @"
feat: Add today/monthly points statistics feature

- Add PointAwardLog model to track point award history
- Fix monthly leaderboard to use calendar month instead of rolling 30 days
- Fix three leaderboard endpoints returning same points
- Implement period-based (today/week/month) points aggregation
- Update leaderboard sorting: today/week/month by period points, all by total points
- Translate all Chinese comments to English
"@

Write-Host "`nCommit message:" -ForegroundColor Yellow
Write-Host $commitMessage

$confirm = Read-Host "`nConfirm commit? (Y/N)"
if ($confirm -eq "Y" -or $confirm -eq "y") {
    git commit -m $commitMessage
    
    Write-Host "`nPushing to remote main branch..." -ForegroundColor Cyan
    git push origin main
    
    Write-Host "`nDone!" -ForegroundColor Green
} else {
    Write-Host "`nCommit cancelled" -ForegroundColor Yellow
}
