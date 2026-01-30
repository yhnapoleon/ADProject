import React, { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Area, AreaChart } from 'recharts';
// @ts-ignore - GeoJSON 文件导入
import singaporeGeoJsonUrl from '../assets/Singapore.geojson?url';
import './AdminDashboard.css';
import request from '../utils/request';

// 区域数据接口 - 与API返回的RegionStatItem对应
interface RegionData {
  regionCode: string;
  regionName: string;
  carbonReduced: number; // kg CO2
  userCount: number;
  reductionRate: number; // percentage
}

// 周报数据接口
interface WeeklyImpactData {
  week: string;
  value: number;
}

// 排放因子接口
interface EmissionFactor {
  id: string;
  category: string;
  itemName: string;
  factor: number;
  unit: string;
  source?: string;
  status: string;
  lastUpdated: string;
}

// 总体统计数据
interface DashboardStats {
  totalUsers: number;
  totalCarbonReduced: number;
  activeFactors: number;
  userGrowth: number;
  carbonGrowth: number;
}


// 简单的坐标投影函数
const projectCoordinates = (lng: number, lat: number, center: [number, number], scale: number, width: number, height: number): [number, number] => {
  const x = (lng - center[0]) * scale + width / 2;
  const y = (center[1] - lat) * scale + height / 2;
  return [x, y];
};

// 将 GeoJSON 坐标转换为 SVG 路径
const geoJsonToPath = (coordinates: any[], center: [number, number], scale: number, width: number, height: number): string => {
  if (!coordinates || coordinates.length === 0) return '';
  
  const processRing = (ring: number[][]): string => {
    if (!ring || ring.length === 0) return '';
    const points = ring.map(([lng, lat]) => {
      const [x, y] = projectCoordinates(lng, lat, center, scale, width, height);
      return `${x},${y}`;
    });
    return `M ${points[0]} L ${points.slice(1).join(' L ')} Z`;
  };
  
  if (Array.isArray(coordinates[0])) {
    if (typeof coordinates[0][0] === 'number') {
      // 单个环
      return processRing(coordinates as number[][]);
    } else if (Array.isArray(coordinates[0][0])) {
      // 多个环
      return (coordinates as number[][][]).map(ring => processRing(ring)).join(' ');
    }
  }
  
  return '';
};

