import React, { useState } from 'react';
import './AdminSettings.css';

const AdminSettings: React.FC = () => {
  const [confidenceThreshold, setConfidenceThreshold] = useState(85);
  const [visionModel, setVisionModel] = useState('v2.1.0 (Stable)');
  const [weeklyDigest, setWeeklyDigest] = useState(true);
  const [maintenanceMode, setMaintenanceMode] = useState(false);

  const handleSave = () => {
    // Handle save logic here
    alert('Settings saved!');
  };

  return (
    <div className="settings">
      <h1 className="page-title">System Settings</h1>
      
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
            />
            <label htmlFor="maintenance-mode" className={`toggle-label ${maintenanceMode ? 'active' : ''}`}>
              <span className="toggle-slider" />
            </label>
          </div>
        </div>
      </div>

      <button className="save-button" onClick={handleSave}>
        Save Changes
      </button>
    </div>
  );
};

export default AdminSettings;
