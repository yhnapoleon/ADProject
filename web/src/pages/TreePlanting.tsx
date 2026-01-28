import { useState, useEffect, useRef } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { Button, Progress, message } from 'antd';
import { ArrowLeftOutlined } from '@ant-design/icons';
import Lottie, { LottieRefCurrentProps } from 'lottie-react';
import backgroundDay from '../assets/icons/background_day.json';
import plant1 from '../assets/icons/plant1.json';
import plant2 from '../assets/icons/plant2.json';
import plant3 from '../assets/icons/plant3.json';
import plant4 from '../assets/icons/plant4.json';
import plant5 from '../assets/icons/plant5.json';
import plant6 from '../assets/icons/plant6.json';
import celebration from '../assets/icons/celebration.json';

const TreePlanting = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const lottiePlantRef = useRef<LottieRefCurrentProps>(null);
  const lottieCelebrationRef = useRef<LottieRefCurrentProps>(null);
  const floatTipTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  // 从路由state获取步数，如果没有则使用Dashboard的默认值9277
  const initialSteps = (location.state as { todaySteps?: number })?.todaySteps ?? 9277;

  // 根据步数计算初始成长值（从localStorage获取已转换的成长值，如果没有则根据步数计算）
  const getInitialGrowth = (): number => {
    const savedGrowth = localStorage.getItem('treeGrowth');
    if (savedGrowth) {
      return parseInt(savedGrowth, 10);
    }
    // 如果没有保存的成长值，根据步数计算初始成长值（每150步=1%成长）
    const calculatedGrowth = Math.floor(initialSteps / 150);
    return Math.min(calculatedGrowth, 100); // 最多100%
  };

  // 状态定义（完全复刻Android端）
  const [todaySteps, setTodaySteps] = useState<number>(initialSteps);
  const [currentTreeGrowth, setCurrentTreeGrowth] = useState<number>(getInitialGrowth()); // 0-100
  const [totalPlantedTrees, setTotalPlantedTrees] = useState<number>(
    parseInt(localStorage.getItem('totalPlantedTrees') || '5', 10)
  );
  const [isCelebrating, setIsCelebrating] = useState<boolean>(false);
  const [floatTipMessage, setFloatTipMessage] = useState<string>('');
  const [showFloatTip, setShowFloatTip] = useState<boolean>(false);

  // 根据currentTreeGrowth计算stage（1-6）
  const calculateStage = (growth: number): number => {
    if (growth <= 0) return 1;
    if (growth < 17) return 1;
    if (growth < 34) return 2;
    if (growth < 51) return 3;
    if (growth < 68) return 4;
    if (growth < 85) return 5;
    return 6; // 85-100
  };

  // 根据stage获取对应的动画
  const getPlantAnimation = (stage: number) => {
    switch (stage) {
      case 1: return plant1;
      case 2: return plant2;
      case 3: return plant3;
      case 4: return plant4;
      case 5: return plant5;
      case 6: return plant6;
      default: return plant1;
    }
  };

  // 更新UI（刷新树木动画和进度条）
  const refreshUI = () => {
    const stage = calculateStage(currentTreeGrowth);
    const animation = getPlantAnimation(stage);
    
    // 如果动画需要切换，更新Lottie
    if (lottiePlantRef.current) {
      // 注意：lottie-react 不支持直接切换animationData，需要通过key强制重新渲染
      // 这里我们使用stage作为key来触发重新渲染
    }
  };

  // 显示浮动提示
  const showAtTreeTop = (message: string) => {
    setFloatTipMessage(message);
    setShowFloatTip(true);

    // 清除之前的定时器
    if (floatTipTimeoutRef.current) {
      clearTimeout(floatTipTimeoutRef.current);
    }

    // 如果是庆祝语，显示更久
    const displayDuration = isCelebrating ? 2800 : 2000;

    floatTipTimeoutRef.current = setTimeout(() => {
      setShowFloatTip(false);
    }, displayDuration);
  };

  // 处理步数转换（核心业务逻辑）
  const handleStepConversion = () => {
    if (isCelebrating) return;

    if (todaySteps > 0) {
      const growthGain = Math.floor(todaySteps / 150);
      // 计算当前进度 + 增量的总和
      const totalPotential = currentTreeGrowth + growthGain;
      setTodaySteps(0);

      // 树木互动动画（缩放效果）- 通过状态触发CSS动画
      // 这个效果会在UI更新时自动触发

      if (totalPotential >= 100) {
        // 核心：计算种成了几棵树，以及剩下多少进度给下一棵
        const treesPlantedThisTime = Math.floor(totalPotential / 100);
        const leftoverGrowth = totalPotential % 100;

        setTotalPlantedTrees((prev) => {
          const newTotal = prev + treesPlantedThisTime;
          localStorage.setItem('totalPlantedTrees', newTotal.toString());
          return newTotal;
        });

        // 先显示当前这棵树为成树状态（100%），同时显示庆祝动画
        setCurrentTreeGrowth(100);
        setIsCelebrating(true);
        
        // 传入剩余进度，开始庆祝流程
        startCelebration(leftoverGrowth);
      } else {
        setCurrentTreeGrowth(totalPotential);
        localStorage.setItem('treeGrowth', totalPotential.toString());
        showAtTreeTop(`Growth +${growthGain}%`);
      }
    } else {
      showAtTreeTop('No steps to convert!');
    }
  };

  // 庆祝阶段
  const startCelebration = (leftover: number) => {
    // isCelebrating已经在调用前设置为true，这里只需要显示提示
    showAtTreeTop('Congratulations! New tree planted! 🎉');

    // 延迟3秒：展现成树和礼花
    setTimeout(() => {
      resetToNewTree(leftover);
    }, 3000);
  };

  // 重置阶段：清空进度，更新UI回到幼苗状态
  const resetToNewTree = (leftover: number) => {
    setCurrentTreeGrowth(leftover); // 新树的起始进度
    localStorage.setItem('treeGrowth', leftover.toString());
    setIsCelebrating(false);

    if (leftover > 0) {
      showAtTreeTop(`New tree starts with ${leftover}%!`);
    } else {
      showAtTreeTop("Let's grow a new one!");
    }
  };

  // 当currentTreeGrowth变化时，刷新UI
  useEffect(() => {
    refreshUI();
  }, [currentTreeGrowth]);

  // 清理定时器
  useEffect(() => {
    return () => {
      if (floatTipTimeoutRef.current) {
        clearTimeout(floatTipTimeoutRef.current);
      }
    };
  }, []);

  // 移除父容器的padding和margin，实现全屏沉浸式效果
  useEffect(() => {
    // 查找父容器（MainLayout的Content包装div）
    const parentElement = document.querySelector('.ant-layout-content > div');
    if (parentElement) {
      const originalStyle = {
        margin: (parentElement as HTMLElement).style.margin,
        padding: (parentElement as HTMLElement).style.padding,
        background: (parentElement as HTMLElement).style.background,
        borderRadius: (parentElement as HTMLElement).style.borderRadius,
      };
      
      // 移除父容器的样式
      (parentElement as HTMLElement).style.margin = '0';
      (parentElement as HTMLElement).style.padding = '0';
      (parentElement as HTMLElement).style.background = 'transparent';
      (parentElement as HTMLElement).style.borderRadius = '0';

      // 清理函数：恢复原始样式
      return () => {
        (parentElement as HTMLElement).style.margin = originalStyle.margin;
        (parentElement as HTMLElement).style.padding = originalStyle.padding;
        (parentElement as HTMLElement).style.background = originalStyle.background;
        (parentElement as HTMLElement).style.borderRadius = originalStyle.borderRadius;
      };
    }
  }, []);

  // 移除Content的margin
  useEffect(() => {
    const contentElement = document.querySelector('.ant-layout-content');
    if (contentElement) {
      const originalMargin = (contentElement as HTMLElement).style.margin;
      (contentElement as HTMLElement).style.margin = '0';
      
      return () => {
        (contentElement as HTMLElement).style.margin = originalMargin;
      };
    }
  }, []);

  const currentStage = calculateStage(currentTreeGrowth);
  const currentPlantAnimation = getPlantAnimation(currentStage);

  return (
    <div
      style={{
        position: 'relative',
        width: '100%',
        height: '100vh',
        margin: 0,
        padding: 0,
        overflow: 'hidden',
        background: '#F4F4F6',
        boxSizing: 'border-box',
      }}
    >
      {/* Layer 1: 背景层 */}
      <div
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          width: '100%',
          height: '100%',
          zIndex: 0,
        }}
      >
        <Lottie
          animationData={backgroundDay}
          loop={true}
          autoplay={true}
          style={{
            width: '100%',
            height: '100%',
            position: 'absolute',
            top: 0,
            left: 0,
            objectFit: 'cover',
          }}
        />
      </div>

      {/* Layer 2: 内容层 */}
      <div
        style={{
          position: 'relative',
          zIndex: 1,
          width: '100%',
          height: '100%',
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        {/* 顶部工具栏 */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '16px',
            background: '#FFFFFF',
            boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
            zIndex: 10,
          }}
        >
          <Button
            type="text"
            icon={<ArrowLeftOutlined />}
            onClick={() => navigate(-1)}
            style={{
              color: '#674fa3',
              fontSize: '16px',
            }}
          />
          <h1
            style={{
              margin: 0,
              fontSize: '20px',
              fontWeight: '600',
              color: '#333333',
              flex: 1,
              textAlign: 'center',
            }}
          >
            My Carbon Forest
          </h1>
          <div style={{ width: '32px' }} /> {/* 占位 */}
        </div>

        {/* 树木计数器（右上角） */}
        <div
          style={{
            position: 'absolute',
            top: '72px',
            right: '16px',
            background: 'rgba(255, 255, 255, 0.95)',
            padding: '8px 16px',
            borderRadius: '20px',
            boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            zIndex: 5,
          }}
        >
          <span style={{ fontSize: '16px', fontWeight: '700', color: '#674fa3' }}>
            Trees: {totalPlantedTrees}
          </span>
        </div>

        {/* 浮动提示 */}
        <div
          style={{
            position: 'absolute',
            bottom: '400px',
            left: '50%',
            transform: 'translateX(-50%)',
            background: 'rgba(0, 0, 0, 0.8)',
            color: '#FFFFFF',
            padding: '8px 16px',
            borderRadius: '20px',
            fontSize: '14px',
            fontWeight: '700',
            zIndex: 15,
            whiteSpace: 'nowrap',
            opacity: showFloatTip ? 1 : 0,
            transition: 'opacity 0.5s',
            pointerEvents: 'none',
            visibility: showFloatTip ? 'visible' : 'hidden',
          }}
        >
          {floatTipMessage}
        </div>

        {/* 核心动画区 - 树木动画 */}
        <div
          style={{
            flex: 1,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            position: 'relative',
          }}
        >
          <div
            key={currentStage} // 使用key强制重新渲染以切换动画
            style={{
              width: '320px',
              height: '320px',
              transition: 'transform 0.15s ease',
            }}
          >
            <Lottie
              lottieRef={lottiePlantRef}
              animationData={currentPlantAnimation}
              loop={false}
              autoplay={true}
              style={{
                width: '100%',
                height: '100%',
              }}
            />
          </div>
        </div>

        {/* 进度条 */}
        <div
          style={{
            position: 'absolute',
            bottom: '200px',
            left: 0,
            right: 0,
            padding: '0 16px',
            zIndex: 5,
          }}
        >
          <Progress
            percent={currentTreeGrowth}
            strokeColor="#4CAF50"
            showInfo={false}
            strokeWidth={24}
            style={{
              background: 'rgba(255, 255, 255, 0.4)',
            }}
          />
        </div>

        {/* 底部控制卡片 */}
        <div
          style={{
            position: 'absolute',
            bottom: 0,
            left: 0,
            right: 0,
            background: 'rgba(204, 255, 255, 0.95)',
            padding: '16px',
            borderRadius: '16px 16px 0 0',
            zIndex: 5,
          }}
        >
          <div
            style={{
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              gap: '12px',
            }}
          >
            {/* 今日步数 */}
            <div
              style={{
                fontSize: '16px',
                fontWeight: '700',
                color: '#674fa3',
              }}
            >
              Today's Steps: {todaySteps.toLocaleString()}
            </div>

            {/* 转换按钮 */}
            <Button
              type="primary"
              size="large"
              onClick={handleStepConversion}
              disabled={isCelebrating || todaySteps === 0}
              style={{
                borderRadius: '20px',
                background: '#674fa3',
                borderColor: '#674fa3',
                fontWeight: '600',
                padding: '8px 24px',
                height: 'auto',
              }}
            >
              Convert to Growth
            </Button>

            {/* 碳影响文本 */}
            <div
              style={{
                fontSize: '13px',
                color: '#333333',
                textAlign: 'center',
                marginTop: '4px',
              }}
            >
              Your carbon reduction from walking is equivalent to planting {totalPlantedTrees} trees for the Earth.
            </div>
          </div>
        </div>

        {/* 庆祝动画（全屏覆盖）- 当树达到100%时和树一起显示 */}
        {isCelebrating && (
          <div
            style={{
              position: 'absolute',
              top: 0,
              left: 0,
              width: '100%',
              height: '100%',
              zIndex: 20,
              pointerEvents: 'none',
            }}
          >
            <Lottie
              lottieRef={lottieCelebrationRef}
              animationData={celebration}
              loop={false}
              autoplay={true}
              style={{
                width: '100%',
                height: '100%',
              }}
            />
          </div>
        )}
      </div>
    </div>
  );
};

export default TreePlanting;
