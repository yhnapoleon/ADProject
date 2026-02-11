# Stage only web frontend source changes (no .env / .env.local)
Set-Location "d:\ADProject-main\ADProject-main"
$log = "d:\ADProject-main\ADProject-main\_git_result.txt"
"=== git status before ===" | Out-File $log -Encoding utf8
git status --short | Out-File $log -Append -Encoding utf8

# Unstage env and apk if staged
git restore --staged "web/.env" 2>$null
git restore --staged "web/.env.local" 2>$null
git restore --staged "EcoLens-debug.apk" 2>$null

# Stage all web frontend source
git add "web/src/"

"=== status after add ===" | Out-File $log -Append -Encoding utf8
git status --short | Out-File $log -Append -Encoding utf8

$staged = git diff --staged --name-only
if ($staged) {
  git commit -m "web: login/profile/request fixes, profile load optimization, API timing and timeout handling"
  "=== commit done ===" | Out-File $log -Append -Encoding utf8
  git log -1 --oneline | Out-File $log -Append -Encoding utf8
} else {
  "=== no web src changes to commit ===" | Out-File $log -Append -Encoding utf8
}

# Push current branch to origin first (so xuwenzhe has the commit)
git push origin xuwenzhe 2>&1 | Out-File $log -Append -Encoding utf8

# Update main: checkout main, pull, merge xuwenzhe, push main
git fetch origin 2>&1 | Out-File $log -Append -Encoding utf8
git checkout main 2>&1 | Out-File $log -Append -Encoding utf8
git pull origin main 2>&1 | Out-File $log -Append -Encoding utf8
git merge xuwenzhe -m "Merge branch xuwenzhe: web frontend updates" 2>&1 | Out-File $log -Append -Encoding utf8
git push origin main 2>&1 | Out-File $log -Append -Encoding utf8

"=== final status ===" | Out-File $log -Append -Encoding utf8
git status -sb | Out-File $log -Append -Encoding utf8
git branch -v | Out-File $log -Append -Encoding utf8
