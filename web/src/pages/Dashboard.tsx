import { useState, useEffect } from 'react';
import { Row, Col, Card, Button, Progress, Spin } from 'antd';
import { useNavigate } from 'react-router-dom';
import Lottie from 'lottie-react';
import walkingAnimation from '../assets/icons/walking.json';
import './Dashboard.module.css';
import mainEatIcon from '../assets/icons/main_eat.svg';
import mainTravelIcon from '../assets/icons/main_travel.svg';
import mainWaterIcon from '../assets/icons/main_water.svg';
import request from '../utils/request';

interface MainPageStats {
  total: number;
  food: number;
  transport: number;
  utility: number;
}

const Dashboard = () => {
  const navigate = useNavigate();
  const numberFormatter = new Intl.NumberFormat('en-US');

  const [statsLoading, setStatsLoading] = useState(true);
  const [stats, setStats] = useState<MainPageStats>({ total: 0, food: 0, transport: 0, utility: 0 });
  const [stepCount, setStepCount] = useState<number>(0);
  const [stepsLoading, setStepsLoading] = useState(true);
  const [todayRanking, setTodayRanking] = useState<{ nickname?: string; username?: string; pointsTotal?: number; pointsToday?: number }[]>([]);
  const [todayRankingLoading, setTodayRankingLoading] = useState(true);

  useEffect(() => {
    const fetchMainPageStats = async () => {
      setStatsLoading(true);
      try {
        const res = await request.get<MainPageStats>('/api/mainpage');
        setStats({
          total: Number(res?.total ?? 0),
          food: Number(res?.food ?? 0),
          transport: Number(res?.transport ?? 0),
          utility: Number(res?.utility ?? 0),
        });
      } catch (e: any) {
        console.error('[Dashboard] Failed to fetch mainpage stats:', e?.response?.status, e?.response?.data ?? e?.message);
        setStats({ total: 0, food: 0, transport: 0, utility: 0 });
      } finally {
        setStatsLoading(false);
      }
    };
    fetchMainPageStats();
  }, []);

  useEffect(() => {
    const fetchStepSync = async () => {
      setStepsLoading(true);
      const fetchOnce = () => request.get<{ todaySteps?: number; availableSteps?: number }>('/api/getTree');
      try {
        let res: { todaySteps?: number; availableSteps?: number } | undefined;
        try {
          res = await fetchOnce();
        } catch (e: any) {
          if (e?.code === 'ECONNABORTED' || e?.message?.includes('timeout')) {
            await new Promise((r) => setTimeout(r, 3000));
            res = await fetchOnce();
          } else throw e;
        }
        setStepCount(Number(res?.todaySteps ?? 0));
      } catch (_e) {
        setStepCount(0);
      } finally {
        setStepsLoading(false);
      }
    };
    fetchStepSync();
  }, []);
  useEffect(() => {
    const fetchTodayRanking = async () => {
      setTodayRankingLoading(true);
      try {
        const res: any = await request.get('/api/Leaderboard', {
          params: { period: 'today', limit: 5 },
        });
        const list = Array.isArray(res) ? res : res?.items ?? res ?? [];
        setTodayRanking(Array.isArray(list) ? list.slice(0, 5) : []);
      } catch (_e) {
        setTodayRanking([]);
      } finally {
        setTodayRankingLoading(false);
      }
    };
    fetchTodayRanking();
  }, []);

  const targetEmissions = 200.0;
  const thisMonthEmissions = stats.total;
  const progressPercent = targetEmissions > 0 ? Math.min(100, (thisMonthEmissions / targetEmissions) * 100) : 0;
  const foodEmissions = stats.food;
  const travelEmissions = stats.transport;
  const utilitiesEmissions = stats.utility;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* This Month's Emissions Card */}
      <Card style={{ background: 'linear-gradient(135deg, #674fa3 0%, #7d5fb8 100%)', border: 'none', borderRadius: '12px', color: 'white', boxShadow: '0 4px 12px rgba(103, 79, 163, 0.15)' }}>
        <Spin spinning={statsLoading} tip="Loading emissions...">
          <div style={{ padding: '32px' }}>
            <Row gutter={24} align="middle">
              <Col span={12}>
                <div style={{ marginBottom: '12px' }}>
                  <div style={{ fontSize: '35px', opacity: '0.9', marginBottom: '8px' }}>This Month's Emissions</div>
                  <div style={{ fontSize: '32px', fontWeight: '700', marginBottom: '12px' }}>{thisMonthEmissions.toFixed(2)} kg</div>
                </div>
              </Col>

              <Col span={12}>
                <div style={{ marginBottom: '12px', textAlign: 'right' }}>
                  <div style={{ fontSize: '20px', opacity: '0.9', marginBottom: '8px' }}>Recommended Monthly Carbon Footprint</div>
                  <div style={{ fontSize: '20px', fontWeight: '700', marginBottom: '12px' }}>{targetEmissions.toFixed(2)} kg</div>
                </div>
              </Col>
            </Row>

            <div style={{ marginTop: '16px', marginBottom: '24px' }}>
              <Progress
                percent={progressPercent}
                strokeColor="#feda00"
                showInfo={false}
              />
            </div>

            <Row gutter={24} style={{ marginTop: '24px' }}>
              <Col span={8}>
                <div style={{ background: 'rgba(255, 255, 255, 0.2)', borderRadius: '12px', padding: '16px', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '8px' }}>
                  <img src={mainEatIcon} alt="Food" style={{ width: '32px', height: '32px' }} />
                  <div style={{ fontSize: '18px', fontWeight: '700' }}>Food</div>
                  <div style={{ fontSize: '18px', fontWeight: '700' }}>{foodEmissions.toFixed(2)} kg</div>
                </div>
              </Col>
              <Col span={8}>
                <div style={{ background: 'rgba(255, 255, 255, 0.2)', borderRadius: '12px', padding: '16px', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '8px' }}>
                  <img src={mainTravelIcon} alt="Travel" style={{ width: '32px', height: '32px' }} />
                  <div style={{ fontSize: '18px', fontWeight: '700' }}>Travel</div>
                  <div style={{ fontSize: '18px', fontWeight: '700' }}>{travelEmissions.toFixed(2)} kg</div>
                </div>
              </Col>
              <Col span={8}>
                <div style={{ background: 'rgba(255, 255, 255, 0.2)', borderRadius: '12px', padding: '16px', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '8px' }}>
                  <img src={mainWaterIcon} alt="Utility" style={{ width: '32px', height: '32px' }} />
                  <div style={{ fontSize: '18px', fontWeight: '700' }}>Utility</div>
                  <div style={{ fontSize: '18px', fontWeight: '700' }}>{utilitiesEmissions.toFixed(2)} kg</div>
                </div>
              </Col>
            </Row>
          </div>
        </Spin>
      </Card>

      {/* Category Cards */}
      <Row gutter={24} style={{ marginTop: '24px' }}>
        <Col xs={24} sm={24} md={8}>
          <Card style={{ borderRadius: '12px', border: '1px solid #f0f0f0', transition: 'all 0.3s ease', height: '100%' }}>
            <div style={{ textAlign: 'center' }}>
              <img src={mainEatIcon} alt="Food" style={{ width: '48px', height: '48px', marginBottom: '12px', filter: 'brightness(0) saturate(100%) invert(35%) sepia(78%) saturate(1352%) hue-rotate(230deg) brightness(95%) contrast(90%)' }} />
              <div style={{ fontSize: '18px', fontWeight: '600', marginBottom: '16px', color: '#333' }}>Food</div>
              <Button
                type="default"
                block
                style={{
                  borderColor: '#674fa3',
                  color: '#674fa3',
                  fontWeight: '600',
                }}
                onClick={() => navigate('/log-meal')}
              >
                Add Food
              </Button>
            </div>
          </Card>
        </Col>

        <Col xs={24} sm={24} md={8}>
          <Card style={{ borderRadius: '12px', border: '1px solid #f0f0f0', transition: 'all 0.3s ease', height: '100%' }}>
            <div style={{ textAlign: 'center' }}>
              <img src={mainTravelIcon} alt="Travel" style={{ width: '48px', height: '48px', marginBottom: '12px', filter: 'brightness(0) saturate(100%) invert(35%) sepia(78%) saturate(1352%) hue-rotate(230deg) brightness(95%) contrast(90%)' }} />
              <div style={{ fontSize: '18px', fontWeight: '600', marginBottom: '16px', color: '#333' }}>Travel</div>
              <Button
                type="default"
                block
                style={{
                  borderColor: '#674fa3',
                  color: '#674fa3',
                  fontWeight: '600',
                }}
                onClick={() => navigate('/log-travel')}
              >
                Add Travel
              </Button>
            </div>
          </Card>
        </Col>

        <Col xs={24} sm={24} md={8}>
          <Card style={{ borderRadius: '12px', border: '1px solid #f0f0f0', transition: 'all 0.3s ease', height: '100%' }}>
            <div style={{ textAlign: 'center' }}>
              <img src={mainWaterIcon} alt="Utility" style={{ width: '48px', height: '48px', marginBottom: '12px', filter: 'brightness(0) saturate(100%) invert(35%) sepia(78%) saturate(1352%) hue-rotate(230deg) brightness(95%) contrast(90%)' }} />
              <div style={{ fontSize: '18px', fontWeight: '600', marginBottom: '16px', color: '#333' }}>Utility</div>
              <Button
                type="default"
                block
                style={{
                  borderColor: '#674fa3',
                  color: '#674fa3',
                  fontWeight: '600',
                }}
                onClick={() => navigate('/log-utility')}
              >
                Add Utility
              </Button>
            </div>
          </Card>
        </Col>
      </Row>

      {/* steps */}
      <Row gutter={24} style={{ marginTop: '2px' }}>
        <Col span={24}>
          <Card 
            style={{ 
              borderRadius: '12px', 
              border: '1px solid #f0f0f0',
              cursor: 'pointer',
              transition: 'all 0.3s ease',
            }}
            onClick={() => navigate('/tree-planting', { state: { steps: stepCount, todaySteps: stepCount } })}
            onMouseEnter={(e) => {
              e.currentTarget.style.boxShadow = '0 4px 12px rgba(103, 79, 163, 0.15)';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.boxShadow = 'none';
            }}
          >
            <div style={{ 
              display: 'flex', 
              alignItems: 'center', 
              padding: '5px 5px', 
              overflow: 'hidden'    
            }}>
              <div style={{ flex: '0 0 auto' }}>
                <Lottie 
                  animationData={walkingAnimation} 
                  loop 
                  autoplay 
                  style={{ width: 300, height: 300 }}
                />
              </div>
              <div style={{
                margin: '0 auto', 
                display: 'flex', 
                alignItems: 'baseline', 
                gap: '20px',
                fontFamily: 'sans-serif',
                whiteSpace: 'nowrap',
              }}>
                <div style={{ fontSize: '20px', color: '#999' }}>Steps Today</div>
                {stepsLoading ? (
                  <div style={{ fontSize: '20px', color: '#999' }}>Loading...</div>
                ) : (
                  <div style={{ fontSize: '35px', fontWeight: 'bold', color: '#674fa3' }}>{numberFormatter.format(stepCount)}</div>
                )}
                <div style={{ fontSize: '20px', color: '#999' }}>Steps</div>
              </div>

              
              <div style={{ 
                flex: '0 0 150px', 
                visibility: 'hidden'
              }}></div>

            </div>
          </Card>
        </Col>
      </Row>

      {/* Leaderboard */}
      <Row gutter={24} style={{ marginTop: '2px' }}>
        <Col span={24}>
          <Card>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <div style={{ fontSize: '16px', fontWeight: '600' }}>🏆 Today's Ranking</div>
              </div>
              <Button type="link" onClick={() => navigate('/leaderboard', { state: { defaultTab: 'today' } })}>
                View all
              </Button>
            </div>
            <div style={{ maxHeight: '300px', overflowY: 'auto' }}>
              {todayRankingLoading ? (
                <div style={{ padding: 24, textAlign: 'center', color: '#999' }}>Loading...</div>
              ) : todayRanking.length === 0 ? (
                <div style={{ padding: 24, textAlign: 'center', color: '#999' }}>No ranking data yet</div>
              ) : (
                todayRanking.map((user, index) => (
                  <div
                    key={user.username ?? index}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'space-between',
                      padding: '12px 0',
                      borderBottom: index < todayRanking.length - 1 ? '1px solid #f0f0f0' : 'none',
                    }}
                  >
                    <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                      <span
                        style={{
                          fontSize: '14px',
                          fontWeight: '600',
                          color: '#674fa3',
                          minWidth: '24px',
                        }}
                      >
                        {index + 1}
                      </span>
                      <span>{user.nickname ?? user.username ?? '-'}</span>
                    </div>
                    <span style={{ fontWeight: 700, color: '#674fa3' }}>
                      {numberFormatter.format(user.pointsToday ?? user.pointsTotal ?? 0)} pts
                    </span>
                  </div>
                ))
              )}
            </div>
          </Card>
        </Col>
      </Row>
    </div>
  );
};

export default Dashboard;
