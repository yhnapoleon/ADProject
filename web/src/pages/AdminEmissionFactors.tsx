import React, { useEffect, useState } from 'react';
import request from '../utils/request';
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
  const [factors, setFactors] = useState<EmissionFactor[]>([]);
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
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const categories = ['All', 'Food', 'Transport', 'Energy', 'Goods'];

  const fetchFactors = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await request.get('/admin/emission-factors', {
        params: {
          q: searchTerm || undefined,
          category: selectedCategory === 'All' ? undefined : selectedCategory,
          page: 1,
          pageSize: 50,
        },
      });

      const items: EmissionFactor[] = Array.isArray(res)
        ? res
        : res?.items || res?.data || [];
      setFactors(items);
    } catch (e: any) {
      console.error('Failed to load emission factors:', e);
      setError(
        e?.response?.data?.error ||
          e?.response?.data?.message ||
          e?.message ||
          'Failed to load emission factors.'
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchFactors();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedCategory]);

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

  const handleSaveNew = async () => {
    if (newFactor.id && newFactor.itemName && newFactor.factor !== undefined) {
      try {
        const payload: EmissionFactor = {
          id: newFactor.id,
          category: newFactor.category || 'Food',
          itemName: newFactor.itemName,
          factor: newFactor.factor,
          unit: newFactor.unit || 'kg CO2/kg',
          source: newFactor.source || '',
          status: newFactor.status || 'Draft',
          lastUpdated: newFactor.lastUpdated || new Date().toISOString().split('T')[0],
        };

        const created = await request.post('/admin/emission-factors', payload);
        setFactors([...factors, (created as EmissionFactor) || payload]);
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
      } catch (e: any) {
        console.error('Failed to add emission factor:', e);
        alert(
          e?.response?.data?.error ||
            e?.response?.data?.message ||
            e?.message ||
            'Failed to add emission factor.'
        );
      }
    }
  };

  const handleBulkImportSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const fileInput = e.currentTarget.querySelector('input[type="file"]') as HTMLInputElement;
    if (fileInput && fileInput.files && fileInput.files[0]) {
      const file = fileInput.files[0];
      const formData = new FormData();
      formData.append('file', file);

      request
        .post('/admin/emission-factors/import', formData)
        .then((res: any) => {
          const importedCount = res?.importedCount ?? res?.imported ?? 0;
          alert(`Imported ${importedCount} factors successfully.`);
          setShowBulkImportModal(false);
          fetchFactors();
        })
        .catch((err: any) => {
          console.error('Failed to import emission factors:', err);
          alert(
            err?.response?.data?.error ||
              err?.response?.data?.message ||
              err?.message ||
              'Error importing file. Please check the file format.'
          );
        });
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
          onBlur={fetchFactors}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              fetchFactors();
            }
          }}
          className="search-input"
        />
      </div>

      {error && (
        <div className="factors-error">
          {error}
        </div>
      )}

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
        {loading ? (
          <div style={{ padding: '20px', textAlign: 'center' }}>Loading emission factors...</div>
        ) : (
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
              {filteredFactors.length > 0 ? (
                filteredFactors.map((factor) => (
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
                ))
              ) : (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', padding: '20px' }}>
                    No emission factors found.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};

export default AdminEmissionFactors;
