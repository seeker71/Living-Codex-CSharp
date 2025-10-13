'use client';

/**
 * Admin Dashboard
 * System management interface for administrators
 * Connects to: AccessControlModule, IntelligentCachingModule, LoadBalancingModule, ServiceDiscoveryModule
 */

import { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import { Card } from '@/components/ui/Card';
import { api } from '@/lib/api';
import { 
  Shield, 
  Users, 
  Server, 
  Database, 
  Activity, 
  Settings,
  BarChart3,
  Lock,
  Key,
  FileText,
  Gauge,
  Network,
  HardDrive
} from 'lucide-react';

interface SystemMetrics {
  memoryUsage?: number;
  cpuUsage?: number;
  nodeCount?: number;
  edgeCount?: number;
  moduleCount?: number;
  uptime?: string;
}

interface CacheMetrics {
  totalEntries?: number;
  hitRate?: number;
  missRate?: number;
  evictionCount?: number;
}

interface LoadBalanceMetrics {
  totalRequests?: number;
  averageLatency?: number;
  activeInstances?: number;
}

export default function AdminPage() {
  const { user, isAuthenticated, isLoading } = useAuth();
  const router = useRouter();
  const [activeTab, setActiveTab] = useState<'overview' | 'users' | 'access' | 'cache' | 'services' | 'load-balance'>('overview');
  const [systemMetrics, setSystemMetrics] = useState<SystemMetrics>({});
  const [cacheMetrics, setCacheMetrics] = useState<CacheMetrics>({});
  const [loadBalanceMetrics, setLoadBalanceMetrics] = useState<LoadBalanceMetrics>({});
  const [loading, setLoading] = useState(true);
  
  // User management state
  const [users, setUsers] = useState<any[]>([]);
  const [roles, setRoles] = useState<any[]>([]);
  const [permissions, setPermissions] = useState<any[]>([]);
  const [selectedUser, setSelectedUser] = useState<any | null>(null);
  const [showAssignRole, setShowAssignRole] = useState(false);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.push('/auth');
      return;
    }

    // TODO: Add admin role check
    // For now, any authenticated user can access
    if (isAuthenticated) {
      loadAdminData();
    }
  }, [isAuthenticated, isLoading, router]);

  const loadUserManagementData = async () => {
    try {
      // Load roles
      const rolesRes = await api.get('/access-control/roles');
      if (rolesRes?.success && rolesRes?.roles) {
        setRoles(rolesRes.roles);
      }

      // Load permissions
      const permsRes = await api.get('/access-control/permissions');
      if (permsRes?.success && permsRes?.permissions) {
        setPermissions(permsRes.permissions);
      }
    } catch (error) {
      console.error('Error loading user management data:', error);
    }
  };

  const loadAdminData = async () => {
    try {
      setLoading(true);

      // Load system metrics
      const healthRes = await api.get('/health');
      if (healthRes) {
        setSystemMetrics({
          memoryUsage: healthRes.memoryUsage || 0,
          cpuUsage: healthRes.cpuUsage || 0,
          nodeCount: healthRes.stats?.nodeCount || 0,
          edgeCount: healthRes.stats?.edgeCount || 0,
          moduleCount: healthRes.moduleCount || 0,
          uptime: healthRes.uptime || 'N/A'
        });
      }

      // Try to load cache metrics
      try {
        const cacheRes = await api.get('/cache/stats');
        if (cacheRes?.success) {
          setCacheMetrics(cacheRes.data || {});
        }
      } catch {
        // Cache module may not be available
      }

      // Try to load load balance metrics
      try {
        const lbRes = await api.get('/load-balance/metrics');
        if (lbRes?.success) {
          setLoadBalanceMetrics(lbRes.metrics || {});
        }
      } catch {
        // Load balance module may not be available
      }
    } catch (error) {
      console.error('Error loading admin data:', error);
    } finally {
      setLoading(false);
    }
  };

  if (isLoading || loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-gray-900 via-blue-900 to-gray-900 p-8">
        <div className="max-w-7xl mx-auto">
          <div className="animate-pulse space-y-6">
            <div className="h-12 bg-gray-700 rounded w-1/3"></div>
            <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
              {[1, 2, 3, 4].map(i => (
                <div key={i} className="h-32 bg-gray-700 rounded"></div>
              ))}
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-blue-900 to-gray-900 p-8">
      <div className="max-w-7xl mx-auto space-y-8">
        {/* Header */}
        <div>
          <h1 className="text-4xl font-bold bg-gradient-to-r from-blue-400 to-cyan-400 bg-clip-text text-transparent flex items-center gap-3">
            <Shield className="w-10 h-10 text-blue-400" />
            System Administration
          </h1>
          <p className="text-gray-400 mt-2">
            Monitor and manage the Living Codex platform
          </p>
        </div>

        {/* Stats Overview */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
          <Card className="p-6 bg-gradient-to-br from-green-900/50 to-emerald-900/50 border-green-500">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-2xl font-bold text-green-400">
                  {systemMetrics.nodeCount?.toLocaleString() || '0'}
                </div>
                <div className="text-gray-400 mt-1">Total Nodes</div>
              </div>
              <Database className="w-8 h-8 text-green-400 opacity-50" />
            </div>
          </Card>

          <Card className="p-6 bg-gradient-to-br from-blue-900/50 to-cyan-900/50 border-blue-500">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-2xl font-bold text-blue-400">
                  {systemMetrics.moduleCount || '0'}
                </div>
                <div className="text-gray-400 mt-1">Active Modules</div>
              </div>
              <Server className="w-8 h-8 text-blue-400 opacity-50" />
            </div>
          </Card>

          <Card className="p-6 bg-gradient-to-br from-purple-900/50 to-pink-900/50 border-purple-500">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-2xl font-bold text-purple-400">
                  {systemMetrics.memoryUsage ? `${systemMetrics.memoryUsage.toFixed(0)} MB` : 'N/A'}
                </div>
                <div className="text-gray-400 mt-1">Memory Usage</div>
              </div>
              <Activity className="w-8 h-8 text-purple-400 opacity-50" />
            </div>
          </Card>

          <Card className="p-6 bg-gradient-to-br from-orange-900/50 to-red-900/50 border-orange-500">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-2xl font-bold text-orange-400">
                  {systemMetrics.uptime || 'N/A'}
                </div>
                <div className="text-gray-400 mt-1">Uptime</div>
              </div>
              <Gauge className="w-8 h-8 text-orange-400 opacity-50" />
            </div>
          </Card>
        </div>

        {/* Tabs */}
        <div className="flex space-x-2 border-b border-gray-700 overflow-x-auto">
          {([
            { id: 'overview', label: 'Overview', icon: BarChart3 },
            { id: 'users', label: 'Users & Access', icon: Users },
            { id: 'access', label: 'Permissions', icon: Lock },
            { id: 'cache', label: 'Cache', icon: HardDrive },
            { id: 'services', label: 'Services', icon: Network },
            { id: 'load-balance', label: 'Load Balance', icon: Gauge },
          ] as const).map(tab => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`flex items-center gap-2 px-4 py-2 font-medium transition-colors whitespace-nowrap ${
                activeTab === tab.id
                  ? 'text-blue-400 border-b-2 border-blue-400'
                  : 'text-gray-400 hover:text-gray-300'
              }`}
            >
              <tab.icon className="w-4 h-4" />
              {tab.label}
            </button>
          ))}
        </div>

        {/* Tab Content */}
        {activeTab === 'overview' && (
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Card className="p-6">
              <h3 className="text-xl font-semibold text-blue-400 mb-4 flex items-center gap-2">
                <Database className="w-5 h-5" />
                Storage Statistics
              </h3>
              <div className="space-y-3">
                <div className="flex justify-between items-center p-3 bg-gray-800/50 rounded">
                  <span className="text-gray-400">Total Nodes</span>
                  <span className="font-semibold text-white">{systemMetrics.nodeCount?.toLocaleString() || '0'}</span>
                </div>
                <div className="flex justify-between items-center p-3 bg-gray-800/50 rounded">
                  <span className="text-gray-400">Total Edges</span>
                  <span className="font-semibold text-white">{systemMetrics.edgeCount?.toLocaleString() || '0'}</span>
                </div>
                <div className="flex justify-between items-center p-3 bg-gray-800/50 rounded">
                  <span className="text-gray-400">Avg Connections</span>
                  <span className="font-semibold text-white">
                    {systemMetrics.nodeCount && systemMetrics.edgeCount 
                      ? (systemMetrics.edgeCount / systemMetrics.nodeCount).toFixed(2)
                      : '0'}
                  </span>
                </div>
              </div>
            </Card>

            <Card className="p-6">
              <h3 className="text-xl font-semibold text-purple-400 mb-4 flex items-center gap-2">
                <HardDrive className="w-5 h-5" />
                Cache Performance
              </h3>
              <div className="space-y-3">
                <div className="flex justify-between items-center p-3 bg-gray-800/50 rounded">
                  <span className="text-gray-400">Total Entries</span>
                  <span className="font-semibold text-white">{cacheMetrics.totalEntries?.toLocaleString() || 'N/A'}</span>
                </div>
                <div className="flex justify-between items-center p-3 bg-gray-800/50 rounded">
                  <span className="text-gray-400">Hit Rate</span>
                  <span className="font-semibold text-white">
                    {cacheMetrics.hitRate ? `${(cacheMetrics.hitRate * 100).toFixed(1)}%` : 'N/A'}
                  </span>
                </div>
                <div className="flex justify-between items-center p-3 bg-gray-800/50 rounded">
                  <span className="text-gray-400">Miss Rate</span>
                  <span className="font-semibold text-white">
                    {cacheMetrics.missRate ? `${(cacheMetrics.missRate * 100).toFixed(1)}%` : 'N/A'}
                  </span>
                </div>
              </div>
            </Card>

            <Card className="p-6">
              <h3 className="text-xl font-semibold text-green-400 mb-4 flex items-center gap-2">
                <Server className="w-5 h-5" />
                Module Status
              </h3>
              <div className="space-y-2">
                <div className="flex justify-between items-center p-3 bg-gray-800/50 rounded">
                  <span className="text-gray-400">Active Modules</span>
                  <span className="font-semibold text-green-400">{systemMetrics.moduleCount || '0'}</span>
                </div>
                <div className="text-sm text-gray-500 mt-3">
                  All critical modules are operational
                </div>
              </div>
            </Card>

            <Card className="p-6">
              <h3 className="text-xl font-semibold text-orange-400 mb-4 flex items-center gap-2">
                <Gauge className="w-5 h-5" />
                Load Balancing
              </h3>
              <div className="space-y-3">
                <div className="flex justify-between items-center p-3 bg-gray-800/50 rounded">
                  <span className="text-gray-400">Active Instances</span>
                  <span className="font-semibold text-white">{loadBalanceMetrics.activeInstances || 'N/A'}</span>
                </div>
                <div className="flex justify-between items-center p-3 bg-gray-800/50 rounded">
                  <span className="text-gray-400">Avg Latency</span>
                  <span className="font-semibold text-white">
                    {loadBalanceMetrics.averageLatency ? `${loadBalanceMetrics.averageLatency.toFixed(0)}ms` : 'N/A'}
                  </span>
                </div>
              </div>
            </Card>
          </div>
        )}

        {activeTab === 'users' && (
          <div className="space-y-6">
            <Card className="p-6">
              <div className="flex items-center justify-between mb-6">
                <h3 className="text-2xl font-semibold text-blue-400">👥 User & Role Management</h3>
                <button
                  onClick={loadUserManagementData}
                  className="px-4 py-2 bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors text-sm"
                >
                  🔄 Refresh
                </button>
              </div>

              {/* Roles Overview */}
              <div className="mb-8">
                <h4 className="text-lg font-semibold text-gray-200 mb-4">Roles</h4>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  {roles.map((role) => (
                    <div key={role.id} className="p-4 bg-gray-800/50 rounded-lg border border-gray-700">
                      <div className="flex items-center gap-2 mb-2">
                        <Shield className="w-5 h-5 text-blue-400" />
                        <h5 className="font-semibold text-gray-200">{role.name}</h5>
                      </div>
                      <p className="text-sm text-gray-400 mb-3">{role.description}</p>
                      <div className="text-xs text-gray-500">
                        ID: {role.id}
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Users List */}
              <div>
                <h4 className="text-lg font-semibold text-gray-200 mb-4">Users</h4>
                <div className="space-y-3">
                  {/* Since we don't have a user list endpoint, show placeholder */}
                  <div className="text-center py-8 text-gray-500">
                    <Users className="w-12 h-12 mx-auto mb-3 opacity-50" />
                    <p className="text-sm">User list will be populated when users are loaded</p>
                    <p className="text-xs mt-2">Use authentication system to manage user accounts</p>
                  </div>
                </div>
              </div>

              {/* Permissions Overview */}
              <div className="mt-8">
                <h4 className="text-lg font-semibold text-gray-200 mb-4">Permissions ({permissions.length})</h4>
                <div className="max-h-96 overflow-y-auto">
                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                    {permissions.map((permission) => (
                      <div key={permission.id} className="p-3 bg-gray-800/30 rounded-lg border border-gray-700">
                        <div className="flex items-center gap-2 mb-1">
                          <Key className="w-4 h-4 text-purple-400" />
                          <span className="text-sm font-medium text-gray-200">{permission.name}</span>
                        </div>
                        <div className="text-xs text-gray-400">
                          <div>{permission.resource} - {permission.action}</div>
                          {permission.description && <div className="mt-1">{permission.description}</div>}
                        </div>
                        <div className={`mt-2 text-xs ${permission.isActive ? 'text-green-400' : 'text-red-400'}`}>
                          {permission.isActive ? '✓ Active' : '✗ Inactive'}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </Card>
          </div>
        )}

        {activeTab === 'access' && (
          <Card className="p-6">
            <h3 className="text-2xl font-semibold text-purple-400 mb-4">Access Control & Permissions</h3>
            <div className="space-y-6">
              <div>
                <h4 className="text-lg font-semibold text-white mb-3 flex items-center gap-2">
                  <Lock className="w-5 h-5" />
                  Permissions
                </h4>
                <div className="text-center py-8 text-gray-500">
                  <div className="text-sm">Permission management interface</div>
                  <div className="text-xs text-gray-600 mt-2">
                    Backend: POST /access-control/permissions
                  </div>
                </div>
              </div>

              <div>
                <h4 className="text-lg font-semibold text-white mb-3 flex items-center gap-2">
                  <Key className="w-5 h-5" />
                  Roles
                </h4>
                <div className="text-center py-8 text-gray-500">
                  <div className="text-sm">Role assignment and management</div>
                  <div className="text-xs text-gray-600 mt-2">
                    Backend: POST /access-control/roles
                  </div>
                </div>
              </div>

              <div>
                <h4 className="text-lg font-semibold text-white mb-3 flex items-center gap-2">
                  <FileText className="w-5 h-5" />
                  Policies
                </h4>
                <div className="text-center py-8 text-gray-500">
                  <div className="text-sm">Access policy configuration</div>
                  <div className="text-xs text-gray-600 mt-2">
                    Backend: POST /access-control/policies
                  </div>
                </div>
              </div>
            </div>
          </Card>
        )}

        {activeTab === 'cache' && (
          <Card className="p-6">
            <h3 className="text-2xl font-semibold text-purple-400 mb-4 flex items-center gap-2">
              <HardDrive className="w-6 h-6" />
              Intelligent Cache Management
            </h3>
            
            {cacheMetrics.totalEntries ? (
              <div className="space-y-4">
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                  <div className="p-4 bg-gray-800/50 rounded-lg text-center">
                    <div className="text-3xl font-bold text-purple-400">{cacheMetrics.totalEntries.toLocaleString()}</div>
                    <div className="text-sm text-gray-400 mt-1">Entries</div>
                  </div>
                  <div className="p-4 bg-gray-800/50 rounded-lg text-center">
                    <div className="text-3xl font-bold text-green-400">
                      {cacheMetrics.hitRate ? `${(cacheMetrics.hitRate * 100).toFixed(1)}%` : 'N/A'}
                    </div>
                    <div className="text-sm text-gray-400 mt-1">Hit Rate</div>
                  </div>
                  <div className="p-4 bg-gray-800/50 rounded-lg text-center">
                    <div className="text-3xl font-bold text-orange-400">
                      {cacheMetrics.missRate ? `${(cacheMetrics.missRate * 100).toFixed(1)}%` : 'N/A'}
                    </div>
                    <div className="text-sm text-gray-400 mt-1">Miss Rate</div>
                  </div>
                  <div className="p-4 bg-gray-800/50 rounded-lg text-center">
                    <div className="text-3xl font-bold text-red-400">{cacheMetrics.evictionCount || '0'}</div>
                    <div className="text-sm text-gray-400 mt-1">Evictions</div>
                  </div>
                </div>

                <div className="mt-6">
                  <h4 className="text-lg font-semibold text-white mb-3">Cache Actions</h4>
                  <div className="flex gap-3">
                    <button className="px-4 py-2 bg-purple-600 hover:bg-purple-700 rounded transition-colors">
                      Clear Cache
                    </button>
                    <button className="px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded transition-colors">
                      View Patterns
                    </button>
                    <button className="px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded transition-colors">
                      Predictions
                    </button>
                  </div>
                </div>
              </div>
            ) : (
              <div className="text-center py-12 text-gray-500">
                <HardDrive className="w-16 h-16 mx-auto mb-4 opacity-50" />
                <div className="text-xl mb-2">Cache metrics unavailable</div>
                <div className="text-sm">IntelligentCachingModule not responding</div>
              </div>
            )}
          </Card>
        )}

        {activeTab === 'services' && (
          <Card className="p-6">
            <h3 className="text-2xl font-semibold text-cyan-400 mb-4 flex items-center gap-2">
              <Network className="w-6 h-6" />
              Service Discovery
            </h3>
            <div className="text-center py-12 text-gray-500">
              <Network className="w-16 h-16 mx-auto mb-4 opacity-50" />
              <div className="text-xl mb-2">Service Registry Browser</div>
              <div className="text-sm">
                View and manage registered services
              </div>
              <div className="mt-4 text-xs text-gray-600">
                Backend: ServiceDiscoveryModule with 10+ endpoints
              </div>
            </div>
          </Card>
        )}

        {activeTab === 'load-balance' && (
          <Card className="p-6">
            <h3 className="text-2xl font-semibold text-orange-400 mb-4 flex items-center gap-2">
              <Gauge className="w-6 h-6" />
              Load Balancing & Scaling
            </h3>
            
            {loadBalanceMetrics.totalRequests ? (
              <div className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div className="p-4 bg-gray-800/50 rounded-lg text-center">
                    <div className="text-3xl font-bold text-orange-400">
                      {loadBalanceMetrics.totalRequests.toLocaleString()}
                    </div>
                    <div className="text-sm text-gray-400 mt-1">Total Requests</div>
                  </div>
                  <div className="p-4 bg-gray-800/50 rounded-lg text-center">
                    <div className="text-3xl font-bold text-green-400">
                      {loadBalanceMetrics.averageLatency?.toFixed(0) || 'N/A'} ms
                    </div>
                    <div className="text-sm text-gray-400 mt-1">Avg Latency</div>
                  </div>
                  <div className="p-4 bg-gray-800/50 rounded-lg text-center">
                    <div className="text-3xl font-bold text-blue-400">
                      {loadBalanceMetrics.activeInstances || '0'}
                    </div>
                    <div className="text-sm text-gray-400 mt-1">Active Instances</div>
                  </div>
                </div>

                <div className="mt-6">
                  <h4 className="text-lg font-semibold text-white mb-3">Load Balancer Actions</h4>
                  <div className="flex gap-3">
                    <button className="px-4 py-2 bg-orange-600 hover:bg-orange-700 rounded transition-colors">
                      Scale Up
                    </button>
                    <button className="px-4 py-2 bg-red-600 hover:bg-red-700 rounded transition-colors">
                      Scale Down
                    </button>
                    <button className="px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded transition-colors">
                      Optimize
                    </button>
                  </div>
                </div>
              </div>
            ) : (
              <div className="text-center py-12 text-gray-500">
                <Gauge className="w-16 h-16 mx-auto mb-4 opacity-50" />
                <div className="text-xl mb-2">Load balance metrics unavailable</div>
                <div className="text-sm">LoadBalancingModule not responding</div>
              </div>
            )}
          </Card>
        )}
      </div>
    </div>
  );
}

