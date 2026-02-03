import { useState, useEffect } from 'react';
import { Card, Row, Col, Select, Spin, message } from 'antd';
import { ResponsiveContainer, LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, PieChart, Pie, Cell } from 'recharts';
import { ArrowUpOutlined, ArrowDownOutlined } from '@ant-design/icons';
import './AboutMe.module.css';
import request from '../utils/request';

/** Backend may return PascalCase (Month, EmissionsTotal...) or camelCase (month, emissionsTotal...).
 * Normalize on the client to always work with camelCase fields.
 */
interface MonthlyEmissionDto {
  month: string;
  emissionsTotal: number;
  food: number;
  transport: number;
  utility: number;
  averageAllUsers: number;
} 

const AboutMe = () => {
  const [loading, setLoading] = useState(true);
  const [monthlyEmissions, setMonthlyEmissions] = useState<MonthlyEmissionDto[]>([]);
  const [comparison, setComparison] = useState<{ percent: number; status: 'lower' | 'higher'; available: boolean }>({ percent: 0, status: 'lower', available: false });
  const [selectedMonth, setSelectedMonth] = useState<string>('all');
  const [latestEmissions, setLatestEmissions] = useState<number>(0);
  const [totalTrees, setTotalTrees] = useState<number>(0);

  // Normalize DTOs coming from backend (support both PascalCase and camelCase)
  const normalizeMonthly = (raw: any): MonthlyEmissionDto => ({
    month: raw?.Month ?? raw?.month ?? '',
    emissionsTotal: Number(raw?.EmissionsTotal ?? raw?.emissionsTotal ?? 0),
    food: Number(raw?.Food ?? raw?.food ?? 0),
    transport: Number(raw?.Transport ?? raw?.transport ?? raw?.Travel ?? 0),
    utility: Number(raw?.Utility ?? raw?.utility ?? 0),
    averageAllUsers: Number(raw?.AverageAllUsers ?? raw?.averageAllUsers ?? 0),
  });

  useEffect(() => {
    const fetchAboutMeData = async () => {
      setLoading(true);
      try {
        const res = await request.get<MonthlyEmissionDto[] | { monthlyData: any[]; comparisonPercent?: number; comparisonStatus?: string }>('/api/about-me');
        const rawList = Array.isArray(res) ? (res as any[]) : (res && (res as any).monthlyData ? (res as any).monthlyData : []);

        if (rawList && rawList.length > 0) {
          const normalized = rawList.map(normalizeMonthly);
          // store normalized DTOs (camelCase)
          setMonthlyEmissions(normalized);

          // Total emissions = sum over available months (past 12 months)
          const total12 = normalized.reduce((s: number, d: MonthlyEmissionDto) => s + (Number(d.emissionsTotal) || 0), 0);
          setLatestEmissions(total12);

          // Comparison: compare average monthly user emissions vs averageAllUsers (monthly)
          const userAvgPerMonth = total12;
          const avgAvg = normalized.reduce((s: number, d: MonthlyEmissionDto) => s + (Number(d.averageAllUsers) || 0), 0);

          if (avgAvg > 0) {
            const percent = Math.round(((userAvgPerMonth - avgAvg) / avgAvg) * 100);
            setComparison({
              percent: Math.abs(percent),
              status: userAvgPerMonth < avgAvg ? 'lower' : 'higher',
              available: true,
            });
          } else {
            setComparison((c) => ({ ...c, available: false }));
          }
        } else {
          setMonthlyEmissions([]);
          setLatestEmissions(0);
          setComparison((c) => ({ ...c, available: false }));
        }

        // Also fetch total trees from tree service (used in TreePlanting page)
        try {
          const treeRes = await request.get('/api/getTree') as { totalTrees?: number };
          setTotalTrees(Number(treeRes?.totalTrees ?? 0));
        } catch (e) {
          console.warn('Failed to load tree data:', e);
          setTotalTrees(0);
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

  // Total emissions shown = sum of past months (normalized to kg)
  const totalEmissions = latestEmissions; 

  // Trees planted count comes from tree service (TreePlanting page)
  const treesPlanted = totalTrees;

  // Get pie chart data
  const getPieData = () => {
    if (selectedMonth === 'all') {
      const foodTotal = monthlyEmissions.reduce((sum, data) => sum + (Number(data.food) || 0), 0);
      const travelTotal = monthlyEmissions.reduce((sum, data) => sum + (Number(data.transport) || 0), 0);
      const utilityTotal = monthlyEmissions.reduce((sum, data) => sum + (Number(data.utility) || 0), 0);
      return [
        { name: 'Food', value: foodTotal },
        { name: 'Travel', value: travelTotal },
        { name: 'Utility', value: utilityTotal },
      ];
    } else {
      const monthData = monthlyEmissions.find(d => d.month === selectedMonth);
      if (monthData) {
        return [
          { name: 'Food', value: Number(monthData.food) || 0 },
          { name: 'Travel', value: Number(monthData.transport) || 0 },
          { name: 'Utility', value: Number(monthData.utility) || 0 },
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
    ...monthlyEmissions.map(d => ({ label: d.month, value: d.month })),
  ];

  const chartData = monthlyEmissions.map(m => ({ month: m.month, total: Number(m.emissionsTotal) }));

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
                {totalEmissions.toFixed(1)} kg
              </div>
              <div style={{ fontSize: '12px', color: '#666' }}>last 12 months</div>
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
                color: comparison.available ? (comparison.status === 'lower' ? '#52c41a' : '#ff4d4f') : '#999', 
                marginBottom: '8px', 
                display: 'flex', 
                alignItems: 'center', 
                justifyContent: 'center', 
                gap: '6px' 
              }}>
                {comparison.available ? (comparison.status === 'lower' ? <ArrowDownOutlined /> : <ArrowUpOutlined />) : null}
                {comparison.available ? `${comparison.percent}%` : '—'}
              </div>
              <div style={{ fontSize: '12px', color: comparison.available ? (comparison.status === 'lower' ? '#52c41a' : '#ff4d4f') : '#999' }}>
                {comparison.available ? `${comparison.status} than average` : 'No comparison data'}
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
        {chartData.length > 0 ? (
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={chartData} margin={{ top: 5, right: 30, left: 0, bottom: 5 }}>
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
