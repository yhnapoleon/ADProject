from __future__ import annotations

import os
import io
from typing import Dict, Any, Optional

import torch
from PIL import Image
from transformers import AutoImageProcessor, AutoModelForImageClassification
from transformers import CLIPProcessor, CLIPModel
from fastapi import FastAPI, UploadFile, File, HTTPException
from pydantic import BaseModel
from contextlib import asynccontextmanager
import uvicorn
from prometheus_fastapi_instrumentator import Instrumentator


class FoodClassifier:
    """
    基于双模型（西餐/中餐）融合策略的食物图像分类器。

    - 西餐模型：prithivMLmods/Food-101-93M（SigLIP/ViT，覆盖 Food-101）
    - 亚洲模型：openai/clip-vit-base-patch32（CLIP 文本-图像对齐，使用候选标签文本匹配）

    预测流程（predict）：
    1. 使用各自模型的 AutoImageProcessor 对输入图像独立预处理
    2. 分别进行前向推理，得到 logits 并做 softmax 得到概率分布
    3. 提取两个模型各自的 Top-1 类别与置信度
    4. 比较两个 Top-1 置信度，较大者对应的模型结果作为最终输出

    返回字典包含：
    - label: 最终类别名称（字符串）
    - score: 最终置信度（float）
    - source_model: 采用结果的来源模型名称（字符串）
    """

    def __init__(self, asian_labels_path: str = "asian_food_labels.txt", asia_score_weight: float = 1.1) -> None:
        # 模型名称常量
        self.western_model_name: str = "prithivMLmods/Food-101-93M"
        self.asian_model_name: str = "openai/clip-vit-base-patch32"

        # 亚洲模型比较权重（仅用于决策比较，不改变返回的原始分数）
        try:
            asia_score_weight = float(asia_score_weight)
        except Exception:
            asia_score_weight = 1.0
        self.asia_score_weight: float = max(0.1, asia_score_weight)

        # 设备自动检测（优先 CUDA，其次 MPS，最后 CPU）
        if torch.cuda.is_available():
            self.device = torch.device("cuda")
        elif hasattr(torch.backends, "mps") and torch.backends.mps.is_available():  # macOS Metal
            self.device = torch.device("mps")
        else:
            self.device = torch.device("cpu")

        # 分别加载两套处理器与分类模型
        # 说明：各模型的预处理规范可能不同，因此分别实例化各自的处理器/模型
        self.western_processor = AutoImageProcessor.from_pretrained(
            self.western_model_name)
        self.western_model = AutoModelForImageClassification.from_pretrained(
            self.western_model_name).to(self.device)
        self.western_model.eval()

        # 亚洲模型改为 CLIP：以文本候选标签进行匹配
        self.asian_processor = CLIPProcessor.from_pretrained(
            self.asian_model_name)
        self.asian_model = CLIPModel.from_pretrained(
            self.asian_model_name).to(self.device)
        self.asian_model.eval()

        # 读取 id2label（西餐）与候选标签列表（亚洲）
        self.western_id2label = self.western_model.config.id2label

        # 加载亚洲食物候选标签（英文文本），文件可由用户后续扩展
        # 支持两种查找：相对 main.py 目录与当前工作目录，便于不同启动位置
        candidate_paths = []
        if os.path.isabs(asian_labels_path):
            candidate_paths.append(asian_labels_path)
        else:
            candidate_paths.append(os.path.join(
                os.path.dirname(__file__), asian_labels_path))
            candidate_paths.append(os.path.join(
                os.getcwd(), asian_labels_path))

        label_file = next(
            (p for p in candidate_paths if os.path.exists(p)), None)
        if label_file is None:
            raise FileNotFoundError(
                f"未找到亚洲食物标签文件：{asian_labels_path}。"
                f"已尝试路径：{candidate_paths}。请创建该文件（UTF-8），每行一个标签。"
            )

        with open(label_file, "r", encoding="utf-8") as f:
            self.asian_labels = [line.strip()
                                 for line in f.readlines() if line.strip()]

        # 若文件存在但为空，则警告并使用内置默认标签以避免服务启动失败
        if not self.asian_labels:
            print(f"[FoodClassifier] 警告：标签文件存在但为空：{label_file}。将使用内置默认标签集合。")
            self.asian_labels = [
                "Hainanese Chicken Rice",
                "Fried Rice",
                "Dumplings",
                "Ramen",
                "Sushi",
                "Pho",
                "Pad Thai",
                "Mapo Tofu",
                "Sweet and Sour Pork",
                "Bibimbap",
            ]

    @torch.inference_mode()
    def predict(self, image_path: str) -> Dict[str, Any]:
        """
        对输入图像进行分类，并基于双模型 Top-1 置信度进行结果合并。

        参数：
                image_path: 图像文件的路径
        返回：
                包含 label、score、source_model 的字典
        """
        if not isinstance(image_path, str) or not image_path:
            raise ValueError("image_path 必须是非空字符串。")
        if not os.path.exists(image_path):
            raise FileNotFoundError(f"未找到图像文件：{image_path}")

        # 打开图像（PIL），保证为 RGB 模式
        image = Image.open(image_path).convert("RGB")

        # --- 西餐模型推理 ---
        w_inputs = self.western_processor(images=image, return_tensors="pt")
        w_inputs = {k: v.to(self.device) for k, v in w_inputs.items()}
        w_outputs = self.western_model(**w_inputs)
        w_logits = w_outputs.logits  # [1, num_classes]
        w_probs = torch.softmax(w_logits, dim=-1)  # [1, num_classes]
        w_score, w_idx = torch.max(w_probs, dim=-1)
        w_top_score = float(w_score.item())
        w_top_idx = int(w_idx.item())
        w_top_label = self.western_id2label.get(w_top_idx, str(w_top_idx))

        # --- 亚洲模型（CLIP）推理：图像-文本相似度 ---
        a_inputs = self.asian_processor(
            text=self.asian_labels, images=image, return_tensors="pt", padding=True)
        a_inputs = {k: v.to(self.device) for k, v in a_inputs.items()}
        a_outputs = self.asian_model(**a_inputs)
        # logits_per_image 形状：[batch=1, num_texts]，越大越相似
        a_logits = a_outputs.logits_per_image  # [1, N]
        a_probs = torch.softmax(a_logits, dim=-1)  # 在候选标签上做 softmax
        a_score, a_idx = torch.max(a_probs, dim=-1)  # [1]
        a_top_score = float(a_score.item())
        a_top_idx = int(a_idx.item())
        a_top_label = self.asian_labels[a_top_idx]

        # 核心决策：比较两个模型的 Top-1 置信度
        # CLIP 的 softmax 值往往更“保守”，对亚洲侧应用权重仅用于比较
        asia_weighted = a_top_score * self.asia_score_weight
        # 打印原始与加权分值，便于调试（不影响接口）
        print(
            f"[FoodClassifier] Compare -> west: {w_top_score:.6f}, asia_raw: {a_top_score:.6f}, asia_weighted({self.asia_score_weight:.2f}x): {asia_weighted:.6f}")

        if w_top_score > asia_weighted:
            return {
                "label": w_top_label,
                "score": w_top_score,
                "source_model": self.western_model_name,
            }
        else:
            return {
                "label": a_top_label,
                "score": a_top_score,
                "source_model": self.asian_model_name,
            }


