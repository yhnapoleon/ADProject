import { useState, useEffect } from 'react';
import { Card, Table, Select, Button, Modal, message, Row, Col, Tag } from 'antd';
import { DeleteOutlined, ExclamationCircleOutlined } from '@ant-design/icons';
import { Record, EmissionType } from '../types/index';
import './Records.module.css';
import mainEatIcon from '../assets/icons/main_eat.svg';
import mainTravelIcon from '../assets/icons/main_travel.svg';
import mainWaterIcon from '../assets/icons/main_water.svg';
import request from '../utils/request';

const Records = () => {
  const [loading, setLoading] = useState(false);
  const [records, setRecords] = useState<Record[]>([]);
  const [filterType, setFilterType] = useState<EmissionType | 'all' | undefined>('all');
  const [filterMonth, setFilterMonth] = useState<string | 'all'>('all');

  const fetchRecords = async () => {
    setLoading(true);
    try {
      // 这里的 params 需要根据后端实际支持的过滤字段调整
      const res: any = await request.get('/api/records', {
        params: {
          type: filterType === 'all' ? undefined : filterType,
          month: filterMonth === 'all' ? undefined : filterMonth,
        }
      });
      // 兼容数组或带 total 的对象
      const list = Array.isArray(res) ? res : res.items || [];
      setRecords(list);
    } catch (error: any) {
      console.error('Failed to fetch records:', error);
      message.error('Failed to load records');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRecords();
  }, [filterType, filterMonth]);

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

  const handleDelete = (id: string) => {
    Modal.confirm({
      title: 'Delete Record',
      icon: <ExclamationCircleOutlined />,
      content: 'Are you sure you want to delete this record? This action cannot be undone.',
      okText: 'Delete',
      okType: 'danger',
      cancelText: 'Cancel',
      async onOk() {
        try {
          await request.delete(`/api/records/${id}`);
          message.success('Record deleted successfully');
          fetchRecords(); // 刷新列表
        } catch (error: any) {
          console.error('Failed to delete record:', error);
          message.error('Failed to delete record');
        }
      },
    });
  };

  const columns = [
    {
      title: 'Date',
      dataIndex: 'date',
      key: 'date',
      render: (text: string) => new Date(text).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
      }),
      width: '15%',
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
      render: (text: number, record: Record) => `${text} ${record.unit || 'kg CO₂e'}`,
      width: '15%',
    },
    {
      title: 'Description',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
      width: '40%',
    },
    {
      title: 'Action',
      key: 'action',
      render: (_: any, record: Record) => (
        <Button
          type="text"
          danger
          icon={<DeleteOutlined />}
          onClick={() => handleDelete(record.id)}
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

  const monthOptions = [
    { label: 'All Months', value: 'all' },
    { label: 'January 2026', value: '2026-01' },
    { label: 'December 2025', value: '2025-12' },
    { label: 'November 2025', value: '2025-11' },
  ];

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
            <Col xs={24} md={8} style={{ display: 'flex', alignItems: 'flex-end' }}>
              <Button
                type="default"
                block
                size="large"
                onClick={() => {
                  setFilterType('all');
                  setFilterMonth('all');
                }}
              >
                Reset Filters
              </Button>
            </Col>
          </Row>
        </div>

        {/* Table */}
        <div style={{ margin: '24px 0' }}>
          <Table
            columns={columns}
            loading={loading}
            dataSource={records.map((record) => ({
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
        {!loading && records.length > 0 && (
          <div style={{ marginTop: '20px', padding: '16px', background: '#f8f5fb', borderRadius: '8px', borderLeft: '4px solid #674fa3', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div style={{ fontSize: '14px', color: '#666' }}>
              Total Emissions: <span style={{ fontWeight: '700', color: '#674fa3', fontSize: '16px' }}>
                {records.reduce((sum, r) => sum + (r.amount || 0), 0).toFixed(2)} kg CO₂e
              </span>
            </div>
          </div>
        )}

        {!loading && records.length === 0 && (
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
