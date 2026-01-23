import React from 'react';
import { PieChart, Pie, Cell, ResponsiveContainer, Legend, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip } from 'recharts';
import './AdminCommunityAnalytics.css';

const AdminCommunityAnalytics: React.FC = () => {
  const pieData = [
    { name: 'Energy', value: 15.0 },
    { name: 'Transport', value: 40.0 },
    { name: 'Food', value: 35.0 },
    { name: 'Goods & Services', value: 10.0 },
  ];

  const barData = [
    { month: 'Jan', dau: 550, mau: 1150 },
    { month: 'Feb', dau: 750, mau: 1450 },
    { month: 'Mar', dau: 1200, mau: 2000 },
    { month: 'Apr', dau: 1400, mau: 2750 },
    { month: 'May', dau: 1600, mau: 3450 },
  ];

  const COLORS = ['#A5D6A7', '#66BB6A', '#4CAF50', '#388E3C'];

  return (
    <div className="community-analytics">
      <h1 className="page-title">Community Analytics Deep Dive</h1>
      
      <div className="charts-container">
        <div className="chart-card">
          <h3>Total Reduction Share by Category</h3>
          <ResponsiveContainer width="100%" height={300}>
            <PieChart>
              <Pie
                data={pieData}
                cx="50%"
                cy="50%"
                labelLine={false}
                label={({ name, percent }) => `${name}: ${(percent * 100).toFixed(1)}%`}
                outerRadius={100}
                fill="#8884d8"
                dataKey="value"
              >
                {pieData.map((entry, index) => (
                  <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                ))}
              </Pie>
              <Tooltip />
            </PieChart>
          </ResponsiveContainer>
        </div>

        <div className="chart-card">
          <h3>User Engagement Growth (2024)</h3>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={barData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#e0e0e0" />
              <XAxis dataKey="month" stroke="#666" />
              <YAxis stroke="#666" domain={[0, 3500]} />
              <Tooltip />
              <Legend />
              <Bar dataKey="dau" fill="#A5D6A7" name="DAU (Daily Active)" />
              <Bar dataKey="mau" fill="#388E3C" name="MAU (Monthly Active)" />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>
    </div>
  );
};

export default AdminCommunityAnalytics;
