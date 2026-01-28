import React, { useEffect, useState } from 'react';
import request from '../utils/request';
import './AdminSettings.css';

const AdminSettings: React.FC = () => {
  const [confidenceThreshold, setConfidenceThreshold] = useState(85);
  const [visionModel, setVisionModel] = useState('v2.1.0 (Stable)');
  const [weeklyDigest, setWeeklyDigest] = useState(true);
  const [maintenanceMode, setMaintenanceMode] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchSettings = async () => {
      setLoading(true);
      setError(null);
      try {
        const res = await request.get('/admin/settings');
        if (res) {
          if (typeof res.confidenceThreshold === 'number') {
            setConfidenceThreshold(res.confidenceThreshold);
          }
          if (typeof res.visionModel === 'string') {
            setVisionModel(res.visionModel);
          }
          if (typeof res.weeklyDigest === 'boolean') {
            setWeeklyDigest(res.weeklyDigest);
          }
          if (typeof res.maintenanceMode === 'boolean') {
            setMaintenanceMode(res.maintenanceMode);
          }
        }
      } catch (e: any) {
        console.error('Failed to load admin settings:', e);
        setError(
          e?.response?.data?.error ||
            e?.response?.data?.message ||
            e?.message ||
            'Failed to load settings.'
        );
      } finally {
        setLoading(false);
      }
    };

    fetchSettings();
  }, []);

  const handleSave = async () => {
    setSaving(true);
    setMessage(null);
    setError(null);
    try {
      await request.put('/admin/settings', {
        confidenceThreshold,
        visionModel,
        weeklyDigest,
        maintenanceMode,
      });
      setMessage('Settings saved successfully.');
    } catch (e: any) {
      console.error('Failed to save admin settings:', e);
      setError(
        e?.response?.data?.error ||
          e?.response?.data?.message ||
          e?.message ||
          'Failed to save settings.'
      );
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="settings">
      <h1 className="page-title">System Settings</h1>

      {loading && (
        <div className="settings-status">Loading settings...</div>
      )}
      {message && !loading && (
        <div className="settings-status success">{message}</div>
      )}
      {error && !loading && (
        <div className="settings-status error">{error}</div>
      )}
      
      <div className="settings-section">
        <h2 className="section-title">AI Engine Configuration (Cognition Engine)</h2>
        
        <div className="setting-item">
          <label className="setting-label">Confidence Threshold for Auto-Logging:</label>
          <div className="slider-container">
            <input
              type="range"
              min="0"
              max="100"
              value={confidenceThreshold}
              onChange={(e) => setConfidenceThreshold(Number(e.target.value))}
              className="slider"
              disabled={loading || saving}
            />
            <div className="slider-value">{confidenceThreshold}%</div>
            <div className="slider-track">
              <div 
                className="slider-fill" 
                style={{ width: `${confidenceThreshold}%` }}
              />
            </div>
          </div>
        </div>

        <div className="setting-item">
          <label className="setting-label">Vision Model Version:</label>
          <input
            type="text"
            value={visionModel}
            onChange={(e) => setVisionModel(e.target.value)}
            className="text-input"
            disabled={loading || saving}
          />
        </div>
      </div>

      <div className="settings-section">
        <h2 className="section-title">General & Notifications</h2>
        
        <div className="setting-item">
          <label className="setting-label">Enable Weekly Admin Digest Email:</label>
          <div className="toggle-switch">
            <input
              type="checkbox"
              checked={weeklyDigest}
              onChange={(e) => setWeeklyDigest(e.target.checked)}
              id="weekly-digest"
              disabled={loading || saving}
            />
            <label htmlFor="weekly-digest" className={`toggle-label ${weeklyDigest ? 'active' : ''}`}>
              <span className="toggle-slider" />
            </label>
          </div>
        </div>

        <div className="setting-item">
          <label className="setting-label">Maintenance Mode (Users cannot log data)</label>
          <div className="toggle-switch">
            <input
              type="checkbox"
              checked={maintenanceMode}
              onChange={(e) => setMaintenanceMode(e.target.checked)}
              id="maintenance-mode"
              disabled={loading || saving}
            />
            <label htmlFor="maintenance-mode" className={`toggle-label ${maintenanceMode ? 'active' : ''}`}>
              <span className="toggle-slider" />
            </label>
          </div>
        </div>
      </div>

      <button className="save-button" onClick={handleSave} disabled={loading || saving}>
        {saving ? 'Saving...' : 'Save Changes'}
      </button>
    </div>
  );
};

export default AdminSettings;
