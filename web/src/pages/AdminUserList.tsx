import React, { useEffect, useState } from 'react';
import { message, Modal } from 'antd';
import request from '../utils/request';
import './AdminUserList.css';

interface User {
  id: string;
  username: string;
  email: string;
  joinedDate: string;
  totalReduction: number;
  points: number;
  status: string;
}

const AdminUserList: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [users, setUsers] = useState<User[]>([]);
  const [totalUsers, setTotalUsers] = useState<number>(0);
  const [isEditMode, setIsEditMode] = useState(false);
  const [editingPoints, setEditingPoints] = useState<Record<string, number>>({});
  const [editingStatus, setEditingStatus] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [detailUser, setDetailUser] = useState<User | null>(null);
  const [detailVisible, setDetailVisible] = useState(false);

  const fetchUsers = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await request.get('/admin/users', {
        params: {
          q: searchTerm || undefined,
          page: 1,
          pageSize: 50,
        },
      });

      const items: User[] = (Array.isArray(res) ? res : res?.items || res?.data || res || []) as User[];
      const total = typeof res === 'object' && !Array.isArray(res) ? (res?.total || items.length) : items.length;
      setUsers(items);
      setTotalUsers(total);
    } catch (e: any) {
      console.error('Failed to load users:', e);
      setError(
        e?.response?.data?.error ||
          e?.response?.data?.message ||
          e?.message ||
          'Failed to load users.'
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchUsers();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const filteredUsers = users.filter((user) => {
    const term = searchTerm.toLowerCase();
    return (
      user.username.toLowerCase().includes(term) ||
      user.email.toLowerCase().includes(term)
    );
  });

  const handleEditModeToggle = () => {
    if (!isEditMode) {
      // Enter edit mode, save current points and status
      const pointsMap: Record<string, number> = {};
      const statusMap: Record<string, string> = {};
      users.forEach(user => {
        pointsMap[user.id] = user.points;
        statusMap[user.id] = user.status;
      });
      setEditingPoints(pointsMap);
      setEditingStatus(statusMap);
    }
    setIsEditMode(!isEditMode);
  };

  const handlePointsChange = (userId: string, value: number) => {
    setEditingPoints({
      ...editingPoints,
      [userId]: value
    });
  };

  const handleStatusChange = (userId: string, value: string) => {
    setEditingStatus({
      ...editingStatus,
      [userId]: value
    });
  };

  const handleSave = async () => {
    setSaving(true);
    setError(null);
    try {
      const updates = users
        .map((user) => {
          const newPoints = editingPoints[user.id];
          const newStatus = editingStatus[user.id];

          const payload: { id: string; points?: number; status?: 'Active' | 'Banned' } = {
            id: user.id,
          };

          if (newPoints !== undefined && newPoints !== user.points) {
            payload.points = newPoints;
          }
          if (newStatus !== undefined && newStatus !== user.status) {
            payload.status = newStatus as 'Active' | 'Banned';
          }

          return payload;
        })
        .filter((u) => u.points !== undefined || u.status !== undefined);

      if (updates.length > 0) {
        await request.post('/admin/users/batch-update', { updates });
        // 本地同步更新
        setUsers(users.map(user => ({
          ...user,
          points: editingPoints[user.id] !== undefined ? editingPoints[user.id] : user.points,
          status: editingStatus[user.id] !== undefined ? editingStatus[user.id] : user.status,
        })));
        const hasBanned = updates.some(u => u.status === 'Banned');
        if (hasBanned) {
          message.success('User banned successfully');
        } else {
          message.success('Saved successfully');
        }
      }

      setIsEditMode(false);
      setEditingPoints({});
      setEditingStatus({});
    } catch (e: any) {
      console.error('Failed to save users:', e);
      setError(
        e?.response?.data?.error ||
          e?.response?.data?.message ||
          e?.message ||
          'Failed to save user updates.'
      );
    } finally {
      setSaving(false);
    }
  };

  const handleCancel = () => {
    setIsEditMode(false);
    setEditingPoints({});
    setEditingStatus({});
  };

  const handleRowClick = (user: User) => {
    setDetailUser(user);
    setDetailVisible(true);
  };

  const closeDetail = () => {
    setDetailVisible(false);
    setDetailUser(null);
  };

  return (
    <div className="user-list">
      <div className="page-header">
        <h1 className="page-title">User Management {totalUsers > 0 && `(${totalUsers} total)`}</h1>
        <div className="header-actions">
          {isEditMode ? (
            <>
              <button onClick={handleSave} className="action-btn save-btn" disabled={saving}>
                {saving ? 'Saving...' : '✓ Save'}
              </button>
              <button onClick={handleCancel} className="action-btn cancel-btn">
                ✕ Cancel
              </button>
            </>
          ) : (
            <button onClick={handleEditModeToggle} className="action-btn edit-btn">
              Edit
            </button>
          )}
        </div>
      </div>
      
      <div className="search-container">
        <input
          type="text"
          placeholder="Search by username or email..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          onBlur={fetchUsers}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              fetchUsers();
            }
          }}
          className="search-input"
        />
      </div>

      {error && (
        <div className="userlist-error">
          {error}
        </div>
      )}

      <div className="table-card">
        {loading ? (
          <div style={{ padding: '20px', textAlign: 'center' }}>Loading users...</div>
        ) : (
          <div className="table-scroll-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>User ID</th>
                <th>Username</th>
                <th>Email</th>
                <th>Joined Date</th>
                <th>Total Reduction (kg)</th>
                <th>Points</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {filteredUsers.length > 0 ? (
                filteredUsers.map((user) => (
                  <tr
                    key={user.id}
                    className="user-row-clickable"
                    onClick={() => handleRowClick(user)}
                  >
                    <td>{user.id}</td>
                    <td>{user.username}</td>
                    <td>{user.email}</td>
                    <td>{user.joinedDate}</td>
                    <td>{user.totalReduction}</td>
                    <td className="points-cell" onClick={(e) => e.stopPropagation()}>
                      {isEditMode ? (
                        <input
                          type="number"
                          value={editingPoints[user.id] !== undefined ? editingPoints[user.id] : user.points}
                          onChange={(e) => handlePointsChange(user.id, Number(e.target.value))}
                          className="points-input"
                          min="0"
                        />
                      ) : (
                        <span className="points-value">{user.points}</span>
                      )}
                    </td>
                    <td className="status-cell" onClick={(e) => e.stopPropagation()}>
                      {isEditMode ? (
                        <select
                          value={editingStatus[user.id] !== undefined ? editingStatus[user.id] : user.status}
                          onChange={(e) => handleStatusChange(user.id, e.target.value)}
                          className="status-select"
                        >
                          <option value="Active">Active</option>
                          <option value="Banned">Banned</option>
                        </select>
                      ) : (
                        <span className={`status-badge ${user.status.toLowerCase()}`}>
                          {user.status}
                        </span>
                      )}
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: '20px' }}>
                    No users found.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
          </div>
        )}
      </div>

      <Modal
        title="User Info"
        open={detailVisible}
        onCancel={closeDetail}
        footer={null}
        width={480}
      >
        {detailUser && (
          <div className="user-detail-content">
            <div className="user-detail-row">
              <span className="user-detail-label">User ID</span>
              <span className="user-detail-value">{detailUser.id}</span>
            </div>
            <div className="user-detail-row">
              <span className="user-detail-label">Username</span>
              <span className="user-detail-value">{detailUser.username}</span>
            </div>
            <div className="user-detail-row">
              <span className="user-detail-label">Email</span>
              <span className="user-detail-value">{detailUser.email}</span>
            </div>
            <div className="user-detail-row">
              <span className="user-detail-label">Joined Date</span>
              <span className="user-detail-value">{detailUser.joinedDate}</span>
            </div>
            <div className="user-detail-row">
              <span className="user-detail-label">Total Reduction (kg)</span>
              <span className="user-detail-value">{detailUser.totalReduction}</span>
            </div>
            <div className="user-detail-row">
              <span className="user-detail-label">Points</span>
              <span className="user-detail-value">{detailUser.points}</span>
            </div>
            <div className="user-detail-row">
              <span className="user-detail-label">Status</span>
              <span className={`status-badge ${detailUser.status.toLowerCase()}`}>
                {detailUser.status}
              </span>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default AdminUserList;
