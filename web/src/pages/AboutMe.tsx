import { useState } from 'react';
import { Card, Row, Col, Select } from 'antd';
import { ResponsiveContainer, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, PieChart, Pie, Cell } from 'recharts';
import { ArrowUpOutlined, ArrowDownOutlined, BgColorsOutlined } from '@ant-design/icons';
import './AboutMe.module.css';

interface MonthlyData {
  month: string;
  food: number;
  travel: number;
  utility: number;
  total: number;
}

const AboutMe = () => {
  // Mock monthly data (past 12 months)
  const monthlyData: MonthlyData[] = [
    { month: 'Jan', food: 120, travel: 80, utility: 50, total: 250 },
    { month: 'Feb', food: 110, travel: 75, utility: 55, total: 240 },
    { month: 'Mar', food: 135, travel: 90, utility: 45, total: 270 },
    { month: 'Apr', food: 125, travel: 85, utility: 60, total: 270 },
    { month: 'May', food: 140, travel: 95, utility: 50, total: 285 },
    { month: 'Jun', food: 155, travel: 105, utility: 55, total: 315 },
    { month: 'Jul', food: 165, travel: 110, utility: 65, total: 340 },
    { month: 'Aug', food: 150, travel: 100, utility: 60, total: 310 },
    { month: 'Sep', food: 135, travel: 90, utility: 55, total: 280 },
    { month: 'Oct', food: 125, travel: 85, utility: 50, total: 260 },
    { month: 'Nov', food: 130, travel: 88, utility: 52, total: 270 },
    { month: 'Dec', food: 115, travel: 80, utility: 48, total: 243 },
  ];

  const [selectedMonth, setSelectedMonth] = useState<string>('all');

  // Calculate total emissions
  const totalEmissions = monthlyData.reduce((sum, data) => sum + data.total, 0);

  // Calculate trees planted equivalent (20kg = 1 tree)
  const treesPlanted = Math.floor(totalEmissions / 20);

  // Get pie chart data
  const getPieData = () => {
    if (selectedMonth === 'all') {
      const foodTotal = monthlyData.reduce((sum, data) => sum + data.food, 0);
      const travelTotal = monthlyData.reduce((sum, data) => sum + data.travel, 0);
      const utilityTotal = monthlyData.reduce((sum, data) => sum + data.utility, 0);
      return [
        { name: 'Food', value: foodTotal },
        { name: 'Travel', value: travelTotal },
        { name: 'Utility', value: utilityTotal },
      ];
    } else {
      const monthData = monthlyData.find(d => d.month === selectedMonth);
      if (monthData) {
        return [
          { name: 'Food', value: monthData.food },
          { name: 'Travel', value: monthData.travel },
          { name: 'Utility', value: monthData.utility },
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
              <div style={{ fontSize: '28px', fontWeight: 'bold', color: '#52c41a', marginBottom: '8px', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '6px' }}>
                <ArrowDownOutlined /> 15%
              </div>
              <div style={{ fontSize: '12px', color: '#52c41a' }}>lower than average</div>
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
          <ResponsiveContainer width="100%" height={300}>
            <PieChart>
              <Pie
                data={pieData}
                cx="50%"
                cy="50%"
                labelLine={false}
                label={({ name, value }) => `${name}: ${value} kg`}
                outerRadius={80}
                fill="#8884d8"
                dataKey="value"
              >
                {pieData.map((entry, index) => (
                  <Cell key={`cell-${index}`} fill={COLORS[entry.name as keyof typeof COLORS]} />
                ))}
              </Pie>
              <Tooltip formatter={(value) => `${value} kg`} />
              <Legend />
            </PieChart>
          </ResponsiveContainer>
        </div>
      </Card>
    </div>
  );
};

export default AboutMe;
