import { Row, Col, Card, Button, Progress } from 'antd';
import { useNavigate } from 'react-router-dom';
import { mockLeaderboardData } from '../mock/data';
import './Dashboard.module.css';
import mainEatIcon from '../assets/icons/main_eat.svg';
import mainTravelIcon from '../assets/icons/main_travel.svg';
import mainWaterIcon from '../assets/icons/main_water.svg';

const Dashboard = () => {
  const navigate = useNavigate();
  const numberFormatter = new Intl.NumberFormat('en-US');

  // Mock data
  const thisMonthEmissions = 45.6;
  const targetEmissions = 100.0;
  const progressPercent = (thisMonthEmissions / targetEmissions) * 100;
  const foodEmissions = 18.5;
  const travelEmissions = 20.3;
  const utilitiesEmissions = 6.8;

  const todayLeaderboard = [...mockLeaderboardData]
    .sort((a, b) => (b.pointsToday || 0) - (a.pointsToday || 0))
    .slice(0, 5);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* This Month's Emissions Card */}
      <Card style={{ background: 'linear-gradient(135deg, #674fa3 0%, #7d5fb8 100%)', border: 'none', borderRadius: '12px', color: 'white', boxShadow: '0 4px 12px rgba(103, 79, 163, 0.15)' }}>
        <div style={{ padding: '32px' }}>
          <Row gutter={24} align="middle">
            <Col span={12}>
              <div style={{ marginBottom: '12px' }}>
                <div style={{ fontSize: '14px', opacity: '0.9', marginBottom: '8px' }}>This Month's Emissions</div>
                <div style={{ fontSize: '32px', fontWeight: '700', marginBottom: '12px' }}>{thisMonthEmissions.toFixed(2)} kg</div>
              </div>
            </Col>

            <Col span={12}>
              <div style={{ marginBottom: '12px', textAlign: 'right' }}>
                <div style={{ fontSize: '14px', opacity: '0.9', marginBottom: '8px' }}>Target</div>
                <div style={{ fontSize: '32px', fontWeight: '700', marginBottom: '12px' }}>{targetEmissions.toFixed(2)} kg</div>
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

      {/* Leaderboard */}
      <Row gutter={24} style={{ marginTop: '24px' }}>
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
              {todayLeaderboard.map((user, index) => (
                <div
                  key={user.username}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    padding: '12px 0',
                    borderBottom: index < todayLeaderboard.length - 1 ? '1px solid #f0f0f0' : 'none',
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
                    <span>{user.nickname}</span>
                  </div>
                  <span style={{ fontWeight: 700, color: '#674fa3' }}>
                    {numberFormatter.format(user.pointsToday || 0)} pts
                  </span>
                </div>
              ))}
            </div>
          </Card>
        </Col>
      </Row>
    </div>
  );
};

export default Dashboard;
