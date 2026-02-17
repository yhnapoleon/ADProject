# Git 提交并推送脚本
# 用于提交排行榜积分功能的相关更改

cd "d:\ADProject-main\ADProject-main"

# 添加修改的文件
git add .NET/EcoLens.Api/Controllers/LeaderboardController.cs
git add .NET/EcoLens.Api/Controllers/StepController.cs
git add .NET/EcoLens.Api/Data/ApplicationDbContext.cs
git add .NET/EcoLens.Api/Migrations/ApplicationDbContextModelSnapshot.cs
git add .NET/EcoLens.Api/Migrations/20260209100000_AddPointAwardLogs.cs
git add .NET/EcoLens.Api/Models/PointAwardLog.cs
git add .NET/EcoLens.Api/Services/IPointService.cs
git add .NET/EcoLens.Api/Services/PointService.cs
git add web/src/pages/Leaderboard.tsx

# 查看暂存状态
Write-Host "`n已暂存的文件：" -ForegroundColor Green
git status --short

# 提交更改
$commitMessage = @"
feat: 添加今日/本月积分统计功能

- 新增 PointAwardLog 模型用于记录积分发放历史
- 修复排行榜月度统计使用日历月而非滚动30天
- 修复三个排行榜接口返回相同积分的问题
- 实现按周期（今日/本周/本月）统计积分
- 更新排行榜排序逻辑：今日/周/月按对应周期积分排序，全部按总积分排序
"@

Write-Host "`n提交信息：" -ForegroundColor Yellow
Write-Host $commitMessage

$confirm = Read-Host "`n确认提交？(Y/N)"
if ($confirm -eq "Y" -or $confirm -eq "y") {
    git commit -m $commitMessage
    
    Write-Host "`n推送到远程 main 分支..." -ForegroundColor Cyan
    git push origin main
    
    Write-Host "`n完成！" -ForegroundColor Green
} else {
    Write-Host "`n已取消提交" -ForegroundColor Yellow
}
