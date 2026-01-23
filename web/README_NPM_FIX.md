# 解决 PowerShell npm 执行策略问题

## 问题
PowerShell 阻止运行 npm.ps1 脚本，报错：`无法加载文件 npm.ps1, 因为在此系统上禁止运行脚本`

## 解决方案

### 方案 1：使用 npm.cmd（推荐，最简单）
在 PowerShell 中，直接使用 `npm.cmd` 代替 `npm`：

```powershell
npm.cmd install
npm.cmd run dev
```

### 方案 2：临时绕过执行策略（仅当前会话）
在 PowerShell 中运行：

```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
npm install
```

### 方案 3：使用 CMD 而不是 PowerShell
打开 CMD（命令提示符）而不是 PowerShell，然后运行：

```cmd
cd web
npm install
npm run dev
```

### 方案 4：修改执行策略（需要管理员权限）
以管理员身份运行 PowerShell，然后执行：

```powershell
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
```

然后重新打开 PowerShell 窗口。

## 推荐操作步骤

1. 在 PowerShell 中进入 web 目录：
   ```powershell
   cd web
   ```

2. 使用 npm.cmd 安装依赖：
   ```powershell
   npm.cmd install
   ```

3. 启动开发服务器：
   ```powershell
   npm.cmd run dev
   ```

或者直接使用 CMD（命令提示符）来运行 npm 命令。
