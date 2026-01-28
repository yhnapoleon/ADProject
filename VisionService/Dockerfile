FROM python:3.9-slim

ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

WORKDIR /app

# 安装运行时图像处理所需的系统依赖
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgl1 \
    libglib2.0-0 \
    libgomp1 \
  && rm -rf /var/lib/apt/lists/*

# 安装 Python 依赖
COPY requirements.txt /app/requirements.txt
RUN pip install --no-cache-dir -r /app/requirements.txt

# 复制应用代码与关键数据文件
COPY main.py /app/main.py
COPY asian_food_labels.txt /app/asian_food_labels.txt

# 暴露容器端口
EXPOSE 80

# 启动 FastAPI（Uvicorn）
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "80"]

