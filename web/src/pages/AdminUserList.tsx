import React, { useState } from 'react';
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
  const [users, setUsers] = useState<User[]>([
    { id: 'U-1024', username: 'eco_warrior_99', email: 'warrior@email.com', joinedDate: '2024-01-15', totalReduction: 450.5, points: 1250, status: 'Active' },
    { id: 'U-1025', username: 'sarah_j', email: 'sarah.j@email.com', joinedDate: '2024-01-16', totalReduction: 120.0, points: 850, status: 'Active' },
    { id: 'U-1026', username: 'mike_steaklover', email: 'mike@email.com', joinedDate: '2024-01-18', totalReduction: 10.2, points: 320, status: 'Active' },
    { id: 'U-1027', username: 'spammer_bot', email: 'bot@spam.com', joinedDate: '2024-01-20', totalReduction: 0.0, points: 0, status: 'Banned' },
    { id: 'U-1028', username: 'new_user_01', email: 'new01@email.com', joinedDate: '2024-01-21', totalReduction: 5.5, points: 150, status: 'Active' },
  ]);
  const [isEditMode, setIsEditMode] = useState(false);
  const [editingPoints, setEditingPoints] = useState<Record<string, number>>({});
  const [editingStatus, setEditingStatus] = useState<Record<string, string>>({});

  const filteredUsers = users.filter(
    (user) =>
      user.username.toLowerCase().includes(searchTerm.toLowerCase()) ||
      user.email.toLowerCase().includes(searchTerm.toLowerCase())
  );

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

  const handleSave = () => {
    // Update all users' points and status
    setUsers(users.map(user => ({
      ...user,
      points: editingPoints[user.id] !== undefined ? editingPoints[user.id] : user.points,
      status: editingStatus[user.id] !== undefined ? editingStatus[user.id] : user.status
    })));
    setIsEditMode(false);
    // Add API call here to batch save to backend
    console.log('Save all user information:', { points: editingPoints, status: editingStatus });
  };

  const handleCancel = () => {
    setIsEditMode(false);
    setEditingPoints({});
    setEditingStatus({});
  };

  return (
    <div className="user-list">
      <div className="page-header">
        <h1 className="page-title">User Management</h1>
        <div className="header-actions">
          {isEditMode ? (
            <>
              <button onClick={handleSave} className="action-btn save-btn">
                ✓ Save
              </button>
              <button onClick={handleCancel} className="action-btn cancel-btn">
                ✕ Cancel
              </button>
            </>
          ) : (
            <button onClick={handleEditModeToggle} className="action-btn edit-btn">
              ✏️ Edit
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
          className="search-input"
        />
      </div>

      <div className="table-card">
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
            {filteredUsers.map((user) => (
              <tr key={user.id}>
                <td>{user.id}</td>
                <td>{user.username}</td>
                <td>{user.email}</td>
                <td>{user.joinedDate}</td>
                <td>{user.totalReduction}</td>
                <td className="points-cell">
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
                <td className="status-cell">
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
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default AdminUserList;
