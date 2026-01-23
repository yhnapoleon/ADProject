import React from 'react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Area, AreaChart } from 'recharts';
import './AdminDashboard.css';

const AdminDashboard: React.FC = () => {
  const weeklyData = [
    { week: 'Week 1', value: 800 },
    { week: 'Week 2', value: 1200 },
    { week: 'Week 3', value: 1100 },
    { week: 'Week 4', value: 1500 },
    { week: 'Week 5', value: 1900 },
  ];

  const emissionFactors = [
    { id: 'EF-001', category: 'Food', itemName: 'Beef Steak (100g)', factor: 2.5, lastUpdated: '2024-05-20', status: 'Active' },
    { id: 'EF-002', category: 'Transport', itemName: 'Bus Ride (1km)', factor: 0.1, lastUpdated: '2024-05-19', status: 'Active' },
    { id: 'EF-003', category: 'Energy', itemName: 'Electricity (1kWh)', factor: 0.5, lastUpdated: '2024-05-18', status: 'Review' },
  ];

  return (
    <div className="dashboard">
      <h1 className="page-title">Community Macro-Monitoring</h1>
      
      <div className="stats-cards">
        <div className="stat-card">
          <h3>Total Eco-Users</h3>
          <div className="stat-value">12,450</div>
          <div className="stat-change positive">+5% this week</div>
        </div>
        <div className="stat-card">
          <h3>Carbon Reduced</h3>
          <div className="stat-value">8,320 kg</div>
          <div className="stat-change positive">+12% this week</div>
        </div>
        <div className="stat-card">
          <h3>Active Factors</h3>
          <div className="stat-value">1,240</div>
          <div className="stat-change">Database Updated</div>
        </div>
      </div>

      <div className="charts-container">
        <div className="chart-card">
          <h3>Regional Reduction Trends (Singapore)</h3>
          <div className="singapore-map-container">
            <svg
              viewBox="0 0 500 300"
              className="singapore-map"
              preserveAspectRatio="xMidYMid meet"
            >
              <defs>
                {/* Clip path to restrict heatmap points within Singapore outline */}
                <clipPath id="singaporeClip">
                  <path
                    d="M 80 50 L 120 45 L 180 55 L 240 50 L 300 55 L 360 60 L 420 70 L 450 90 L 460 130 L 455 170 L 445 210 L 430 240 L 410 260 L 380 270 L 350 275 L 320 270 L 290 265 L 260 260 L 230 255 L 200 250 L 170 245 L 140 240 L 110 235 L 85 225 L 70 200 L 65 170 L 60 140 L 58 110 L 70 80 L 80 50 Z"
                  />
                </clipPath>
              </defs>
              
              {/* Singapore Main Island - based on the provided outline */}
              <path
                d="M 80 50 L 120 45 L 180 55 L 240 50 L 300 55 L 360 60 L 420 70 L 450 90 L 460 130 L 455 170 L 445 210 L 430 240 L 410 260 L 380 270 L 350 275 L 320 270 L 290 265 L 260 260 L 230 255 L 200 250 L 170 245 L 140 240 L 110 235 L 85 225 L 70 200 L 65 170 L 60 140 L 58 110 L 70 80 L 80 50 Z"
                fill="#e0e0e0"
                stroke="#999"
                strokeWidth="1.5"
              />
              
              {/* Heatmap Data Points - clipped to map outline */}
              <g clipPath="url(#singaporeClip)">
                {Array.from({ length: 60 }).map((_, i) => {
                  // Generate points within the Singapore outline bounds
                  const baseX = 100 + Math.random() * 350;
                  const baseY = 60 + Math.random() * 220;
                  const intensity = Math.random();
                  const size = 4 + intensity * 8;
                  const opacity = 0.3 + intensity * 0.7;
                  
                  // Color gradient: yellow-green to dark green
                  let color;
                  if (intensity > 0.7) {
                    color = '#2E7D32'; // Dark green
                  } else if (intensity > 0.4) {
                    color = '#4CAF50'; // Medium green
                  } else {
                    color = '#81C784'; // Light green
                  }
                  
                  return (
                    <circle
                      key={i}
                      cx={baseX}
                      cy={baseY}
                      r={size}
                      fill={color}
                      opacity={opacity}
                      className="heat-point"
                    />
                  );
                })}
              </g>
            </svg>
          </div>
        </div>
        <div className="chart-card">
          <h3>Weekly Platform Impact (kg CO2)</h3>
          <ResponsiveContainer width="100%" height={300}>
            <AreaChart data={weeklyData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#e0e0e0" />
              <XAxis dataKey="week" stroke="#666" />
              <YAxis stroke="#666" domain={[0, 2000]} />
              <Tooltip />
              <Area type="monotone" dataKey="value" stroke="#4CAF50" fill="#4CAF50" fillOpacity={0.3} />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      </div>

      <div className="table-card">
        <h3>Emission Factor Database (Recent Updates)</h3>
        <table className="data-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>Category</th>
              <th>Item Name</th>
              <th>Emission Factor (kg CO2/unit)</th>
              <th>Last Updated</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {emissionFactors.map((item) => (
              <tr key={item.id}>
                <td>{item.id}</td>
                <td>{item.category}</td>
                <td>{item.itemName}</td>
                <td>{item.factor}</td>
                <td>{item.lastUpdated}</td>
                <td>
                  <span className={`status-badge ${item.status.toLowerCase()}`}>
                    {item.status}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default AdminDashboard;
