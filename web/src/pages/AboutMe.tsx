import { useState, useEffect } from 'react';
import { Card, Row, Col, Select, Spin, message } from 'antd';
import { ResponsiveContainer, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, PieChart, Pie, Cell } from 'recharts';
import { ArrowUpOutlined, ArrowDownOutlined } from '@ant-design/icons';
import './AboutMe.module.css';
import request from '../utils/request';

interface MonthlyData {
  month: string;
  food: number;
  travel: number;
  utility: number;
  total: number;
}

/** Backend returns array of { Month, EmissionsTotal, Food, Transport, Utility, AverageAllUsers } */
interface MonthlyEmissionDto {
  Month: string;
  EmissionsTotal: number;
  Food: number;
  Transport: number;
  Utility: number;
  AverageAllUsers: number;
}

const AboutMe = () => {
  const [loading, setLoading] = useState(true);
  const [monthlyData, setMonthlyData] = useState<MonthlyData[]>([]);
  const [comparison, setComparison] = useState({ percent: 15, status: 'lower' });
  const [selectedMonth, setSelectedMonth] = useState<string>('all');

  useEffect(() => {
    const fetchAboutMeData = async () => {
      setLoading(true);
      try {
        const res = await request.get<MonthlyEmissionDto[] | { monthlyData: MonthlyData[]; comparisonPercent?: number; comparisonStatus?: string }>('/api/about-me');
        if (Array.isArray(res) && res.length > 0) {
          const dtoList = res as MonthlyEmissionDto[];
          const mapped: MonthlyData[] = dtoList.map((d) => ({
            month: d.Month,
            total: Number(d.EmissionsTotal),
            food: Number(d.Food),
            travel: Number(d.Transport),
            utility: Number(d.Utility),
          }));
          setMonthlyData(mapped);
          const last = dtoList[dtoList.length - 1];
          const avg = Number(last.AverageAllUsers);
          const userTotal = Number(last.EmissionsTotal);
          if (avg > 0) {
            const percent = Math.round(((userTotal - avg) / avg) * 100);
            setComparison({
              percent: Math.abs(percent),
              status: userTotal < avg ? 'lower' : 'higher',
            });
          }
        } else if (res && (res as any).monthlyData?.length > 0) {
          setMonthlyData((res as any).monthlyData);
          if ((res as any).comparisonPercent !== undefined) {
            setComparison({
              percent: (res as any).comparisonPercent,
              status: (res as any).comparisonStatus || 'lower',
            });
          }
        }
      } catch (error) {
        console.error('Failed to fetch about-me data:', error);
        message.error('Failed to load your carbon journey data');
      } finally {
        setLoading(false);
      }
    };
    fetchAboutMeData();
  }, []);

  // Calculate total emissions
  const totalEmissions = monthlyData.reduce((sum, data) => sum + data.total, 0);

  // Calculate trees planted equivalent (20kg = 1 tree)
  const treesPlanted = Math.floor(totalEmissions / 20);

  // Get pie chart data
  const getPieData = () => {
    if (selectedMonth === 'all') {
      const foodTotal = monthlyData.reduce((sum, data) => sum + (data.food || 0), 0);
      const travelTotal = monthlyData.reduce((sum, data) => sum + (data.travel || 0), 0);
      const utilityTotal = monthlyData.reduce((sum, data) => sum + (data.utility || 0), 0);
      return [
        { name: 'Food', value: foodTotal },
        { name: 'Travel', value: travelTotal },
        { name: 'Utility', value: utilityTotal },
      ];
    } else {
      const monthData = monthlyData.find(d => d.month === selectedMonth);
      if (monthData) {
        return [
          { name: 'Food', value: monthData.food || 0 },
          { name: 'Travel', value: monthData.travel || 0 },
          { name: 'Utility', value: monthData.utility || 0 },
        ];
      }
      return [];
    }
  };

  const pieData = getPieData();
  const COLORS = {
    Food: '#9b77f7',
    Travel: '#69b3f9',
    Utility: '#fce354',
  };

  const monthOptions = [
    { label: 'All Time', value: 'all' },
    ...monthlyData.map(d => ({ label: d.month, value: d.month })),
  ];

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
        <Spin size="large" tip="Loading your carbon journey..." />
      </div>
    );
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Top Metrics Section */}
      <Row gutter={24}>
        {/* Total Emissions Card */}
        <Col xs={24} sm={12} md={8}>
          <Card style={{ borderRadius: '12px', border: '1px solid #f0f0f0', height: '100%' }}>
            <div style={{ textAlign: 'center' }}>
              <div style={{ fontSize: '14px', color: '#999', marginBottom: '8px' }}>Total Emissions</div>
              <div style={{ fontSize: '32px', fontWeight: 'bold', color: '#674fa3', marginBottom: '8px' }}>
                {totalEmissions.toFixed(0)} kg
              </div>
              <div style={{ fontSize: '12px', color: '#666' }}>since you joined</div>
            </div>
          </Card>
        </Col>

        {/* Comparison Card */}
        <Col xs={24} sm={12} md={8}>
          <Card style={{ borderRadius: '12px', border: '1px solid #f0f0f0', height: '100%' }}>
            <div style={{ textAlign: 'center' }}>
              <div style={{ fontSize: '14px', color: '#999', marginBottom: '8px' }}>Comparison</div>
              <div style={{ 
                fontSize: '28px', 
                fontWeight: 'bold', 
                color: comparison.status === 'lower' ? '#52c41a' : '#ff4d4f', 
                marginBottom: '8px', 
                display: 'flex', 
                alignItems: 'center', 
                justifyContent: 'center', 
                gap: '6px' 
              }}>
                {comparison.status === 'lower' ? <ArrowDownOutlined /> : <ArrowUpOutlined />} {comparison.percent}%
              </div>
              <div style={{ fontSize: '12px', color: comparison.status === 'lower' ? '#52c41a' : '#ff4d4f' }}>
                {comparison.status} than average
              </div>
            </div>
          </Card>
        </Col>

        {/* Trees Planted Card */}
        <Col xs={24} sm={12} md={8}>
          <Card style={{ borderRadius: '12px', border: '1px solid #f0f0f0', height: '100%' }}>
            <div style={{ textAlign: 'center' }}>
              <div style={{ fontSize: '14px', color: '#999', marginBottom: '8px' }}>Trees Planted</div>
              <div style={{ fontSize: '28px', fontWeight: 'bold', color: '#52c41a', marginBottom: '8px' }}>
                🌱 {treesPlanted}
              </div>
              <div style={{ fontSize: '12px', color: '#666' }}>equivalent trees</div>
            </div>
          </Card>
        </Col>
      </Row>

      {/* Monthly Trend Chart */}
      <Card style={{ borderRadius: '12px', border: '1px solid #f0f0f0' }}>
        <div style={{ fontSize: '16px', fontWeight: '600', marginBottom: '20px' }}>Monthly Emission Trend</div>
        {monthlyData.length > 0 ? (
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={monthlyData} margin={{ top: 5, right: 30, left: 0, bottom: 5 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
              <XAxis dataKey="month" />
              <YAxis />
              <Tooltip />
              <Legend />
              <Line
                type="monotone"
                dataKey="total"
                stroke="#6B4EFF"
                dot={false}
                strokeWidth={2}
                name="Total Emissions (kg)"
              />
            </LineChart>
          </ResponsiveContainer>
        ) : (
          <div style={{ textAlign: 'center', padding: '40px', color: '#999' }}>No data available yet</div>
        )}
      </Card>

      {/* Emission Composition Pie Chart */}
      <Card style={{ borderRadius: '12px', border: '1px solid #f0f0f0' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <div style={{ fontSize: '16px', fontWeight: '600' }}>Emission Composition</div>
          <Select
            value={selectedMonth}
            onChange={setSelectedMonth}
            options={monthOptions}
            style={{ width: '150px' }}
          />
        </div>
        <div style={{ display: 'flex', justifyContent: 'center' }}>
          {pieData.some(d => d.value > 0) ? (
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={pieData}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={({ name, value }) => `${name}: ${value.toFixed(1)} kg`}
                  outerRadius={80}
                  fill="#8884d8"
                  dataKey="value"
                >
                  {pieData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[entry.name as keyof typeof COLORS]} />
                  ))}
                </Pie>
                <Tooltip formatter={(value: number) => `${value.toFixed(1)} kg`} />
                <Legend />
              </PieChart>
            </ResponsiveContainer>
          ) : (
            <div style={{ textAlign: 'center', padding: '40px', color: '#999' }}>No emissions recorded for this period</div>
          )}
        </div>
      </Card>
    </div>
  );
};

export default AboutMe;
