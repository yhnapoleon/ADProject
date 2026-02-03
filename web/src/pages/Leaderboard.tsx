import { Avatar, Card, Segmented, Space, Table, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMemo, useState, useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import type { LeaderboardEntry } from '../types';
import { TrophyFilled } from '@ant-design/icons';
import './Leaderboard.module.css';
import request from '../utils/request';

export type LeaderboardPeriod = 'today' | 'month' | 'all';

const Leaderboard = () => {
  const location = useLocation();
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<LeaderboardEntry[]>([]);
  const [period, setPeriod] = useState<LeaderboardPeriod>(() => {
    const defaultTab = (location.state as any)?.defaultTab;
    if (defaultTab && ['today', 'month', 'all'].includes(defaultTab)) {
      return defaultTab as LeaderboardPeriod;
    }
    return 'month';
  });

  const fetchLeaderboard = async (currentPeriod: LeaderboardPeriod) => {
    setLoading(true);
    try {
      let url: string;
      if (currentPeriod === 'today') url = '/api/Leaderboard/today';
      else if (currentPeriod === 'month') url = '/api/Leaderboard/month';
      else url = '/api/Leaderboard';
      const res: any = await request.get(url);
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
      dataIndex: 'nickname',
      key: 'user',
      render: (_: string, row: any) => {
        const normalizeUrl = (url?: string | null) => {
          if (!url) return '';
          const s = String(url);
          if (s.startsWith('data:image')) return s;
          if (s.startsWith('http')) return s;
          return `${import.meta.env.VITE_API_URL || ''}${s}`;
        };
        const avatarRaw = row.avatarUrl ?? row.avatar;
        const avatarSrc = normalizeUrl(avatarRaw) || undefined;
        const name = row.nickname ?? row.username ?? '';
        const initials = name && name.trim().length > 0
          ? (name.trim().split(/\s+/).length === 1
              ? name.trim().slice(0, 2).toUpperCase()
              : (name.trim().split(/\s+/)[0][0] + name.trim().split(/\s+/).slice(-1)[0][0]).toUpperCase())
          : undefined;
        return (
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <Avatar src={avatarSrc}>{initials}</Avatar>
            <span>{name || '-'}</span>
          </div>
        );
      },
    },
    {
      title: 'Points',
      key: 'points',
      width: 160,
      render: (_: unknown, row: any) => {
        const points = row.pointsTotal ?? 0;
        const emissions = row.emissionsTotal ?? row.emissions ?? 0;
        return (
          <div style={{ display: 'flex', flexDirection: 'column', lineHeight: 1.2 }}>
            <span style={{ fontWeight: 700 }}>{Number(points).toLocaleString()} pts</span>
            <span style={{ fontSize: 12, color: '#999' }}>{Number(emissions).toFixed(2)} kg CO₂e</span>
          </div>
        );
      },
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
            onChange={(val) => setPeriod(val as LeaderboardPeriod)}
            options={[
              { label: 'Today', value: 'today' },
              { label: 'This Month', value: 'month' },
              { label: 'All Time', value: 'all' },
            ]}
          />
        </Space>
        <Table
          rowKey={(row) => row.username ?? String(row.rank ?? Math.random())}
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

