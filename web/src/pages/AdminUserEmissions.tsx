import React, { useState } from 'react';
import request from '../utils/request';
import './AdminUserEmissions.css';

type TimePreset = 'all' | '7' | '30' | 'custom';

interface EmissionStats {
  userId: number;
  totalItems: number;
  totalEmission: number;
  from?: string;
  to?: string;
}

interface UserItem {
  id: string;
  username: string;
  email: string;
}

const AdminUserEmissions: React.FC = () => {
  const [userId, setUserId] = useState('');
  const [selectedUser, setSelectedUser] = useState<UserItem | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [searchLoading, setSearchLoading] = useState(false);
  const [userMatches, setUserMatches] = useState<UserItem[]>([]);
  const [timePreset, setTimePreset] = useState<TimePreset>('all');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<EmissionStats | null>(null);

  const handleSearchUser = async () => {
    const q = searchTerm.trim();
    if (!q) {
      setUserMatches([]);
      return;
    }
    setSearchLoading(true);
    setUserMatches([]);
    setError(null);
    try {
      const res = await request.get('/admin/users', {
        params: { q, page: 1, pageSize: 20 },
      });
      const items = (res?.items ?? (Array.isArray(res) ? res : [])) as UserItem[];
      setUserMatches(items);
      if (items.length === 0) setError('No users found for this search.');
    } catch (e: any) {
      const msg = e?.response?.data?.error ?? e?.message ?? 'Search failed.';
      setError(msg);
    } finally {
      setSearchLoading(false);
    }
  };

  const selectUser = (u: UserItem) => {
    setUserId(u.id);
    setSelectedUser(u);
    setUserMatches([]);
    setSearchTerm('');
    setError(null);
  };

  const handleQuery = async () => {
    const uid = userId.trim();
    if (!uid) {
      setError('Please enter User ID.');
      return;
    }
    const idNum = parseInt(uid, 10);
    if (Number.isNaN(idNum) || idNum <= 0) {
      setError('User ID must be a positive number.');
      return;
    }

    setLoading(true);
    setError(null);
    setResult(null);
    try {
      const params: Record<string, string | number> = {};
      if (timePreset === '7') params.days = 7;
      else if (timePreset === '30') params.days = 30;
      else if (timePreset === 'custom') {
        if (fromDate) params.from = fromDate;
        if (toDate) params.to = toDate;
      }

      const res = await request.get(`/admin/users/${idNum}/emissions/stats`, { params });
      setResult(res as EmissionStats);
    } catch (e: any) {
      const msg =
        e?.response?.data?.error ||
        e?.response?.data?.message ||
        e?.message ||
        'Failed to load emission stats.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="admin-user-emissions">
      <h1 className="page-title">User Carbon Emissions Query</h1>
      <p className="page-desc">Query a user&apos;s carbon emission statistics by User ID, username, or email, and optional time range.</p>

      <div className="query-card">
        <div className="form-row">
          <label>Search by username or email</label>
          <div className="search-user-row">
            <input
              type="text"
              placeholder="Type username or email, then click Search"
              value={searchTerm}
              onChange={(e) => { setSearchTerm(e.target.value); setUserMatches([]); }}
              onKeyDown={(e) => e.key === 'Enter' && handleSearchUser()}
              className="input-field search-input"
            />
            <button
              type="button"
              onClick={handleSearchUser}
              disabled={searchLoading}
              className="search-user-btn"
            >
              {searchLoading ? 'Searching...' : 'Search'}
            </button>
          </div>
          {userMatches.length > 0 && (
            <ul className="user-matches-list">
              {userMatches.map((u) => (
                <li key={u.id}>
                  <button type="button" className="user-match-item" onClick={() => selectUser(u)}>
                    <span className="match-id">ID: {u.id}</span>
                    <span className="match-name">{u.username}</span>
                    <span className="match-email">{u.email}</span>
                  </button>
                </li>
              ))}
            </ul>
          )}
          {selectedUser && (
            <p className="selected-user-hint">
              Selected: <strong>{selectedUser.username}</strong> ({selectedUser.email}) — ID {selectedUser.id}
              <button type="button" className="clear-selection" onClick={() => { setUserId(''); setSelectedUser(null); }}>Clear</button>
            </p>
          )}
        </div>
        <div className="form-row">
          <label htmlFor="userId">User ID (or select above)</label>
          <input
            id="userId"
            type="text"
            placeholder="e.g. 1"
            value={userId}
            onChange={(e) => { setUserId(e.target.value); setSelectedUser(null); }}
            className="input-field"
          />
        </div>
        <div className="form-row">
          <label>Time Range</label>
          <div className="time-options">
            <label className="radio-label">
              <input
                type="radio"
                name="time"
                checked={timePreset === 'all'}
                onChange={() => setTimePreset('all')}
              />
              All time
            </label>
            <label className="radio-label">
              <input
                type="radio"
                name="time"
                checked={timePreset === '7'}
                onChange={() => setTimePreset('7')}
              />
              Last 7 days
            </label>
            <label className="radio-label">
              <input
                type="radio"
                name="time"
                checked={timePreset === '30'}
                onChange={() => setTimePreset('30')}
              />
              Last 30 days
            </label>
            <label className="radio-label">
              <input
                type="radio"
                name="time"
                checked={timePreset === 'custom'}
                onChange={() => setTimePreset('custom')}
              />
              Custom range
            </label>
          </div>
        </div>
        {timePreset === 'custom' && (
          <div className="form-row date-range">
            <label htmlFor="fromDate">From</label>
            <input
              id="fromDate"
              type="date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
              className="input-field"
            />
            <label htmlFor="toDate">To</label>
            <input
              id="toDate"
              type="date"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
              className="input-field"
            />
          </div>
        )}
        <div className="form-actions">
          <button
            type="button"
            onClick={handleQuery}
            disabled={loading}
            className="query-btn"
          >
            {loading ? 'Querying...' : 'Query'}
          </button>
        </div>
      </div>

      {error && (
        <div className="emissions-error">
          {error}
        </div>
      )}

      {result && (
        <div className="result-card">
          <h2 className="result-title">Result</h2>
          <dl className="result-list">
            <div className="result-row">
              <dt>User ID</dt>
              <dd>{result.userId}</dd>
            </div>
            {result.from != null && (
              <div className="result-row">
                <dt>From</dt>
                <dd>{result.from}</dd>
              </div>
            )}
            {result.to != null && (
              <div className="result-row">
                <dt>To</dt>
                <dd>{result.to}</dd>
              </div>
            )}
            <div className="result-row">
              <dt>Record count</dt>
              <dd>{result.totalItems}</dd>
            </div>
            <div className="result-row highlight">
              <dt>Total emission (kg CO₂e)</dt>
              <dd>{Number(result.totalEmission).toFixed(2)}</dd>
            </div>
          </dl>
        </div>
      )}
    </div>
  );
};

export default AdminUserEmissions;
