-- 迁移脚本：将 AvatarUrl 字段从 nvarchar(1024) 改为 nvarchar(max)
-- 用于支持 Base64 编码的头像图片存储

USE [ecolens];
GO

-- 检查字段当前类型
IF EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'ApplicationUsers' 
    AND COLUMN_NAME = 'AvatarUrl'
    AND DATA_TYPE = 'nvarchar'
    AND CHARACTER_MAXIMUM_LENGTH = 1024
)
BEGIN
    -- 修改字段类型为 nvarchar(max)
    ALTER TABLE [ApplicationUsers]
    ALTER COLUMN [AvatarUrl] NVARCHAR(MAX) NULL;
    
    PRINT 'AvatarUrl 字段已成功修改为 nvarchar(max)';
END
ELSE IF EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'ApplicationUsers' 
    AND COLUMN_NAME = 'AvatarUrl'
    AND DATA_TYPE = 'nvarchar'
    AND CHARACTER_MAXIMUM_LENGTH = -1
)
BEGIN
    PRINT 'AvatarUrl 字段已经是 nvarchar(max)，无需修改';
END
ELSE
BEGIN
    PRINT '警告：未找到 AvatarUrl 字段或字段类型不匹配';
END
GO

-- 验证修改结果
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ApplicationUsers' 
AND COLUMN_NAME = 'AvatarUrl';
GO
