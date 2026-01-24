import { Avatar, Card, Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { mockLeaderboardData } from '../mock/data';
import type { LeaderboardEntry } from '../types';
import './Leaderboard.module.css';

const columns: ColumnsType<LeaderboardEntry> = [
  { title: 'Rank', dataIndex: 'rank', key: 'rank', width: 90 },
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
    title: 'Emissions',
    dataIndex: 'emissions',
    key: 'emissions',
    width: 140,
    render: (value: number) => `${value.toFixed(2)} kg`,
  },
];

const Leaderboard = () => {
  return (
    <div style={{ width: '100%' }}>
      <Card
        title={<span style={{ fontSize: 18, fontWeight: 600 }}>Leaderboard</span>}
        style={{ borderRadius: 12, boxShadow: '0 2px 8px rgba(0, 0, 0, 0.06)' }}
      >
        <Table
          rowKey={(row) => `${row.rank}-${row.username}`}
          columns={columns}
          dataSource={mockLeaderboardData}
          pagination={false}
        />
      </Card>
    </div>
  );
};

export default Leaderboard;

