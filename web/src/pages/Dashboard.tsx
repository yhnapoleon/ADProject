import { Row, Col, Card, Button, Progress } from 'antd';
import { useNavigate } from 'react-router-dom';
import { getTopLeaderboard } from '../mock/data';
import './Dashboard.module.css';

const Dashboard = () => {
  const navigate = useNavigate();

  // Mock data
  const todayEmissions = 0.0;
  const todaySteps = 9277;
  const goalProgress = 0;
  const foodEmissions = 0.0;
  const transportEmissions = 0.0;
  const utilitiesEmissions = 0.0;

  const leaderboard = getTopLeaderboard(5);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Today's Emissions Card */}
      <Card style={{ background: 'linear-gradient(135deg, #674fa3 0%, #7d5fb8 100%)', border: 'none', borderRadius: '12px', color: 'white', boxShadow: '0 4px 12px rgba(103, 79, 163, 0.15)' }}>
        <div style={{ padding: '32px' }}>
          <Row gutter={24} align="middle">
            <Col span={12}>
              <div style={{ marginBottom: '12px' }}>
                <div style={{ fontSize: '14px', opacity: '0.9', marginBottom: '8px' }}>Today's Emissions</div>
                <div style={{ fontSize: '32px', fontWeight: '700', marginBottom: '12px' }}>{todayEmissions.toFixed(2)} kg</div>
                <Progress
                  percent={0}
                  strokeColor="rgba(255,255,255,0.8)"
                  style={{ marginTop: '12px' }}
                />
                <div style={{ fontSize: '12px', opacity: '0.85', marginTop: '8px' }}>Target progress: {todayEmissions.toFixed(2)} kg</div>
              </div>
            </Col>

            <Col span={12}>
              <div style={{ marginBottom: '12px' }}>
                <div style={{ fontSize: '14px', opacity: '0.9', marginBottom: '8px' }}>Steps Today</div>
                <div style={{ fontSize: '32px', fontWeight: '700', marginBottom: '12px' }}>{todaySteps} steps</div>
                <Progress
                  percent={goalProgress}
                  strokeColor="rgba(255,255,255,0.8)"
                  style={{ marginTop: '12px' }}
                />
                <div style={{ fontSize: '12px', opacity: '0.85', marginTop: '8px' }}>Goal progress</div>
              </div>
            </Col>
          </Row>

          <Row gutter={24} style={{ marginTop: '24px' }}>
            <Col span={8}>
              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', width: '90px', height: '90px', borderRadius: '50%', border: '2px solid rgba(255,255,255,0.3)', margin: '0 auto' }}>
                <div style={{ fontSize: '20px', fontWeight: '700' }}>{foodEmissions.toFixed(2)}</div>
                <div style={{ fontSize: '12px', opacity: '0.8' }}>kg</div>
              </div>
              <div style={{ marginTop: '8px', fontSize: '14px', color: 'white', textAlign: 'center' }}>Food</div>
            </Col>
            <Col span={8}>
              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', width: '90px', height: '90px', borderRadius: '50%', border: '2px solid rgba(255,255,255,0.3)', margin: '0 auto' }}>
                <div style={{ fontSize: '20px', fontWeight: '700' }}>{transportEmissions.toFixed(2)}</div>
                <div style={{ fontSize: '12px', opacity: '0.8' }}>kg</div>
              </div>
              <div style={{ marginTop: '8px', fontSize: '14px', color: 'white', textAlign: 'center' }}>Transport</div>
            </Col>
            <Col span={8}>
              <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', width: '90px', height: '90px', borderRadius: '50%', border: '2px solid rgba(255,255,255,0.3)', margin: '0 auto' }}>
                <div style={{ fontSize: '20px', fontWeight: '700' }}>{utilitiesEmissions.toFixed(2)}</div>
                <div style={{ fontSize: '12px', opacity: '0.8' }}>kg</div>
              </div>
              <div style={{ marginTop: '8px', fontSize: '14px', color: 'white', textAlign: 'center' }}>Utilities</div>
            </Col>
          </Row>
        </div>
      </Card>

      {/* Category Cards */}
      <Row gutter={24} style={{ marginTop: '24px' }}>
        <Col xs={24} sm={24} md={8}>
          <Card style={{ borderRadius: '12px', border: '1px solid #f0f0f0', transition: 'all 0.3s ease', height: '100%' }}>
            <div style={{ textAlign: 'center' }}>
              <div style={{ fontSize: '24px', marginBottom: '12px' }}>🍴</div>
              <div style={{ fontSize: '18px', fontWeight: '600', marginBottom: '12px', color: '#333' }}>Food</div>
              <ul style={{ listStyle: 'none', padding: 0, margin: 0, fontSize: '14px', color: '#666', textAlign: 'left', display: 'inline-block' }}>
                <li style={{ marginBottom: '6px' }}><span style={{ color: '#674fa3', fontWeight: 'bold', marginRight: '6px' }}>•</span>Log meals</li>
                <li style={{ marginBottom: '6px' }}><span style={{ color: '#674fa3', fontWeight: 'bold', marginRight: '6px' }}>•</span>kgCO2e</li>
              </ul>
              <Button
                type="default"
                block
                style={{
                  borderColor: '#674fa3',
                  color: '#674fa3',
                  marginTop: '16px',
                  fontWeight: '600',
                }}
                onClick={() => navigate('/records')}
              >
                Log Food
              </Button>
            </div>
          </Card>
        </Col>

        <Col xs={24} sm={24} md={8}>
          <Card style={{ borderRadius: '12px', border: '1px solid #f0f0f0', transition: 'all 0.3s ease', height: '100%' }}>
            <div style={{ textAlign: 'center' }}>
              <div style={{ fontSize: '24px', marginBottom: '12px' }}>🚗</div>
              <div style={{ fontSize: '18px', fontWeight: '600', marginBottom: '12px', color: '#333' }}>Transport</div>
              <ul style={{ listStyle: 'none', padding: 0, margin: 0, fontSize: '14px', color: '#666', textAlign: 'left', display: 'inline-block' }}>
                <li style={{ marginBottom: '6px' }}><span style={{ color: '#674fa3', fontWeight: 'bold', marginRight: '6px' }}>•</span>Mode</li>
                <li style={{ marginBottom: '6px' }}><span style={{ color: '#674fa3', fontWeight: 'bold', marginRight: '6px' }}>•</span>Distance / Trips</li>
              </ul>
              <Button
                type="default"
                block
                style={{
                  borderColor: '#674fa3',
                  color: '#674fa3',
                  marginTop: '16px',
                  fontWeight: '600',
                }}
                onClick={() => navigate('/records')}
              >
                Log Trip
              </Button>
            </div>
          </Card>
        </Col>

        <Col xs={24} sm={24} md={8}>
          <Card style={{ borderRadius: '12px', border: '1px solid #f0f0f0', transition: 'all 0.3s ease', height: '100%' }}>
            <div style={{ textAlign: 'center' }}>
              <div style={{ fontSize: '24px', marginBottom: '12px' }}>⚡</div>
              <div style={{ fontSize: '18px', fontWeight: '600', marginBottom: '12px', color: '#333' }}>Utilities</div>
              <ul style={{ listStyle: 'none', padding: 0, margin: 0, fontSize: '14px', color: '#666', textAlign: 'left', display: 'inline-block' }}>
                <li style={{ marginBottom: '6px' }}><span style={{ color: '#674fa3', fontWeight: 'bold', marginRight: '6px' }}>•</span>Water / Power / Gas</li>
                <li style={{ marginBottom: '6px' }}><span style={{ color: '#674fa3', fontWeight: 'bold', marginRight: '6px' }}>•</span>Usage</li>
              </ul>
              <Button
                type="default"
                block
                style={{
                  borderColor: '#674fa3',
                  color: '#674fa3',
                  marginTop: '16px',
                  fontWeight: '600',
                }}
                onClick={() => navigate('/records')}
              >
                Log Usage
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
              <div style={{ fontSize: '16px', fontWeight: '600' }}>🏆 Leaderboard</div>
              <Button type="link" onClick={() => navigate('/leaderboard')}>
                View all
              </Button>
            </div>
            <div style={{ maxHeight: '300px', overflowY: 'auto' }}>
              {leaderboard.map((user, index) => (
                <div
                  key={`${user.rank}-${user.username}`}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    padding: '12px 0',
                    borderBottom: index < leaderboard.length - 1 ? '1px solid #f0f0f0' : 'none',
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
                      #{user.rank}
                    </span>
                    <span>{user.nickname}</span>
                  </div>
                  <span style={{ fontWeight: '600' }}>{user.emissions.toFixed(2)} kg</span>
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
