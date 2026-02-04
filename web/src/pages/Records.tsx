import { useState, useEffect, useMemo } from 'react';
import { Card, Table, Select, Button, Modal, message, Row, Col, Tag, Space } from 'antd'; // 引入 Space
import { DeleteOutlined, ExclamationCircleOutlined, ClearOutlined } from '@ant-design/icons'; // 引入 ClearOutlined
import { Record, EmissionType } from '../types/index';
import './Records.module.css';
import mainEatIcon from '../assets/icons/main_eat.svg';
import mainTravelIcon from '../assets/icons/main_travel.svg';
import mainWaterIcon from '../assets/icons/main_water.svg';
import request from '../utils/request';

/** 内部记录类型，包含原始 ID、来源类型和 API 返回的 notes */
interface InternalRecord extends Record {
  _source: 'food' | 'travel' | 'utility';
  _originalId: number;
  notes?: string;
}

const Records = () => {
  const [loading, setLoading] = useState(false);
  const [records, setRecords] = useState<InternalRecord[]>([]);
  const [filterType, setFilterType] = useState<EmissionType | 'all' | undefined>('all');
  const [filterMonth, setFilterMonth] = useState<string | 'all'>('all');
  
  // 1. 新增：存储选中的行 ID
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);

  const fetchRecords = async () => {
    setLoading(true);
    try {
      const [foodRes, travelRes, utilityRes] = await Promise.all([
        request.get('/api/FoodRecords/my-records').catch(() => ({ items: [] })),
        request.get('/api/Travel/my-travels').catch(() => ({ items: [] })),
        request.get('/api/UtilityBill/my-bills').catch(() => ({ items: [] })),
      ]);

      const foodList = (Array.isArray(foodRes) ? foodRes : foodRes?.items || []).map((item: any) => ({
        id: `food_${item.id}`,
        _source: 'food' as const,
        _originalId: item.id,
        date: item.createdAt || item.date,
        type: 'Food' as EmissionType,
        amount: item.emission ?? item.totalEmission ?? item.carbonEmission ?? 0,
        unit: 'kg CO₂e',
        description: item.foodName || item.name || item.detectedLabel || 'Food record',
        notes: item.notes ?? item.note ?? '',
      }));

      const travelList = (Array.isArray(travelRes) ? travelRes : travelRes?.items || []).map((item: any) => ({
        id: `travel_${item.id}`,
        _source: 'travel' as const,
        _originalId: item.id,
        date: item.createdAt || item.date,
        type: 'Transport' as EmissionType,
        amount: item.carbonEmission ?? 0,
        unit: 'kg CO₂e',
        description: `${item.originAddress || ''} → ${item.destinationAddress || ''} (${item.transportModeName || item.transportMode || ''})`,
        notes: item.notes ?? item.Notes ?? '',
      }));

      const utilityList = (Array.isArray(utilityRes) ? utilityRes : utilityRes?.items || []).map((item: any) => ({
        id: `utility_${item.id}`,
        _source: 'utility' as const,
        _originalId: item.id,
        date: item.billPeriodEnd || item.createdAt || item.date,
        type: 'Utilities' as EmissionType,
        amount: item.totalCarbonEmission ?? 0,
        unit: 'kg CO₂e',
        description: `Utility bill ${item.yearMonth || ''} (Elec ${item.electricityUsage ?? 0}kWh / Water ${item.waterUsage ?? 0}m³ / Gas ${item.gasUsage ?? 0})`,
        notes: item.notes ?? item.note ?? '',
      }));

      const allRecords: InternalRecord[] = [...foodList, ...travelList, ...utilityList];
      allRecords.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
      setRecords(allRecords);
    } catch (error: any) {
      console.error('Failed to fetch records:', error);
      message.error('Failed to load records');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRecords();
  }, []);

  const filteredRecords = useMemo(() => {
    let list = records;
    if (filterType && filterType !== 'all') {
      list = list.filter((r) => r.type === filterType);
    }
    if (filterMonth && filterMonth !== 'all') {
      list = list.filter((r) => {
        const d = new Date(r.date);
        const ym = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`;
        return ym === filterMonth;
      });
    }
    return list;
  }, [records, filterType, filterMonth]);

  const getTypeColor = (type: EmissionType) => {
    const colorMap: { [key in EmissionType]: string } = {
      'Food': 'green',
      'Transport': 'blue',
      'Utilities': 'orange',
    };
    return colorMap[type] || 'default';
  };

  const getTypeIcon = (type: EmissionType) => {
    const iconMap: { [key in EmissionType]: string } = {
      'Food': mainEatIcon,
      'Transport': mainTravelIcon,
      'Utilities': mainWaterIcon,
    };
    return iconMap[type];
  };

  const handleDelete = (record: InternalRecord) => {
    Modal.confirm({
      title: 'Delete Record',
      icon: <ExclamationCircleOutlined />,
      content: 'Are you sure you want to delete this record? This action cannot be undone.',
      okText: 'Delete',
      okType: 'danger',
      cancelText: 'Cancel',
      async onOk() {
        try {
          const deleteUrl = {
            food: `/api/FoodRecords/${record._originalId}`,
            travel: `/api/Travel/${record._originalId}`,
            utility: `/api/UtilityBill/${record._originalId}`,
          }[record._source];

          await request.delete(deleteUrl);
          message.success('Record deleted successfully');
          
          // 如果删除的行在选中列表中，将其移除
          if (selectedRowKeys.includes(record.id)) {
            setSelectedRowKeys(prev => prev.filter(k => k !== record.id));
          }
          
          fetchRecords();
        } catch (error: any) {
          console.error('Failed to delete record:', error);
          message.error('Failed to delete record');
        }
      },
    });
  };

  // 2. 新增：批量删除逻辑
  const handleBatchDelete = () => {
    if (selectedRowKeys.length === 0) return;

    Modal.confirm({
      title: `Delete ${selectedRowKeys.length} records?`,
      icon: <ExclamationCircleOutlined />,
      content: 'Are you sure you want to delete these records? This action cannot be undone.',
      okText: 'Batch Delete',
      okType: 'danger',
      cancelText: 'Cancel',
      async onOk() {
        try {
          setLoading(true);
          
          // 1. 找到所有被选中的完整记录
          const selectedItems = records.filter(r => selectedRowKeys.includes(r.id));
          
          // 2. 按照后端 Swagger 要求，将 ID 分类放入三个数组
          const activityLogIds: number[] = []; // 对应 food
          const travelLogIds: number[] = [];   // 对应 travel
          const utilityBillIds: number[] = []; // 对应 utility

          selectedItems.forEach(item => {
            if (item._source === 'food') {
              activityLogIds.push(item._originalId);
            } else if (item._source === 'travel') {
              travelLogIds.push(item._originalId);
            } else if (item._source === 'utility') {
              utilityBillIds.push(item._originalId);
            }
          });

          // 3. 构造 Payload (完全符合 Swagger 定义)
          const payload = {
            activityLogIds,
            travelLogIds,
            utilityBillIds
          };

          console.log('Sending Batch Delete Payload:', payload);

          // 4. 发送请求
          await request.post('/api/carbon-emission/batch-delete', payload);
          
          message.success('Selected records deleted successfully');
          setSelectedRowKeys([]); 
          fetchRecords(); 
        } catch (error: any) {
          console.error('Batch delete failed:', error);
          const errorMsg = error.response?.data?.title || error.message || 'Unknown error';
          message.error(`Batch delete failed: ${errorMsg}`);
          setLoading(false);
        }
      },
    });
  };

  // 3. 新增：表格选中配置
  const onSelectChange = (newSelectedRowKeys: React.Key[]) => {
    setSelectedRowKeys(newSelectedRowKeys);
  };

  const rowSelection = {
    selectedRowKeys,
    onChange: onSelectChange,
  };

  const columns = [
    {
      title: 'Date',
      dataIndex: 'date',
      key: 'date',
      render: (text: string, record: InternalRecord) =>
        record._source === 'utility'
          ? new Date(text).toLocaleDateString('en-US', { year: 'numeric', month: 'short' })
          : new Date(text).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' }),
      width: '10%',
    },
    {
      title: 'Type',
      dataIndex: 'type',
      key: 'type',
      align: 'center' as const,
      render: (text: EmissionType) => (
        <Tag color={getTypeColor(text)} style={{ marginRight: 0, display: 'flex', alignItems: 'center', gap: '6px', justifyContent: 'center' }}>
          {getTypeIcon(text) && <img src={getTypeIcon(text)} alt={text} style={{ width: '16px', height: '16px', filter: 'brightness(0) saturate(100%) invert(40%)' }} />}
          {text}
        </Tag>
      ),
      width: '15%',
    },
    {
      title: 'Amount',
      dataIndex: 'amount',
      key: 'amount',
      render: (text: number, record: InternalRecord) => `${Number(text).toFixed(2)} ${record.unit || 'kg CO₂e'}`,
      width: '15%',
    },
    {
      title: 'Description',
      dataIndex: 'description',
      key: 'description',
      ellipsis: false,
      render: (_: unknown, record: InternalRecord) => (
        <span style={{ whiteSpace: 'normal', wordBreak: 'break-word', display: 'block' }}>
          {record.description || '—'}
        </span>
      ),
      width: '32%',
    },
    {
      title: 'Notes',
      dataIndex: 'notes',
      key: 'notes',
      ellipsis: true,
      width: '20%',
      render: (_: unknown, record: InternalRecord) =>
        record.notes?.trim() ? (
          record.notes
        ) : (
          <span style={{ color: '#bfbfbf' }}>no notes</span>
        ),
    },
    {
      title: 'Action',
      key: 'action',
      render: (_: any, record: InternalRecord) => (
        <Button
          type="text"
          danger
          icon={<DeleteOutlined />}
          onClick={(e) => {
            e.stopPropagation(); // 阻止冒泡，避免触发 onRow 点击选中
            handleDelete(record);
          }}
          size="small"
        >
          Delete
        </Button>
      ),
      width: '15%',
    },
  ];

  const typeOptions = [
    { label: 'All Types', value: 'all' },
    { label: 'Food', value: 'Food' as EmissionType },
    { label: 'Transport', value: 'Transport' as EmissionType },
    { label: 'Utilities', value: 'Utilities' as EmissionType },
  ];

  const monthOptions = useMemo(() => {
    const options = [{ label: 'All Months', value: 'all' }];
    const now = new Date();
    for (let i = 0; i < 12; i++) {
      const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
      const ym = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`;
      const label = d.toLocaleDateString('en-US', { year: 'numeric', month: 'short' });
      options.push({ label, value: ym });
    }
    return options;
  }, []);

  return (
    <div style={{ width: '100%' }}>
      <Card>
        {/* Filters */}
        <div style={{ marginBottom: '24px', paddingBottom: '20px', borderBottom: '1px solid #f0f0f0' }}>
          <div style={{ marginBottom: '20px' }}>
            <div style={{ fontSize: '18px', fontWeight: '600', marginBottom: '16px' }}>
              Carbon Emission Records
            </div>
          </div>

          <Row gutter={16} style={{ marginBottom: '24px' }}>
            <Col xs={24} sm={12} md={8}>
              <div style={{ marginBottom: '8px', fontSize: '14px', fontWeight: '500' }}>
                Filter by Type
              </div>
              <Select
                value={filterType}
                onChange={(value) => setFilterType(value as EmissionType | 'all' | undefined)}
                options={typeOptions}
                style={{ width: '100%' }}
                size="large"
              />
            </Col>
            <Col xs={24} sm={12} md={8}>
              <div style={{ marginBottom: '8px', fontSize: '14px', fontWeight: '500' }}>
                Filter by Month
              </div>
              <Select
                value={filterMonth}
                onChange={setFilterMonth}
                options={monthOptions}
                style={{ width: '100%' }}
                size="large"
              />
            </Col>
            {/* 4. 修改：布局 Col，加入批量删除按钮 */}
            <Col xs={24} md={8} style={{ display: 'flex', alignItems: 'flex-end' }}>
              <Space style={{ width: '100%' }}>
                <Button
                  type="default"
                  size="large"
                  onClick={() => {
                    setFilterType('all');
                    setFilterMonth('all');
                  }}
                  style={{ flex: 1 }}
                >
                  Reset Filters
                </Button>
                
                {selectedRowKeys.length > 0 && (
                  <Button
                    type="primary"
                    danger
                    size="large"
                    icon={<DeleteOutlined />}
                    onClick={handleBatchDelete}
                  >
                    Batch Delete ({selectedRowKeys.length})
                  </Button>
                )}
              </Space>
            </Col>
          </Row>
        </div>

        {/* Table */}
        <div style={{ margin: '24px 0' }}>
          <Table
            columns={columns}
            loading={loading}
            // 5. 新增：rowSelection
            rowSelection={rowSelection}
            // 6. 新增：onRow 实现点击行选中/取消选中
            onRow={(record) => ({
              onClick: () => {
                const selected = selectedRowKeys.includes(record.id);
                if (selected) {
                  setSelectedRowKeys(prev => prev.filter(k => k !== record.id));
                } else {
                  setSelectedRowKeys(prev => [...prev, record.id]);
                }
              },
            })}
            dataSource={filteredRecords.map((record) => ({
              ...record,
              key: record.id,
            }))}
            pagination={{
              position: ['bottomRight'],
              defaultPageSize: 10,
              showSizeChanger: true,
              pageSizeOptions: ['5', '10', '20', '50'],
              showTotal: (total) => `Total ${total} records`,
            }}
            scroll={{ x: 800 }}
          />
        </div>

        {/* Summary */}
        {!loading && filteredRecords.length > 0 && (
          <div style={{ marginTop: '20px', padding: '16px', background: '#f8f5fb', borderRadius: '8px', borderLeft: '4px solid #674fa3', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div style={{ fontSize: '14px', color: '#666' }}>
              Total Emissions ({filteredRecords.length} records): <span style={{ fontWeight: '700', color: '#674fa3', fontSize: '16px' }}>
                {filteredRecords.reduce((sum, r) => sum + (r.amount || 0), 0).toFixed(2)} kg CO₂e
              </span>
            </div>
          </div>
        )}

        {!loading && filteredRecords.length === 0 && (
          <div style={{ textAlign: 'center', padding: '60px 20px', color: '#999' }}>
            <div style={{ fontSize: '16px' }}>
              No records found. Adjust your filters to see data.
            </div>
          </div>
        )}
      </Card>
    </div>
  );
};

export default Records;