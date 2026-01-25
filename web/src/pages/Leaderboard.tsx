import { Avatar, Card, Segmented, Space, Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMemo, useState } from 'react';
import { mockLeaderboardData } from '../mock/data';
import type { LeaderboardEntry } from '../types';
import { getPoints, type PointsPeriod } from '../utils/points';
import { TrophyFilled } from '@ant-design/icons';
import './Leaderboard.module.css';

const Leaderboard = () => {
  const [period, setPeriod] = useState<PointsPeriod>('month');

  const dataSource = useMemo(() => {
    const sorted = [...mockLeaderboardData]
      .sort((a, b) => getPoints(b, period) - getPoints(a, period));
    return sorted.map((row, idx) => ({ ...row, rank: idx + 1 }));
  }, [period]);

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
      render: (_: string, row) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <Avatar src={row.avatarUrl} />
          <span>{row.nickname}</span>
        </div>
      ),
    },
    {
      title: 'Points',
      key: 'points',
      width: 160,
      render: (_, row) => (
        <div style={{ display: 'flex', flexDirection: 'column', lineHeight: 1.2 }}>
          <span style={{ fontWeight: 700 }}>{getPoints(row, period)}</span>
          <span style={{ fontSize: 12, color: '#999' }}>{row.emissions.toFixed(2)} kg CO₂e</span>
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
          pagination={false}
        />
      </Card>
    </div>
  );
};

export default Leaderboard;

