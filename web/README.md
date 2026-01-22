# Web 前端项目指南

一个基于 **Vite + React + TypeScript + Axios** 的现代化前端项目脚手架。

## 📁 项目结构

```
web/
├── public/                 # 静态资源文件夹
├── src/                    # 源代码文件夹
│   ├── api/               # API 接口层（业务接口实现）
│   ├── assets/            # 静态资源（图片、字体等）
│   ├── components/        # 可复用的 React 组件
│   ├── hooks/             # 自定义 React Hooks
│   ├── pages/             # 页面级组件
│   ├── types/             # TypeScript 类型定义
│   │   └── request.d.ts   # 请求相关的类型定义
│   ├── utils/             # 工具函数
│   │   └── request.ts     # HTTP 请求客户端（Axios）
│   ├── App.tsx            # 根组件
│   └── main.tsx           # 应用入口文件
├── index.html             # HTML 模板
├── package.json           # 项目依赖配置
├── tsconfig.json          # TypeScript 编译配置
├── tsconfig.node.json     # Node 环境 TypeScript 配置
├── vite.config.ts         # Vite 构建配置
└── vite-env.d.ts         # Vite 类型声明文件
```

## 🚀 快速开始

### 安装依赖
```bash
npm install
```

### 开发模式
```bash
npm run dev
```
开发服务器将在 `http://localhost:3000` 启动，支持热模块替换 (HMR)

### 生产构建
```bash
npm run build
```
输出的文件将保存在 `dist` 文件夹中

### 预览生产构建
```bash
npm run preview
```

## 📚 核心文件说明

### src/utils/request.ts - HTTP 客户端
Axios 的统一配置文件，处理所有 HTTP 请求。

**主要功能：**
- 请求拦截器：可以在这里添加认证令牌、请求头等
- 响应拦截器：统一处理响应数据和错误

**使用示例：**
```typescript
import request from '@/utils/request';

// 发送 GET 请求
request.get('/users/1');

// 发送 POST 请求
request.post('/users', { name: 'John' });
```

### src/types/request.d.ts - 请求类型定义
定义 API 响应的统一格式。

```typescript
interface ApiResponse<T = any> {
  code: number;      // 响应码
  message: string;   // 响应消息
  data: T;          // 实际数据
}
```

## 📝 开发规范

### 目录职责

| 目录 | 职责 | 示例 |
|------|------|------|
| `api/` | API 接口定义 | `userApi.ts`, `productApi.ts` |
| `components/` | 可复用组件 | `Button.tsx`, `Card.tsx` |
| `hooks/` | 自定义 Hooks | `useAuth.ts`, `useFetch.ts` |
| `pages/` | 页面组件 | `HomePage.tsx`, `LoginPage.tsx` |
| `assets/` | 静态资源 | 图片、SVG、字体文件 |
| `utils/` | 工具函数 | `request.ts`, `common.ts` |
| `types/` | 类型定义 | `request.d.ts`, `user.d.ts` |

### 代码样式

1. **使用 TypeScript**：所有代码文件使用 `.tsx` 或 `.ts` 扩展名
2. **函数式组件**：优先使用函数式组件 + Hooks
3. **类型安全**：为所有函数和变量添加类型注解
4. **命名规范**：
   - 组件：PascalCase（如 `UserCard.tsx`）
   - 函数/变量：camelCase（如 `getUserInfo`）
   - 常量：UPPER_SNAKE_CASE（如 `API_BASE_URL`）

### API 调用最佳实践

**❌ 不推荐：直接在组件中调用 API**
```typescript
function UserComponent() {
  useEffect(() => {
    request.get('/users/1').then(res => {
      // 处理响应
    });
  }, []);
}
```

**✅ 推荐：在 api 文件夹中创建接口，然后在组件中使用**

创建 `src/api/userApi.ts`：
```typescript
import request from '@/utils/request';

export const fetchUserInfo = (userId: string) => {
  return request.get(`/users/${userId}`);
};
```

在组件中使用：
```typescript
import { fetchUserInfo } from '@/api/userApi';

function UserComponent() {
  useEffect(() => {
    fetchUserInfo('1').then(res => {
      // 处理响应
    });
  }, []);
}
```

## 🔌 环境变量配置

在项目根目录创建 `.env` 文件配置环境变量：

```env
# .env
VITE_API_URL=http://localhost:5000/api

# .env.production
VITE_API_URL=https://api.example.com
```

在代码中使用：
```typescript
const apiUrl = import.meta.env.VITE_API_URL;
```

## 📦 项目依赖

| 包 | 版本 | 说明 |
|----|------|------|
| react | ^18.2.0 | UI 库 |
| react-dom | ^18.2.0 | React DOM 适配器 |
| axios | ^1.6.0 | HTTP 客户端 |
| vite | ^4.4.0 | 构建工具 |
| typescript | ^5.0.2 | TypeScript 编译器 |

## 🛠 添加新功能

### 添加 API 接口

1. 在 `src/api` 文件夹中创建文件：
```typescript
// src/api/productApi.ts
import request from '@/utils/request';
import type { ApiResponse } from '@/types/request';

export interface Product {
  id: string;
  name: string;
  price: number;
}

export const getProducts = () => {
  return request.get<ApiResponse<Product[]>>('/products');
};

export const getProductById = (id: string) => {
  return request.get<ApiResponse<Product>>(`/products/${id}`);
};
```

2. 在组件中使用：
```typescript
import { getProducts } from '@/api/productApi';

function ProductList() {
  const [products, setProducts] = useState([]);
  
  useEffect(() => {
    getProducts().then(res => {
      setProducts(res.data.data);
    });
  }, []);
  
  return (
    // JSX 代码
  );
}
```

### 添加自定义 Hook

在 `src/hooks` 文件夹中创建：
```typescript
// src/hooks/useFetch.ts
import { useState, useEffect } from 'react';

export const useFetch = (url: string) => {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    // 获取数据的逻辑
  }, [url]);

  return { data, loading, error };
};
```

### 添加可复用组件

在 `src/components` 文件夹中创建：
```typescript
// src/components/UserCard.tsx
import React from 'react';

interface UserCardProps {
  name: string;
  email: string;
  avatar?: string;
}

export const UserCard: React.FC<UserCardProps> = ({ name, email, avatar }) => {
  return (
    <div className="user-card">
      {avatar && <img src={avatar} alt={name} />}
      <h3>{name}</h3>
      <p>{email}</p>
    </div>
  );
};
```

## 🐛 常见问题

### Q: 如何添加 CSS 样式？
**A:** Vite 原生支持 CSS、SCSS、Less 等。直接在组件中引入即可：
```typescript
import './UserCard.css';
// 或
import styles from './UserCard.module.css';
```

### Q: 如何处理认证令牌？
**A:** 在 `src/utils/request.ts` 的请求拦截器中添加：
```typescript
service.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});
```

### Q: 如何与后端 API 对接？
**A:** 修改 `.env` 文件中的 `VITE_API_URL` 为后端 API 地址，然后创建对应的 API 接口文件即可。

## 📖 相关文档

- [React 官方文档](https://react.dev)
- [Vite 官方文档](https://vitejs.dev)
- [TypeScript 官方文档](https://www.typescriptlang.org)
- [Axios 官方文档](https://axios-http.com)

## 🤝 协作指南

1. 遵循项目结构，在合适的目录创建文件
2. 所有代码使用 TypeScript，添加类型注解
3. 提交前确保代码可以正常构建和运行
4. 复杂的逻辑单独提取为 Hook 或工具函数
5. 为公共组件和函数添加必要的文档注释