class PredictionResponse(BaseModel):
    """
    预测响应数据结构，便于 .NET 客户端反序列化。
    """
    label: str
    confidence: float
    source_model: str


# 使用 FastAPI lifespan，在启动时加载全局分类器实例
CLASSIFIER: Optional[FoodClassifier] = None


@asynccontextmanager
async def lifespan(app: FastAPI):
    """
    应用生命周期：
    - startup：初始化并缓存 FoodClassifier（加载模型到显存/内存）
    - shutdown：释放资源（如有需要）
    """
    global CLASSIFIER
    try:
        CLASSIFIER = FoodClassifier()
        app.state.classifier = CLASSIFIER
        yield
    finally:
        # 可选清理逻辑：将全局引用置空，便于进程退出时回收
        # 注意：显存/内存会在进程结束时由操作系统回收
        app.state.classifier = None
        CLASSIFIER = None


app = FastAPI(lifespan=lifespan)

# Prometheus metrics: instrument and expose /metrics
Instrumentator().instrument(app).expose(app)

@app.post("/predict/image", response_model=PredictionResponse)
async def predict_image(file: UploadFile = File(...)):
    """
    上传图片进行食物分类。
    - 接收 multipart/form-data 的图片文件
    - 将字节流转换为 PIL Image 做基本校验
    - 调用已加载的全局 FoodClassifier 执行预测
    """
    # 校验全局模型是否已就绪
    global CLASSIFIER
    if CLASSIFIER is None:
        raise HTTPException(status_code=500, detail="服务未就绪：模型尚未加载完成。")

    # 读取文件字节
    try:
        content = await file.read()
    except Exception as e:
        raise HTTPException(status_code=400, detail=f"无法读取上传文件：{e}")

    if not content:
        raise HTTPException(status_code=400, detail="上传文件为空。")

    # 将字节转换为 PIL Image，做基础有效性校验
    try:
        image = Image.open(io.BytesIO(content))
        # 若不是 RGB，此处不强制转换；分类器内部会统一为 RGB
        _ = image.size  # 触发加载，避免延迟错误
    except Exception:
        raise HTTPException(status_code=400, detail="无效的图片文件，无法解析。")

    # 由于 FoodClassifier 的 predict 接口基于文件路径，这里将图片保存为临时文件再推理
    # 这样既满足“将字节流转换为 PIL Image”的要求，又复用既有推理接口
    tmp_path = None
    try:
        import tempfile

        with tempfile.NamedTemporaryFile(delete=False, suffix=".png") as tmp:
            tmp_path = tmp.name
            # 统一保存为 PNG，避免有损压缩带来的信息丢失
            image.convert("RGB").save(tmp_path, format="PNG")

        result = CLASSIFIER.predict(tmp_path)

        # 将 score 字段映射为 confidence（基础响应）
        response = PredictionResponse(
            label=result.get("label", ""),
            confidence=float(result.get("score", 0.0)),
            source_model=result.get("source_model", ""),
        )

        return response
    except HTTPException:
        # 透传已构造的 HTTP 异常
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"预测失败：{e}")
    finally:
        if tmp_path and os.path.exists(tmp_path):
            try:
                os.remove(tmp_path)
            except Exception:
                # 清理失败不应影响主流程
                pass


if __name__ == "__main__":
    # 启动 FastAPI 服务
    # 监听 0.0.0.0:8000，便于容器或远程访问
    uvicorn.run("main:app", host="0.0.0.0", port=8000,
                log_level="info", reload=False)
