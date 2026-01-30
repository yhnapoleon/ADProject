import React, { useEffect, useState } from 'react';
import { PieChart, Pie, Cell, ResponsiveContainer, Legend, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip } from 'recharts';
import request from '../utils/request';
import './AdminCommunityAnalytics.css';

interface CategoryShareItem {
  name: string;
  value: number;
}

interface EngagementItem {
  month: string;
  dau: number;
  mau: number;
}

const AdminCommunityAnalytics: React.FC = () => {
  const [pieData, setPieData] = useState<CategoryShareItem[]>([]);
  const [barData, setBarData] = useState<EngagementItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchAnalytics = async () => {
      setLoading(true);
      setError(null);
      try {
        const [categoryShareRes, engagementRes] = await Promise.all([
          request.get('/admin/analytics/category-share'),
          request.get('/admin/analytics/engagement'),
        ]);

        setPieData(Array.isArray(categoryShareRes) ? categoryShareRes : []);
        setBarData(Array.isArray(engagementRes) ? engagementRes : []);
      } catch (e: any) {
        console.error('Failed to load community analytics:', e);
        setError(
          e?.response?.data?.error ||
            e?.response?.data?.message ||
            e?.message ||
            'Failed to load analytics data.'
        );
      } finally {
        setLoading(false);
      }
    };

    fetchAnalytics();
  }, []);

  const COLORS = ['#A5D6A7', '#66BB6A', '#4CAF50', '#388E3C'];

  return (
    <div className="community-analytics">
      <h1 className="page-title">Community Analytics Deep Dive</h1>

      {error && (
        <div className="analytics-error">
          {error}
        </div>
      )}
      
      <div className="charts-container">
        <div className="chart-card">
          <h3>Total Reduction Share by Category</h3>
          {loading && pieData.length === 0 ? (
            <div style={{ height: 300, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              Loading category share...
            </div>
          ) : (
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={pieData}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={({ name, percent }) => (percent >= 0.01 ? `${name}: ${(percent * 100).toFixed(1)}%` : null)}
                  outerRadius={100}
                  fill="#8884d8"
                  dataKey="value"
                  nameKey="name"
                >
                  {pieData.map((_, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip />
                <Legend />
              </PieChart>
            </ResponsiveContainer>
          )}
        </div>

        <div className="chart-card">
          <h3>User Engagement Growth (2024)</h3>
          {loading && barData.length === 0 ? (
            <div style={{ height: 300, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              Loading engagement data...
            </div>
          ) : (
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={barData}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e0e0e0" />
                <XAxis dataKey="month" stroke="#666" />
                <YAxis stroke="#666" />
                <Tooltip />
                <Legend />
                <Bar dataKey="dau" fill="#A5D6A7" name="DAU (Daily Active)" />
                <Bar dataKey="mau" fill="#388E3C" name="MAU (Monthly Active)" />
              </BarChart>
            </ResponsiveContainer>
          )}
        </div>
      </div>
    </div>
  );
};

export default AdminCommunityAnalytics;
