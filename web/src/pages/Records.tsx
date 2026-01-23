import { useState } from 'react';
import { Card, Table, Select, Button, Modal, message, Row, Col, Tag } from 'antd';
import { DeleteOutlined, ExclamationCircleOutlined } from '@ant-design/icons';
import { Record, EmissionType } from '../types/index';
import './Records.module.css';

const Records = () => {
  const [filterType, setFilterType] = useState<EmissionType | undefined>();
  const [filterMonth, setFilterMonth] = useState<string | undefined>();

  // Mock data
  const mockRecords: Record[] = [
    {
      id: '1',
      date: '2026-01-23',
      type: 'Food',
      amount: 2.5,
      unit: 'kg CO₂e',
      description: 'Beef meal at restaurant',
    },
    {
      id: '2',
      date: '2026-01-22',
      type: 'Transport',
      amount: 1.8,
      unit: 'kg CO₂e',
      description: 'Drive car to office (25 km)',
    },
    {
      id: '3',
      date: '2026-01-21',
      type: 'Utilities',
      amount: 0.5,
      unit: 'kg CO₂e',
      description: 'Electricity usage',
    },
    {
      id: '4',
      date: '2026-01-20',
      type: 'Food',
      amount: 1.2,
      unit: 'kg CO₂e',
      description: 'Chicken pasta',
    },
    {
      id: '5',
      date: '2026-01-19',
      type: 'Transport',
      amount: 0.9,
      unit: 'kg CO₂e',
      description: 'Public bus ride',
    },
    {
      id: '6',
      date: '2026-01-18',
      type: 'Utilities',
      amount: 0.3,
      unit: 'kg CO₂e',
      description: 'Water usage',
    },
    {
      id: '7',
      date: '2026-01-17',
      type: 'Food',
      amount: 0.8,
      unit: 'kg CO₂e',
      description: 'Vegetable salad',
    },
    {
      id: '8',
      date: '2026-01-16',
      type: 'Transport',
      amount: 2.1,
      unit: 'kg CO₂e',
      description: 'Flight to Singapore',
    },
    {
      id: '9',
      date: '2026-01-15',
      type: 'Utilities',
      amount: 0.6,
      unit: 'kg CO₂e',
      description: 'Gas heating',
    },
    {
      id: '10',
      date: '2026-01-14',
      type: 'Food',
      amount: 3.1,
      unit: 'kg CO₂e',
      description: 'BBQ party',
    },
  ];

  // Filter records
  const filteredRecords = mockRecords.filter((record) => {
    const matchType = !filterType || record.type === filterType;
    const recordMonth = record.date.substring(0, 7); // YYYY-MM
    const matchMonth = !filterMonth || recordMonth === filterMonth;
    return matchType && matchMonth;
  });

  const getTypeColor = (type: EmissionType) => {
    const colorMap: { [key in EmissionType]: string } = {
      'Food': 'green',
      'Transport': 'blue',
      'Utilities': 'orange',
    };
    return colorMap[type];
  };

  const getTypeIcon = (type: EmissionType) => {
    const iconMap: { [key in EmissionType]: string } = {
      'Food': '🍴',
      'Transport': '🚗',
      'Utilities': '⚡',
    };
    return iconMap[type];
  };

  const handleDelete = () => {
    Modal.confirm({
      title: 'Delete Record',
      icon: <ExclamationCircleOutlined />,
      content: 'Are you sure you want to delete this record? This action cannot be undone.',
      okText: 'Delete',
      okType: 'danger',
      cancelText: 'Cancel',
      onOk() {
        message.success('Record deleted successfully');
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
      render: (text: EmissionType) => (
        <Tag color={getTypeColor(text)} style={{ marginRight: 0 }}>
          {getTypeIcon(text)} {text}
        </Tag>
      ),
      width: '15%',
    },
    {
      title: 'Amount',
      dataIndex: 'amount',
      key: 'amount',
      render: (text: number, record: Record) => `${text} ${record.unit}`,
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
      render: () => (
        <Button
          type="text"
          danger
          icon={<DeleteOutlined />}
          onClick={() => handleDelete()}
          size="small"
        >
          Delete
        </Button>
      ),
      width: '15%',
    },
  ];

  const typeOptions = [
    { label: 'All Types', value: undefined },
    { label: 'Food', value: 'Food' as EmissionType },
    { label: 'Transport', value: 'Transport' as EmissionType },
    { label: 'Utilities', value: 'Utilities' as EmissionType },
  ];

  const monthOptions = [
    { label: 'All Months', value: undefined },
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
                onChange={setFilterType}
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
                  setFilterType(undefined);
                  setFilterMonth(undefined);
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
            dataSource={filteredRecords.map((record) => ({
              ...record,
              key: record.id,
            }))}
            pagination={{
              pageSize: 10,
              showSizeChanger: true,
              showTotal: (total) => `Total ${total} records`,
            }}
            scroll={{ x: 800 }}
          />
        </div>

        {/* Summary */}
        {filteredRecords.length > 0 && (
          <div style={{ marginTop: '20px', padding: '16px', background: '#f8f5fb', borderRadius: '8px', borderLeft: '4px solid #674fa3', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div style={{ fontSize: '14px', color: '#666' }}>
              Total Emissions: <span style={{ fontWeight: '700', color: '#674fa3', fontSize: '16px' }}>
                {filteredRecords.reduce((sum, r) => sum + r.amount, 0).toFixed(2)} kg CO₂e
              </span>
            </div>
          </div>
        )}

        {filteredRecords.length === 0 && (
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
