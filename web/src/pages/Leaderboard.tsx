import { Avatar, Card, Segmented, Space, Table, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMemo, useState, useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import type { LeaderboardEntry } from '../types';
import { getPoints, type PointsPeriod } from '../utils/points';
import { TrophyFilled } from '@ant-design/icons';
import './Leaderboard.module.css';
import request from '../utils/request';

const Leaderboard = () => {
  const location = useLocation();
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<LeaderboardEntry[]>([]);
  const [period, setPeriod] = useState<PointsPeriod>(() => {
    const defaultTab = (location.state as any)?.defaultTab;
    if (defaultTab && ['today', 'week', 'month', 'all'].includes(defaultTab)) {
      return defaultTab as PointsPeriod;
    }
    return 'month';
  });

  const fetchLeaderboard = async (currentPeriod: string) => {
    setLoading(true);
    try {
      // 后端 API: /api/Leaderboard?period=...
      const res: any = await request.get(`/api/Leaderboard`, {
        params: { period: currentPeriod }
      });
      // 假设返回的是数组，或者在 res.items 中
      const list = Array.isArray(res) ? res : res.items || [];
      setData(list);
    } catch (error: any) {
      console.error('Failed to fetch leaderboard:', error);
      message.error('Failed to load leaderboard data');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLeaderboard(period);
  }, [period]);

  const dataSource = useMemo(() => {
    return data.map((row, idx) => ({ ...row, rank: idx + 1 }));
  }, [data]);

  const columns: ColumnsType<LeaderboardEntry> = [
    {
      title: 'Rank',
      dataIndex: 'rank',
      key: 'rank',
      width: 80,
      align: 'center',
      render: (rank: number) => {
        const trophyColor =
          rank === 1 ? '#FFD700' :
          rank === 2 ? '#C0C0C0' :
          rank === 3 ? '#CD7F32' :
          null;

        if (!trophyColor) {
          return (
            <div style={{ display: 'flex', justifyContent: 'center' }}>
              <span style={{ fontWeight: 600 }}>{rank}</span>
            </div>
          );
        }

        return (
          <div style={{ display: 'flex', justifyContent: 'center' }}>
            <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6, fontWeight: 700 }}>
              <TrophyFilled style={{ color: trophyColor }} />
              {rank}
            </span>
          </div>
        );
      },
    },
    {
      title: 'User',
      dataIndex: 'username',
      key: 'username',
      render: (_: string, row) => {
        const baseUrl = import.meta.env.VITE_API_URL || '';
        const avatarSrc = row.avatarUrl 
          ? (row.avatarUrl.startsWith('http') ? row.avatarUrl : `${baseUrl}${row.avatarUrl}`)
          : undefined;
        return (
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <Avatar src={avatarSrc} />
            <span>{row.nickname || row.username}</span>
          </div>
        );
      },
    },
    {
      title: 'Points',
      key: 'points',
      width: 160,
      render: (_, row) => (
        <div style={{ display: 'flex', flexDirection: 'column', lineHeight: 1.2 }}>
          <span style={{ fontWeight: 700 }}>{getPoints(row, period)}</span>
          <span style={{ fontSize: 12, color: '#999' }}>{(row.emissions || 0).toFixed(2)} kg CO₂e</span>
        </div>
      ),
    },
  ];

  return (
    <div style={{ width: '100%' }}>
      <Card
        title={<span style={{ fontSize: 18, fontWeight: 600 }}>Leaderboard</span>}
        style={{ borderRadius: 12, boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)' }}
      >
        <Space style={{ marginBottom: 16 }}>
          <Segmented
            value={period}
            onChange={(val) => setPeriod(val as PointsPeriod)}
            options={[
              { label: 'Today', value: 'today' },
              { label: 'This Week', value: 'week' },
              { label: 'This Month', value: 'month' },
              { label: 'All Time', value: 'all' },
            ]}
          />
        </Space>
        <Table
          rowKey={(row) => row.username}
          columns={columns}
          dataSource={dataSource}
          loading={loading}
          pagination={{ pageSize: 20 }}
        />
      </Card>
    </div>
  );
};

export default Leaderboard;