const AdminDashboard: React.FC = () => {
  const navigate = useNavigate();
  const [geoData, setGeoData] = useState<any>(null);
  const [regionData, setRegionData] = useState<Record<string, RegionData>>({});
  const [bounds, setBounds] = useState<{ minX: number; maxX: number; minY: number; maxY: number; center: [number, number]; scale: number } | null>(null);
  const svgRef = useRef<SVGSVGElement>(null);
  const [tooltip, setTooltip] = useState<{ regionCode: string; regionName: string; data: RegionData | null; x: number; y: number } | null>(null);
  const tooltipTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [weeklyData, setWeeklyData] = useState<WeeklyImpactData[]>([]);
  const [emissionFactors, setEmissionFactors] = useState<EmissionFactor[]>([]);
  const [stats, setStats] = useState<DashboardStats>({
    totalUsers: 0,
    totalCarbonReduced: 0,
    activeFactors: 0,
    userGrowth: 0,
    carbonGrowth: 0,
  });
  const [loading, setLoading] = useState(true);
  /** Regional Reduction Trends 时间范围：近7天 / 近30天 */
  const [regionTrendsRange, setRegionTrendsRange] = useState<'7' | '30'>('30');
  const regionTrendsRangeInitialized = useRef(false);

  // 计算边界框和投影配置，确保地图完整显示
  const calculateBounds = (data: any) => {
    let minLng = Infinity;
    let maxLng = -Infinity;
    let minLat = Infinity;
    let maxLat = -Infinity;

    const processCoordinates = (coords: any): void => {
      if (!Array.isArray(coords)) return;
      
      if (coords.length > 0 && typeof coords[0] === 'number') {
        // 这是一个坐标点 [lng, lat]
        const [lng, lat] = coords;
        minLng = Math.min(minLng, lng);
        maxLng = Math.max(maxLng, lng);
        minLat = Math.min(minLat, lat);
        maxLat = Math.max(maxLat, lat);
      } else {
        // 这是一个数组，递归处理
        coords.forEach(processCoordinates);
      }
    };

    if (data.features) {
      data.features.forEach((feature: any) => {
        if (feature.geometry?.coordinates) {
          processCoordinates(feature.geometry.coordinates);
        }
      });
    }

    const centerLng = (minLng + maxLng) / 2;
    const centerLat = (minLat + maxLat) / 2;
    const lngRange = maxLng - minLng;
    const latRange = maxLat - minLat;

    // 计算缩放比例，留出边距，确保地图完整显示
    const padding = 0.15; // 15% 边距
    const width = 800;
    const height = 600;
    const scaleX = (width * (1 - padding * 2)) / lngRange;
    const scaleY = (height * (1 - padding * 2)) / latRange;
    const scale = Math.min(scaleX, scaleY);

    return {
      minX: minLng,
      maxX: maxLng,
      minY: minLat,
      maxY: maxLat,
      center: [centerLng, centerLat] as [number, number],
      scale,
    };
  };

  // 加载所有数据
  useEffect(() => {
    const loadAllData = async () => {
      setLoading(true);
      try {
        // 并行加载：全量区域统计（用于顶部卡片）、近30天热力图、其余数据
        const [geoJsonRes, regionsRes, regionMapRes, weeklyRes, factorsRes] = await Promise.allSettled([
          fetch(singaporeGeoJsonUrl).then(res => res.json()),
          request.get('/admin/regions/stats'),
          request.get('/admin/regions/stats/monthly'),
          request.get('/admin/impact/weekly'),
          request.get('/admin/emission-factors', { params: { page: 1, pageSize: 10 } }),
        ]);

        // 加载GeoJSON数据
        if (geoJsonRes.status === 'fulfilled') {
          const geoData = geoJsonRes.value;
          setGeoData(geoData);
          const calculatedBounds = calculateBounds(geoData);
          setBounds(calculatedBounds);
        }

        // 全量区域统计 → 仅用于顶部 Carbon Reduced / Total Eco-Users 卡片
        if (regionsRes.status === 'fulfilled') {
          const regions: RegionData[] = regionsRes.value || [];
          const totalUsers = regions.reduce((sum, r) => sum + (r.userCount || 0), 0);
          const totalCarbonReduced = regions.reduce((sum, r) => sum + (r.carbonReduced || 0), 0);
          setStats(prev => ({
            ...prev,
            totalUsers,
            totalCarbonReduced: Math.round(totalCarbonReduced),
          }));
        } else {
          console.error('Failed to load regions stats:', regionsRes.reason);
        }

        // 热力图默认近30天数据
        if (regionMapRes.status === 'fulfilled') {
          const regions: RegionData[] = regionMapRes.value || [];
          const regionDataMap: Record<string, RegionData> = {};
          regions.forEach((region: RegionData) => {
            if (region.regionCode) {
              regionDataMap[region.regionCode] = region;
            }
          });
          setRegionData(regionDataMap);
        } else {
          console.error('Failed to load region map stats:', regionMapRes.reason);
        }

        // 加载周报数据
        if (weeklyRes.status === 'fulfilled') {
          const weekly: any[] = weeklyRes.value || [];
          // 转换API返回的数据格式为图表所需格式
          const formattedWeekly = weekly.map((item: any, index: number) => ({
            week: item.week || `Week ${index + 1}`,
            value: item.value || item.carbonReduced || 0,
          }));
          setWeeklyData(formattedWeekly);
        } else {
          console.error('Failed to load weekly impact:', weeklyRes.reason);
          // API失败时使用空数组，不显示硬编码数据
          setWeeklyData([]);
        }

        // 加载排放因子列表
        if (factorsRes.status === 'fulfilled') {
          const factorsData = factorsRes.value;
          // 处理分页响应或直接数组
          const factors: EmissionFactor[] = Array.isArray(factorsData) 
            ? factorsData 
            : (factorsData?.data || factorsData?.items || []);
          
          setEmissionFactors(factors.slice(0, 10)); // 只显示最近10条
          setStats(prev => ({
            ...prev,
            activeFactors: factorsData?.total || factors.length || 0,
          }));
        } else {
          console.error('Failed to load emission factors:', factorsRes.reason);
        }
      } catch (error) {
        console.error('Error loading dashboard data:', error);
      } finally {
        setLoading(false);
      }
    };

    loadAllData();
  }, []);

  // 切换近7天/近30天时重新拉取热力图数据
  useEffect(() => {
    if (!regionTrendsRangeInitialized.current) {
      regionTrendsRangeInitialized.current = true;
      return;
    }
    const url = regionTrendsRange === '7' ? '/admin/regions/stats/weekly' : '/admin/regions/stats/monthly';
    request
      .get(url)
      .then((regions: RegionData[] | any) => {
        const list = Array.isArray(regions) ? regions : regions?.items ?? regions?.data ?? [];
        const regionDataMap: Record<string, RegionData> = {};
        list.forEach((r: RegionData) => {
          if (r.regionCode) regionDataMap[r.regionCode] = r;
        });
        setRegionData(regionDataMap);
      })
      .catch((e) => console.error('Failed to load region stats by range:', e));
  }, [regionTrendsRange]);

  // 根据区域数据获取颜色
  const getRegionColor = (regionCode: string): string => {
    if (!regionCode) return '#e0e0e0'; // 默认灰色（无区域代码）
    
    const data = regionData[regionCode];
    if (!data) {
      console.warn(`No data found for region: ${regionCode}`);
      return '#e0e0e0'; // 默认灰色（无数据）
    }

    // 根据碳减排量设置颜色强度
    const carbonReduced = data.carbonReduced;
    
    if (carbonReduced > 1750) {
      return '#2E7D32'; // 深绿色 - 高减排 (>1750)
    } else if (carbonReduced > 1000) {
      return '#4CAF50'; // 中绿色 (1000-1750)
    } else if (carbonReduced > 500) {
      return '#81C784'; // 浅绿色 (500-1000)
    } else {
      return '#C8E6C9'; // 很浅的绿色 (<500)
    }
  };


  return (
    <div className="dashboard">
      <h1 className="page-title">Community Macro-Monitoring</h1>
      
      <div className="stats-cards">
        <div
          className="stat-card stat-card-clickable"
          onClick={() => navigate('/admin/users')}
          role="button"
          tabIndex={0}
          onKeyDown={(e) => e.key === 'Enter' && navigate('/admin/users')}
        >
          <h3>Total Eco-Users</h3>
          <div className="stat-value">
            {loading ? 'Loading...' : stats.totalUsers.toLocaleString()}
          </div>
          <div className="stat-change positive">
            {stats.userGrowth > 0 ? `+${stats.userGrowth}% this week` : 'Database Updated'}
          </div>
        </div>
        <div
          className="stat-card stat-card-clickable"
          onClick={() => navigate('/admin/community-analytics')}
          role="button"
          tabIndex={0}
          onKeyDown={(e) => e.key === 'Enter' && navigate('/admin/community-analytics')}
        >
          <h3>Carbon Reduced</h3>
          <div className="stat-value">
            {loading ? 'Loading...' : `${stats.totalCarbonReduced.toLocaleString()} kg`}
          </div>
          <div className="stat-change positive">
            {stats.carbonGrowth > 0 ? `+${stats.carbonGrowth}% this week` : 'Database Updated'}
          </div>
        </div>
        <div
          className="stat-card stat-card-clickable"
          onClick={() => navigate('/admin/emission-factors')}
          role="button"
          tabIndex={0}
          onKeyDown={(e) => e.key === 'Enter' && navigate('/admin/emission-factors')}
        >
          <h3>Active Factors</h3>
          <div className="stat-value">
            {loading ? 'Loading...' : stats.activeFactors.toLocaleString()}
          </div>
          <div className="stat-change">Database Updated</div>
        </div>
      </div>

      <div className="charts-container">
        <div className="chart-card">
          <div className="chart-card-header-with-tabs">
            <h3>Regional Reduction Trends (Singapore)</h3>
            <div className="region-trends-tabs" role="tablist" aria-label="时间范围">
              <button
                type="button"
                role="tab"
                aria-selected={regionTrendsRange === '7'}
                className={`region-trends-tab ${regionTrendsRange === '7' ? 'active' : ''}`}
                onClick={() => setRegionTrendsRange('7')}
              >
                近7天
              </button>
              <button
                type="button"
                role="tab"
                aria-selected={regionTrendsRange === '30'}
                className={`region-trends-tab ${regionTrendsRange === '30' ? 'active' : ''}`}
                onClick={() => setRegionTrendsRange('30')}
              >
                近30天
              </button>
            </div>
          </div>
          <div 
            className="singapore-map-container"
            onMouseLeave={() => {
              if (tooltipTimeoutRef.current) {
                clearTimeout(tooltipTimeoutRef.current);
              }
              tooltipTimeoutRef.current = setTimeout(() => {
                setTooltip(null);
              }, 100);
            }}
          >
            {geoData && bounds && Object.keys(regionData).length > 0 ? (
              <>
                <svg
                  ref={svgRef}
                  viewBox="0 0 800 600"
                  className="singapore-map"
                  preserveAspectRatio="xMidYMid meet"
                  onMouseMove={(e) => {
                    if (tooltipTimeoutRef.current) {
                      clearTimeout(tooltipTimeoutRef.current);
                      tooltipTimeoutRef.current = null;
                    }
                    const containerRect = e.currentTarget.closest('.singapore-map-container')?.getBoundingClientRect();
                    if (!containerRect) return;
                    const x = e.clientX - containerRect.left;
                    const y = e.clientY - containerRect.top;
                    const target = e.target as SVGElement;
                    const path = target.closest?.('path.region-path') as SVGPathElement | null;
                    if (path) {
                      const regionCode = path.getAttribute('data-region-code') ?? '';
                      const regionName = path.getAttribute('data-region-name') ?? '';
                      const data = regionData[regionCode] ?? null;
                      setTooltip({ regionCode, regionName, data, x, y });
                    } else {
                      setTooltip(prev => prev ? { ...prev, x, y } : null);
                    }
                  }}
                >
                  {geoData.features?.map((feature: any, index: number) => {
                    const regionCode = feature.properties?.REGION_C || '';
                    const regionName = feature.properties?.REGION_N || '';
                    const fillColor = getRegionColor(regionCode);
                    const geometry = feature.geometry;
                    const data = regionData[regionCode] || null;
                    
                    if (geometry.type === 'MultiPolygon') {
                      return geometry.coordinates.map((polygon: any, polyIndex: number) => {
                        const pathData = polygon.map((ring: number[][]) => 
                          geoJsonToPath(ring, bounds.center, bounds.scale, 800, 600)
                        ).join(' ');
                        
                        return (
                          <path
                            key={`${index}-${polyIndex}`}
                            d={pathData}
                            fill={fillColor}
                            stroke="#fff"
                            strokeWidth={0.5}
                            className="region-path"
                            data-region-code={regionCode}
                            data-region-name={regionName}
                            style={{ cursor: 'pointer' }}
                            onMouseEnter={(e) => {
                              // 清除任何待清除的 tooltip 延迟
                              if (tooltipTimeoutRef.current) {
                                clearTimeout(tooltipTimeoutRef.current);
                                tooltipTimeoutRef.current = null;
                              }
                              
                              // 找到所有相同区域的 path 元素并同时高亮
                              const svgElement = e.currentTarget.closest('svg');
                              if (svgElement && regionCode) {
                                const sameRegionPaths = svgElement.querySelectorAll(
                                  `path[data-region-code="${regionCode}"]`
                                ) as NodeListOf<SVGPathElement>;
                                
                                sameRegionPaths.forEach((path) => {
                                  path.style.fill = '#66BB6A';
                                  path.style.stroke = '#2E7D32';
                                  path.style.strokeWidth = '1.5';
                                });
                              }
                              
                              const containerRect = e.currentTarget.closest('.singapore-map-container')?.getBoundingClientRect();
                              if (containerRect) {
                                setTooltip({
                                  regionCode,
                                  regionName,
                                  data,
                                  x: e.clientX - containerRect.left,
                                  y: e.clientY - containerRect.top,
                                });
                              }
                            }}
                            onMouseLeave={(e) => {
                              // 找到所有相同区域的 path 元素并恢复颜色
                              const svgElement = e.currentTarget.closest('svg');
                              if (svgElement && regionCode) {
                                const sameRegionPaths = svgElement.querySelectorAll(
                                  `path[data-region-code="${regionCode}"]`
                                ) as NodeListOf<SVGPathElement>;
                                
                                sameRegionPaths.forEach((path) => {
                                  path.style.fill = fillColor;
                                  path.style.stroke = '#fff';
                                  path.style.strokeWidth = '0.5';
                                });
                              }
                              
                              // 延迟清除 tooltip，给新区域的 onMouseEnter 时间触发
                              if (tooltipTimeoutRef.current) {
                                clearTimeout(tooltipTimeoutRef.current);
                              }
                              tooltipTimeoutRef.current = setTimeout(() => {
                                // 检查鼠标是否真的离开了所有区域
                                const relatedTarget = e.relatedTarget as Element;
                                if (!relatedTarget || !relatedTarget.closest('.region-path')) {
                                  setTooltip(null);
                                }
                                tooltipTimeoutRef.current = null;
                              }, 50);
                            }}
                          />
                        );
                      });
                    }
                    
                    return null;
                  })}
                </svg>
                {tooltip && (
                  <div
                    className="map-tooltip"
                    style={{
                      left: `${tooltip.x + 10}px`,
                      top: `${tooltip.y + 10}px`,
                    }}
                  >
                    <div className="tooltip-title">{tooltip.regionName}</div>
                    {tooltip.data ? (
                      <div className="tooltip-content">
                        <div className="tooltip-item">
                          <span className="tooltip-label">碳减排量:</span>
                          <span className="tooltip-value">{tooltip.data.carbonReduced.toFixed(2)} kg CO₂</span>
                        </div>
                        <div className="tooltip-item">
                          <span className="tooltip-label">用户数量:</span>
                          <span className="tooltip-value">{tooltip.data.userCount.toLocaleString()}</span>
                        </div>
                        <div className="tooltip-item">
                          <span className="tooltip-label">减排率:</span>
                          <span className="tooltip-value">{tooltip.data.reductionRate.toFixed(1)}%</span>
                        </div>
                      </div>
                    ) : (
                      <div className="tooltip-content">
                        <div className="tooltip-item">暂无数据</div>
                      </div>
                    )}
                  </div>
                )}
              </>
            ) : (
              <div className="map-loading">Loading map data...</div>
            )}
          </div>
          {Object.keys(regionData).length > 0 && (
            <div className="map-legend">
              <div className="legend-title">Carbon Reduction (kg CO2)</div>
              <div className="legend-items">
                <div className="legend-item">
                  <span className="legend-color" style={{ backgroundColor: '#2E7D32' }}></span>
                  <span>High (&gt;1750)</span>
                </div>
                <div className="legend-item">
                  <span className="legend-color" style={{ backgroundColor: '#4CAF50' }}></span>
                  <span>Medium (1000-1750)</span>
                </div>
                <div className="legend-item">
                  <span className="legend-color" style={{ backgroundColor: '#81C784' }}></span>
                  <span>Low (500-1000)</span>
                </div>
                <div className="legend-item">
                  <span className="legend-color" style={{ backgroundColor: '#C8E6C9' }}></span>
                  <span>Very Low (&lt;500)</span>
                </div>
              </div>
            </div>
          )}
        </div>
        <div className="chart-card">
          <h3>Weekly Platform Impact (kg CO2)</h3>
          {loading ? (
            <div style={{ height: 300, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              Loading chart data...
            </div>
          ) : (
            <ResponsiveContainer width="100%" height={300}>
              <AreaChart data={weeklyData}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e0e0e0" />
                <XAxis dataKey="week" stroke="#666" />
                <YAxis stroke="#666" domain={[0, 'dataMax + 200']} />
                <Tooltip />
                <Area type="monotone" dataKey="value" stroke="#4CAF50" fill="#4CAF50" fillOpacity={0.3} />
              </AreaChart>
            </ResponsiveContainer>
          )}
        </div>
      </div>

      <div
        className="table-card table-card-clickable"
        onClick={() => navigate('/admin/emission-factors')}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => e.key === 'Enter' && navigate('/admin/emission-factors')}
      >
        <h3>Emission Factor Database (Recent Updates)</h3>
        {loading ? (
          <div style={{ padding: '20px', textAlign: 'center' }}>Loading emission factors...</div>
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Category</th>
                <th>Item Name</th>
                <th>Emission Factor ({emissionFactors[0]?.unit || 'kg CO2/unit'})</th>
                <th>Last Updated</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {emissionFactors.length > 0 ? (
                emissionFactors.map((item) => (
                  <tr key={item.id}>
                    <td>{item.id}</td>
                    <td>{item.category}</td>
                    <td>{item.itemName}</td>
                    <td>{item.factor}</td>
                    <td>{item.lastUpdated}</td>
                    <td>
                      <span className={`status-badge ${item.status?.toLowerCase().replace(/\s+/g, '-') || 'active'}`}>
                        {item.status || 'Active'}
                      </span>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={6} style={{ textAlign: 'center', padding: '20px' }}>
                    No emission factors found
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

export default AdminDashboard;
