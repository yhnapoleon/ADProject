FROM python:3.13-slim

ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

WORKDIR /app

# 安装运行时图像处理所需的系统依赖
RUN apt-get update && apt-get install -y --no-install-recommends \
  libgl1 \
  libglib2.0-0 \
  libgomp1 \
  && rm -rf /var/lib/apt/lists/*

# 仅复制依赖文件并安装（充分利用 Docker 层缓存）
COPY requirements.txt /app/requirements.txt
RUN python -m pip install --upgrade pip && \
  pip install --no-cache-dir -r /app/requirements.txt

# 再复制应用代码
COPY . /app

# 暴露容器端口
EXPOSE 80

# 启动 FastAPI（Uvicorn）
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "80"]

