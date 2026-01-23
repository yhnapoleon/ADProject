import React, { useState } from 'react';
import './AdminEmissionFactors.css';

interface EmissionFactor {
  id: string;
  category: string;
  itemName: string;
  factor: number;
  unit: string;
  source: string;
  status: string;
  lastUpdated: string;
}

const AdminEmissionFactors: React.FC = () => {
  const [factors, setFactors] = useState<EmissionFactor[]>([
    { id: 'EF-001', category: 'Food', itemName: 'Beef (High Impact)', factor: 27.0, unit: 'kg CO2/kg', source: 'IPCC 2023', status: 'Published', lastUpdated: '2024-05-20' },
    { id: 'EF-004', category: 'Food', itemName: 'Chicken Breast', factor: 6.9, unit: 'kg CO2/kg', source: 'OurWorldInData', status: 'Published', lastUpdated: '2024-05-15' },
    { id: 'EF-008', category: 'Transport', itemName: 'Gasoline Car (Avg)', factor: 0.192, unit: 'kg CO2/km', source: 'EPA', status: 'Published', lastUpdated: '2024-04-10' },
    { id: 'EF-012', category: 'Energy', itemName: 'Grid Electricity (US)', factor: 0.385, unit: 'kg CO2/kWh', source: 'EIA 2022', status: 'Review Pending', lastUpdated: '2024-05-21' },
    { id: 'EF-045', category: 'Goods', itemName: 'Cotton T-Shirt', factor: 10.5, unit: 'kg CO2/unit', source: 'Mfg Data', status: 'Draft', lastUpdated: '2024-05-21' },
  ]);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('All');
  const [showCategoryFilter, setShowCategoryFilter] = useState(false);
  const [showAddModal, setShowAddModal] = useState(false);
  const [showBulkImportModal, setShowBulkImportModal] = useState(false);
  const [newFactor, setNewFactor] = useState<Partial<EmissionFactor>>({
    id: '',
    category: 'Food',
    itemName: '',
    factor: 0,
    unit: 'kg CO2/kg',
    source: '',
    status: 'Draft',
    lastUpdated: new Date().toISOString().split('T')[0],
  });

  const categories = ['All', 'Food', 'Transport', 'Energy', 'Goods'];

  const filteredFactors = factors.filter(
    (factor) => {
      const matchesSearch = 
        factor.id.toLowerCase().includes(searchTerm.toLowerCase()) ||
        factor.itemName.toLowerCase().includes(searchTerm.toLowerCase());
      const matchesCategory = selectedCategory === 'All' || factor.category === selectedCategory;
      return matchesSearch && matchesCategory;
    }
  );

  const getStatusClass = (status: string) => {
    if (status === 'Published') return 'published';
    if (status === 'Review Pending') return 'review-pending';
    if (status === 'Draft') return 'draft';
    return '';
  };

  const handleAddNew = () => {
    setShowAddModal(true);
  };

  const handleBulkImport = () => {
    setShowBulkImportModal(true);
  };

  const handleSaveNew = () => {
    if (newFactor.id && newFactor.itemName && newFactor.factor !== undefined) {
      setFactors([...factors, newFactor as EmissionFactor]);
      setNewFactor({
        id: '',
        category: 'Food',
        itemName: '',
        factor: 0,
        unit: 'kg CO2/kg',
        source: '',
        status: 'Draft',
        lastUpdated: new Date().toISOString().split('T')[0],
      });
      setShowAddModal(false);
      // Add API call here to save to backend
      console.log('Add new factor:', newFactor);
    }
  };

  const handleBulkImportSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const fileInput = e.currentTarget.querySelector('input[type="file"]') as HTMLInputElement;
    if (fileInput && fileInput.files && fileInput.files[0]) {
      const file = fileInput.files[0];
      const reader = new FileReader();
      reader.onload = (event) => {
        try {
          const text = event.target?.result as string;
          // Parse CSV or JSON file
          // For demo, we'll just show an alert
          alert(`File "${file.name}" imported successfully! (This is a demo - implement actual parsing logic)`);
          setShowBulkImportModal(false);
          // Add API call here to bulk import to backend
        } catch (error) {
          alert('Error importing file. Please check the file format.');
        }
      };
      reader.readAsText(file);
    }
  };

  return (
    <div className="emission-factors">
      <div className="page-header">
        <h1 className="page-title">Emission Factor Database</h1>
        <div className="action-buttons">
          <button className="btn-primary" onClick={handleAddNew}>+ Add New</button>
          <button className="btn-secondary" onClick={handleBulkImport}>Bulk Import</button>
        </div>
      </div>

      <div className="search-container">
        <input
          type="text"
          placeholder="Search by ID or Item Name..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="search-input"
        />
      </div>

      {showAddModal && (
        <div className="modal-overlay" onClick={() => setShowAddModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Add New Emission Factor</h2>
              <button className="modal-close" onClick={() => setShowAddModal(false)}>×</button>
            </div>
            <div className="modal-body">
              <div className="form-group">
                <label>ID *</label>
                <input
                  type="text"
                  value={newFactor.id}
                  onChange={(e) => setNewFactor({ ...newFactor, id: e.target.value })}
                  placeholder="e.g., EF-046"
                />
              </div>
              <div className="form-group">
                <label>Category *</label>
                <select
                  value={newFactor.category}
                  onChange={(e) => setNewFactor({ ...newFactor, category: e.target.value })}
                >
                  <option value="Food">Food</option>
                  <option value="Transport">Transport</option>
                  <option value="Energy">Energy</option>
                  <option value="Goods">Goods</option>
                </select>
              </div>
              <div className="form-group">
                <label>Item Name *</label>
                <input
                  type="text"
                  value={newFactor.itemName}
                  onChange={(e) => setNewFactor({ ...newFactor, itemName: e.target.value })}
                  placeholder="e.g., Pork (Medium Impact)"
                />
              </div>
              <div className="form-group">
                <label>Factor *</label>
                <input
                  type="number"
                  step="0.001"
                  value={newFactor.factor}
                  onChange={(e) => setNewFactor({ ...newFactor, factor: Number(e.target.value) })}
                  placeholder="e.g., 12.5"
                />
              </div>
              <div className="form-group">
                <label>Unit *</label>
                <input
                  type="text"
                  value={newFactor.unit}
                  onChange={(e) => setNewFactor({ ...newFactor, unit: e.target.value })}
                  placeholder="e.g., kg CO2/kg"
                />
              </div>
              <div className="form-group">
                <label>Source/Ref</label>
                <input
                  type="text"
                  value={newFactor.source}
                  onChange={(e) => setNewFactor({ ...newFactor, source: e.target.value })}
                  placeholder="e.g., IPCC 2023"
                />
              </div>
              <div className="form-group">
                <label>Status</label>
                <select
                  value={newFactor.status}
                  onChange={(e) => setNewFactor({ ...newFactor, status: e.target.value })}
                >
                  <option value="Draft">Draft</option>
                  <option value="Review Pending">Review Pending</option>
                  <option value="Published">Published</option>
                </select>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn-secondary" onClick={() => setShowAddModal(false)}>Cancel</button>
              <button className="btn-primary" onClick={handleSaveNew}>Save</button>
            </div>
          </div>
        </div>
      )}

      {showCategoryFilter && (
        <div
          className="filter-overlay"
          onClick={() => setShowCategoryFilter(false)}
        />
      )}

      {showBulkImportModal && (
        <div className="modal-overlay" onClick={() => setShowBulkImportModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Bulk Import Emission Factors</h2>
              <button className="modal-close" onClick={() => setShowBulkImportModal(false)}>×</button>
            </div>
            <div className="modal-body">
              <form onSubmit={handleBulkImportSubmit}>
                <div className="form-group">
                  <label>Select File (CSV or JSON)</label>
                  <input type="file" accept=".csv,.json" required />
                  <p className="form-hint">Upload a CSV or JSON file containing emission factor data.</p>
                </div>
                <div className="modal-footer">
                  <button type="button" className="btn-secondary" onClick={() => setShowBulkImportModal(false)}>Cancel</button>
                  <button type="submit" className="btn-primary">Import</button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      <div className="table-card">
        <table className="data-table">
          <thead>
            <tr>
              <th>ID</th>
              <th className="category-header">
                <div className="category-header-content">
                  <span>Category</span>
                  <div className="category-filter-wrapper">
                    <button
                      className="category-filter-btn"
                      onClick={() => setShowCategoryFilter(!showCategoryFilter)}
                      title="Filter by Category"
                    >
                      🔽
                    </button>
                    {showCategoryFilter && (
                      <div className="category-filter-dropdown" onClick={(e) => e.stopPropagation()}>
                        {categories.map((category) => (
                          <div
                            key={category}
                            className={`category-filter-option ${selectedCategory === category ? 'active' : ''}`}
                            onClick={() => {
                              setSelectedCategory(category);
                              setShowCategoryFilter(false);
                            }}
                          >
                            {category === 'All' ? 'All Categories' : category}
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              </th>
              <th>Item Name</th>
              <th>Factor</th>
              <th>Unit</th>
              <th>Source/Ref</th>
              <th>Status</th>
              <th>Last Updated</th>
            </tr>
          </thead>
          <tbody>
            {filteredFactors.map((factor) => (
              <tr key={factor.id}>
                <td>{factor.id}</td>
                <td>{factor.category}</td>
                <td>{factor.itemName}</td>
                <td>{factor.factor}</td>
                <td>{factor.unit}</td>
                <td>{factor.source}</td>
                <td>
                  <span className={`status-badge ${getStatusClass(factor.status)}`}>
                    {factor.status}
                  </span>
                </td>
                <td>{factor.lastUpdated}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default AdminEmissionFactors;
