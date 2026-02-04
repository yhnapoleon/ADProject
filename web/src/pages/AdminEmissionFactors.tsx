import React, { useEffect, useRef, useState } from 'react';
import request from '../utils/request';
import './AdminEmissionFactors.css';

interface EmissionFactor {
  id: string;
  category: string;
  itemName: string;
  factor: number;
  unit: string;
  source: string;
  lastUpdated: string;
  status?: string; // API may return; not displayed or edited
}

const AdminEmissionFactors: React.FC = () => {
  const [factors, setFactors] = useState<EmissionFactor[]>([]);
  const [totalFactors, setTotalFactors] = useState<number>(0);
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
    lastUpdated: new Date().toISOString().split('T')[0],
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [categories, setCategories] = useState<string[]>(['All']);
  const [isEditMode, setIsEditMode] = useState(false);
  const [editingFactors, setEditingFactors] = useState<Record<string, number>>({});
  const [saving, setSaving] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  /** 全量因子列表缓存，用于 Add/Import 时的重复校验（避免分页漏检） */
  const allFactorsRef = useRef<EmissionFactor[] | null>(null);

  // 从排放因子数据中提取唯一的分类
  const extractCategories = (factors: EmissionFactor[]): string[] => {
    const uniqueCategories = new Set<string>();
    factors.forEach(factor => {
      if (factor.category) {
        uniqueCategories.add(factor.category);
      }
    });
    return ['All', ...Array.from(uniqueCategories).sort()];
  };

  const fetchFactors = async () => {
    setLoading(true);
    setError(null);
    try {
      const res: any = await request.get('/admin/emission-factors', {
        params: {
          q: searchTerm || undefined,
          category: selectedCategory === 'All' ? undefined : selectedCategory,
          page: 1,
          pageSize: 50,
        },
      });

      const raw = res as any;
      const items: EmissionFactor[] = Array.isArray(raw)
        ? raw
        : raw?.items || raw?.data || raw || [];
      const total = typeof raw === 'object' && !Array.isArray(raw) ? (raw?.total || items.length) : items.length;
      setFactors(items);
      setTotalFactors(total);
      // 从数据中提取分类列表
      setCategories(extractCategories(items));
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

  // 初始化时加载所有分类
  useEffect(() => {
    const loadCategories = async () => {
      try {
        // 加载所有数据（不筛选）以获取所有分类
        const res: any = await request.get('/admin/emission-factors', {
          params: {
            page: 1,
            pageSize: 1000, // 获取足够多的数据以提取所有分类
          },
        });
        const raw = res as any;
        const items: EmissionFactor[] = Array.isArray(raw)
          ? raw
          : raw?.items || raw?.data || [];
        setCategories(extractCategories(items));
      } catch (e) {
        console.error('Failed to load categories:', e);
        // 如果失败，使用默认分类
        setCategories(['All', 'Food', 'Transport', 'Utility']);
      }
    };
    loadCategories();
  }, []);

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

  /** 拉取全量因子列表（分页请求直到取完），用于重复校验；结果缓存在 allFactorsRef */
  const ensureAllFactorsLoaded = async (): Promise<EmissionFactor[]> => {
    if (allFactorsRef.current) return allFactorsRef.current;
    const pageSize = 100;
    let page = 1;
    const all: EmissionFactor[] = [];
    let total = 0;
    do {
      const res: any = await request.get('/admin/emission-factors', {
        params: { page, pageSize },
      });
      const raw = res as any;
      const items: EmissionFactor[] = raw?.items ?? raw?.data ?? (Array.isArray(raw) ? raw : []);
      total = typeof raw === 'object' && !Array.isArray(raw) ? raw?.total ?? items.length : items.length;
      all.push(...items);
      if (items.length < pageSize || all.length >= total) break;
      page += 1;
    } while (true);
    allFactorsRef.current = all;
    return all;
  };

  /** 检查 (itemName, category) 是否在列表中已存在；list 不传则用当前页 factors */
  const isDuplicateItemNameCategory = (
    itemName: string,
    category: string,
    excludeId?: string,
    list?: EmissionFactor[]
  ): boolean => {
    const n = (itemName || '').trim().toLowerCase();
    const c = (category || '').trim();
    const target = list ?? factors;
    return target.some(
      (f) =>
        f.id !== excludeId &&
        (f.itemName || '').trim().toLowerCase() === n &&
        (f.category || '').trim() === c
    );
  };

  const handleAddNew = () => {
    setShowAddModal(true);
  };

  const handleBulkImport = () => {
    setShowBulkImportModal(true);
  };

  const handleEditModeToggle = () => {
    if (!isEditMode) {
      // Enter edit mode, save current factors
      const factorsMap: Record<string, number> = {};
      filteredFactors.forEach(factor => {
        factorsMap[factor.id] = factor.factor;
      });
      setEditingFactors(factorsMap);
    }
    setIsEditMode(!isEditMode);
  };

  const handleFactorChange = (factorId: string, value: number) => {
    setEditingFactors({
      ...editingFactors,
      [factorId]: value
    });
  };

  const handleSave = async () => {
    setSaving(true);
    setError(null);
    try {
      const updatePromises = filteredFactors.map(async (factor) => {
        const newFactorVal = editingFactors[factor.id];
        if (newFactorVal === undefined || newFactorVal === factor.factor) {
          return null;
        }
        const payload: Partial<EmissionFactor> = { factor: newFactorVal };
        try {
          const updated = await request.put(`/admin/emission-factors/${factor.id}`, payload);
          return { id: factor.id, updated };
        } catch (e: any) {
          console.error(`Failed to update factor ${factor.id}:`, e);
          throw new Error(`Failed to update ${factor.itemName}: ${e?.response?.data?.message || e?.message || 'Unknown error'}`);
        }
      });

      const results = await Promise.all(updatePromises);
      const successCount = results.filter(r => r !== null).length;
      
      if (successCount > 0) {
        await fetchFactors();
        setIsEditMode(false);
        setEditingFactors({});
        alert(`Successfully updated ${successCount} emission factor(s).`);
      }
    } catch (e: any) {
      console.error('Failed to save emission factors:', e);
      setError(
        e?.response?.data?.error ||
        e?.response?.data?.message ||
        e?.message ||
        'Failed to save emission factor updates.'
      );
    } finally {
      setSaving(false);
    }
  };

  const handleCancel = () => {
    setIsEditMode(false);
    setEditingFactors({});
  };

  const handleDelete = async (factor: EmissionFactor) => {
    if (!window.confirm(`Are you sure you want to delete "${factor.itemName}" (ID: ${factor.id})? This action cannot be undone.`)) {
      return;
    }
    setDeletingId(factor.id);
    setError(null);
    try {
      await request.delete(`/admin/emission-factors/${factor.id}`);
      allFactorsRef.current = null;
      setFactors(factors.filter((f) => f.id !== factor.id));
      setTotalFactors((prev) => Math.max(0, prev - 1));
    } catch (e: any) {
      console.error('Failed to delete emission factor:', e);
      setError(
        e?.response?.data?.error ||
        e?.response?.data?.message ||
        e?.message ||
        'Failed to delete emission factor.'
      );
    } finally {
      setDeletingId(null);
    }
  };

  const handleSaveNew = async () => {
    if (newFactor.itemName && newFactor.factor !== undefined) {
      const itemName = (newFactor.itemName || '').trim();
      const category = newFactor.category || 'Food';
      try {
        const fullList = await ensureAllFactorsLoaded();
        if (isDuplicateItemNameCategory(itemName, category, undefined, fullList)) {
          alert('Item Name and Category cannot duplicate an existing emission factor. Please use a different name or category.');
          return;
        }
      } catch (e) {
        console.error('Failed to load full list for duplicate check:', e);
        alert('Could not validate duplicates. Please try again.');
        return;
      }
      try {
        const payload = {
          category,
          itemName,
          factor: newFactor.factor,
          unit: newFactor.unit || 'kg CO2/kg',
          source: newFactor.source || '',
          lastUpdated: newFactor.lastUpdated || new Date().toISOString().split('T')[0],
        };

        const created: any = await request.post('/admin/emission-factors', payload);
        const createdFactor = (created as any) ?? payload;
        setFactors([...factors, createdFactor as EmissionFactor]);
        allFactorsRef.current = null;
        setNewFactor({
          id: '',
          category: 'Food',
          itemName: '',
          factor: 0,
          unit: 'kg CO2/kg',
          source: '',
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

  const handleBulkImportSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const fileInput = e.currentTarget.querySelector('input[type="file"]') as HTMLInputElement;
    if (!fileInput?.files?.[0]) return;
    const file = fileInput.files[0];
    try {
      const text = await file.text();
      const items: Array<{ itemName?: string; category?: string }> = JSON.parse(text);
      if (!Array.isArray(items)) {
        alert('File must be a JSON array.');
        return;
      }
      const seen = new Set<string>();
      let fullList: EmissionFactor[];
      try {
        fullList = await ensureAllFactorsLoaded();
      } catch (err) {
        console.error('Failed to load full list for duplicate check:', err);
        alert('Could not validate duplicates. Please try again.');
        return;
      }
      for (let i = 0; i < items.length; i++) {
        const itemName = (items[i].itemName ?? '').trim().toLowerCase();
        const category = (items[i].category ?? '').trim();
        if (!itemName || !category) continue;
        const key = `${itemName}|${category}`;
        if (seen.has(key)) {
          alert(`Duplicate (Item Name + Category) in file: "${items[i].itemName}" / ${category} (row ${i + 1}).`);
          return;
        }
        seen.add(key);
        if (isDuplicateItemNameCategory(items[i].itemName ?? '', category, undefined, fullList)) {
          alert(`Row ${i + 1} duplicates an existing factor: "${items[i].itemName}" / ${category}.`);
          return;
        }
      }
    } catch (err: any) {
      if (err?.message?.includes('JSON')) {
        alert('Invalid JSON file.');
        return;
      }
      throw err;
    }
    const formData = new FormData();
    formData.append('file', file);
    try {
      const res: any = await request.post('/admin/emission-factors/import', formData);
      const importedCount = res?.importedCount ?? res?.imported ?? 0;
      allFactorsRef.current = null;
      alert(`Imported ${importedCount} factors successfully.`);
      setShowBulkImportModal(false);
      fetchFactors();
    } catch (err: any) {
      console.error('Failed to import emission factors:', err);
      alert(
        err?.response?.data?.error ||
          err?.response?.data?.message ||
          err?.message ||
          'Error importing file. Please check the file format.'
      );
    }
  };

  return (
    <div className="emission-factors">
      <div className="page-header">
        <h1 className="page-title">Emission Factor Database {totalFactors > 0 && `(${totalFactors} total)`}</h1>
        <div className="action-buttons">
          {isEditMode ? (
            <>
              <button className="btn-primary" onClick={handleSave} disabled={saving}>
                {saving ? 'Saving...' : '✓ Save'}
              </button>
              <button className="btn-secondary" onClick={handleCancel}>
                ✕ Cancel
              </button>
            </>
          ) : (
            <>
              <button className="btn-primary" onClick={handleEditModeToggle}>Edit</button>
              <button className="btn-primary" onClick={handleAddNew}>+ Add New</button>
              <button className="btn-secondary" onClick={handleBulkImport}>Bulk Import</button>
            </>
          )}
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
                  <label>Select File (JSON)</label>
                  <input type="file" accept=".json" required />
                  <p className="form-hint">Upload a JSON file containing emission factor data.</p>
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
          <div className="table-scroll-wrap">
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
                <th>Last Updated</th>
                {isEditMode && <th>Actions</th>}
              </tr>
            </thead>
            <tbody>
              {filteredFactors.length > 0 ? (
                filteredFactors.map((factor) => (
                  <tr key={factor.id}>
                    <td>{factor.id}</td>
                    <td>{factor.category}</td>
                    <td>{factor.itemName}</td>
                    <td className="factor-cell">
                      {isEditMode ? (
                        <input
                          type="number"
                          step="0.001"
                          value={editingFactors[factor.id] !== undefined ? editingFactors[factor.id] : factor.factor}
                          onChange={(e) => handleFactorChange(factor.id, Number(e.target.value))}
                          className="factor-input"
                          min="0"
                        />
                      ) : (
                        <span className="factor-value">{factor.factor}</span>
                      )}
                    </td>
                    <td>{factor.unit}</td>
                    <td>{factor.source}</td>
                    <td>{factor.lastUpdated}</td>
                    {isEditMode && (
                      <td className="actions-cell">
                        <button
                          type="button"
                          className="btn-delete"
                          onClick={() => handleDelete(factor)}
                          disabled={deletingId === factor.id}
                          title="Delete this emission factor"
                        >
                          {deletingId === factor.id ? 'Deleting...' : '🗑 Delete'}
                        </button>
                      </td>
                    )}
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={isEditMode ? 8 : 7} style={{ textAlign: 'center', padding: '20px' }}>
                    No emission factors found.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
          </div>
        )}
      </div>
    </div>
  );
};

export default AdminEmissionFactors;
