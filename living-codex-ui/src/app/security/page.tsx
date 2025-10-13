'use client';

/**
 * Security Settings Page
 * Active sessions, access logs, and digital signatures management
 * Connects to: IdentityModule, AccessControlModule, DigitalSignatureModule
 */

import { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import { Card } from '@/components/ui/Card';
import { api } from '@/lib/api';
import { Shield, Key, Activity, FileText, Clock, Lock, Unlock, AlertCircle, CheckCircle } from 'lucide-react';

interface Session {
  sessionId: string;
  userId: string;
  createdAt: string;
  lastActivity: string;
  ipAddress?: string;
  userAgent?: string;
  isActive: boolean;
}

interface AccessLog {
  logId: string;
  userId: string;
  action: string;
  resource: string;
  timestamp: string;
  success: boolean;
  details?: string;
}

interface DigitalSignature {
  nodeId: string;
  signature: string;
  publicKey: string;
  algorithm: string;
  timestamp: string;
  verified?: boolean;
}

export default function SecurityPage() {
  const { user, isAuthenticated, isLoading } = useAuth();
  const router = useRouter();
  const [activeTab, setActiveTab] = useState<'sessions' | 'logs' | 'signatures'>('sessions');
  const [loading, setLoading] = useState(false);
  
  // Sessions state
  const [sessions, setSessions] = useState<Session[]>([]);
  
  // Logs state
  const [accessLogs, setAccessLogs] = useState<AccessLog[]>([]);
  const [logFilter, setLogFilter] = useState<'all' | 'success' | 'failed'>('all');
  
  // Signatures state
  const [signatures, setSignatures] = useState<DigitalSignature[]>([]);
  const [showGenerateKey, setShowGenerateKey] = useState(false);
  const [generatedKeyPair, setGeneratedKeyPair] = useState<any>(null);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.push('/auth');
      return;
    }

    if (isAuthenticated && user?.id) {
      loadSecurityData();
    }
  }, [isAuthenticated, isLoading, user, router]);

  const loadSecurityData = async () => {
    await loadSessions();
    await loadAccessLogs();
    await loadSignatures();
  };

  const loadSessions = async () => {
    try {
      setLoading(true);
      // Note: This endpoint may not exist yet - using placeholder
      const response = await api.get('/auth/sessions');
      if (response?.success && response?.sessions) {
        setSessions(response.sessions);
      } else {
        // Mock data for demonstration
        setSessions([{
          sessionId: 'current-session',
          userId: user?.id || '',
          createdAt: new Date().toISOString(),
          lastActivity: new Date().toISOString(),
          isActive: true
        }]);
      }
    } catch (error) {
      console.error('Error loading sessions:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadAccessLogs = async () => {
    try {
      // Note: This endpoint may not exist yet - using placeholder
      const response = await api.get('/security/access-logs', { userId: user?.id, limit: 50 });
      if (response?.success && response?.logs) {
        setAccessLogs(response.logs);
      }
    } catch (error) {
      console.error('Error loading access logs:', error);
    }
  };

  const loadSignatures = async () => {
    try {
      // Load signed nodes for current user
      const response = await api.get('/signature/list', { userId: user?.id });
      if (response?.success && response?.signatures) {
        setSignatures(response.signatures);
      }
    } catch (error) {
      console.error('Error loading signatures:', error);
    }
  };

  const revokeSession = async (sessionId: string) => {
    try {
      await api.delete(`/auth/sessions/${sessionId}`);
      await loadSessions();
    } catch (error) {
      console.error('Error revoking session:', error);
    }
  };

  const generateKeyPair = async () => {
    try {
      const response = await api.post('/signature/generate-keypair', {});
      if (response?.success) {
        setGeneratedKeyPair(response);
        setShowGenerateKey(true);
      }
    } catch (error) {
      console.error('Error generating key pair:', error);
    }
  };

  const verifySignature = async (signature: DigitalSignature) => {
    try {
      const response = await api.post('/signature/verify-node', {
        nodeId: signature.nodeId,
        publicKey: signature.publicKey
      });
      
      if (response?.success) {
        setSignatures(prev => 
          prev.map(sig => 
            sig.nodeId === signature.nodeId 
              ? { ...sig, verified: response.isValid } 
              : sig
          )
        );
      }
    } catch (error) {
      console.error('Error verifying signature:', error);
    }
  };

  const filteredLogs = logFilter === 'all' 
    ? accessLogs 
    : accessLogs.filter(log => 
        logFilter === 'success' ? log.success : !log.success
      );

  if (loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-gray-900 p-8">
        <div className="max-w-7xl mx-auto">
          <div className="text-center py-20">
            <div className="animate-spin rounded-full h-16 w-16 border-b-2 border-blue-400 mx-auto"></div>
            <p className="text-gray-400 mt-4">Loading security data...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-gray-900 p-8">
      <div className="max-w-7xl mx-auto space-y-6">
        {/* Header */}
        <div>
          <h1 className="text-4xl font-bold bg-gradient-to-r from-purple-400 to-pink-400 bg-clip-text text-transparent flex items-center gap-3">
            <Shield className="w-10 h-10 text-purple-400" />
            Security Center
          </h1>
          <p className="text-gray-400 mt-2">
            Manage your sessions, access logs, and cryptographic signatures
          </p>
        </div>

        {/* Tabs */}
        <div className="flex space-x-4 border-b border-gray-700">
          {[
            { id: 'sessions', label: 'Active Sessions', icon: Activity },
            { id: 'logs', label: 'Access Logs', icon: FileText },
            { id: 'signatures', label: 'Digital Signatures', icon: Key }
          ].map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id as any)}
              className={`flex items-center gap-2 px-4 py-2 border-b-2 transition-colors ${
                activeTab === tab.id
                  ? 'border-purple-400 text-purple-400'
                  : 'border-transparent text-gray-400 hover:text-gray-300'
              }`}
            >
              <tab.icon className="w-4 h-4" />
              {tab.label}
            </button>
          ))}
        </div>

        {/* Sessions Tab */}
        {activeTab === 'sessions' && (
          <Card className="p-6">
            <div className="flex items-center justify-between mb-6">
              <h3 className="text-xl font-semibold text-gray-200">
                🔐 Active Sessions ({sessions.length})
              </h3>
              <button
                onClick={loadSessions}
                className="px-4 py-2 bg-purple-600 hover:bg-purple-700 rounded-lg transition-colors text-sm"
              >
                🔄 Refresh
              </button>
            </div>

            <div className="space-y-3">
              {sessions.length > 0 ? (
                sessions.map((session) => (
                  <div
                    key={session.sessionId}
                    className="p-4 bg-gray-800/50 rounded-lg border border-gray-700"
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-2">
                          <span className={`w-2 h-2 rounded-full ${session.isActive ? 'bg-green-400' : 'bg-gray-500'}`}></span>
                          <span className="font-medium text-gray-200">
                            {session.sessionId === 'current-session' ? 'Current Session' : `Session ${session.sessionId.substring(0, 8)}`}
                          </span>
                        </div>
                        <div className="space-y-1 text-sm text-gray-400">
                          <div className="flex items-center gap-2">
                            <Clock className="w-3 h-3" />
                            Created: {new Date(session.createdAt).toLocaleString()}
                          </div>
                          <div className="flex items-center gap-2">
                            <Activity className="w-3 h-3" />
                            Last Active: {new Date(session.lastActivity).toLocaleString()}
                          </div>
                          {session.ipAddress && (
                            <div>📍 IP: {session.ipAddress}</div>
                          )}
                        </div>
                      </div>
                      {session.sessionId !== 'current-session' && (
                        <button
                          onClick={() => revokeSession(session.sessionId)}
                          className="px-3 py-1 bg-red-600 hover:bg-red-700 rounded text-sm transition-colors"
                        >
                          Revoke
                        </button>
                      )}
                    </div>
                  </div>
                ))
              ) : (
                <div className="text-center py-12 text-gray-500">
                  <Activity className="w-16 h-16 mx-auto mb-4 opacity-50" />
                  <p>No active sessions found</p>
                </div>
              )}
            </div>
          </Card>
        )}

        {/* Access Logs Tab */}
        {activeTab === 'logs' && (
          <Card className="p-6">
            <div className="flex items-center justify-between mb-6">
              <h3 className="text-xl font-semibold text-gray-200">
                📋 Access Logs ({filteredLogs.length})
              </h3>
              <div className="flex gap-2">
                <select
                  value={logFilter}
                  onChange={(e) => setLogFilter(e.target.value as any)}
                  className="px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-gray-200"
                >
                  <option value="all">All</option>
                  <option value="success">Successful</option>
                  <option value="failed">Failed</option>
                </select>
                <button
                  onClick={loadAccessLogs}
                  className="px-4 py-2 bg-purple-600 hover:bg-purple-700 rounded-lg transition-colors text-sm"
                >
                  🔄 Refresh
                </button>
              </div>
            </div>

            <div className="space-y-2 max-h-[600px] overflow-y-auto">
              {filteredLogs.length > 0 ? (
                filteredLogs.map((log) => (
                  <div
                    key={log.logId}
                    className="p-3 bg-gray-800/50 rounded-lg border border-gray-700"
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex items-start gap-3 flex-1">
                        <div className="mt-1">
                          {log.success ? (
                            <CheckCircle className="w-4 h-4 text-green-400" />
                          ) : (
                            <AlertCircle className="w-4 h-4 text-red-400" />
                          )}
                        </div>
                        <div className="flex-1">
                          <div className="flex items-center gap-2 mb-1">
                            <span className="font-medium text-gray-200">{log.action}</span>
                            <span className="text-xs text-gray-500">•</span>
                            <span className="text-sm text-gray-400">{log.resource}</span>
                          </div>
                          {log.details && (
                            <p className="text-xs text-gray-500">{log.details}</p>
                          )}
                        </div>
                      </div>
                      <div className="text-xs text-gray-500 text-right">
                        {new Date(log.timestamp).toLocaleString()}
                      </div>
                    </div>
                  </div>
                ))
              ) : (
                <div className="text-center py-12 text-gray-500">
                  <FileText className="w-16 h-16 mx-auto mb-4 opacity-50" />
                  <p>No access logs found</p>
                  <p className="text-sm mt-2">Your activity will be logged here</p>
                </div>
              )}
            </div>
          </Card>
        )}

        {/* Digital Signatures Tab */}
        {activeTab === 'signatures' && (
          <Card className="p-6">
            <div className="flex items-center justify-between mb-6">
              <h3 className="text-xl font-semibold text-gray-200">
                🔐 Digital Signatures
              </h3>
              <button
                onClick={generateKeyPair}
                className="px-4 py-2 bg-purple-600 hover:bg-purple-700 rounded-lg transition-colors text-sm"
              >
                + Generate Key Pair
              </button>
            </div>

            {/* Key Pair Generation Modal */}
            {showGenerateKey && generatedKeyPair && (
              <div className="mb-6 p-4 bg-purple-900/20 border border-purple-700 rounded-lg">
                <div className="flex items-center justify-between mb-4">
                  <h4 className="font-semibold text-purple-400">New Key Pair Generated</h4>
                  <button
                    onClick={() => setShowGenerateKey(false)}
                    className="text-gray-500 hover:text-gray-300"
                  >
                    ✕
                  </button>
                </div>
                <div className="space-y-3">
                  <div>
                    <label className="block text-sm text-gray-400 mb-1">Public Key</label>
                    <div className="p-2 bg-gray-900/50 rounded font-mono text-xs text-gray-300 break-all">
                      {generatedKeyPair.publicKey}
                    </div>
                  </div>
                  <div>
                    <label className="block text-sm text-gray-400 mb-1">Private Key</label>
                    <div className="p-2 bg-gray-900/50 rounded font-mono text-xs text-gray-300 break-all">
                      {generatedKeyPair.privateKey}
                    </div>
                  </div>
                  <div className="flex items-center gap-2 text-sm text-orange-400">
                    <AlertCircle className="w-4 h-4" />
                    <span>Save your private key securely - it cannot be recovered</span>
                  </div>
                </div>
              </div>
            )}

            {/* Signatures List */}
            <div className="space-y-3">
              {signatures.length > 0 ? (
                signatures.map((sig) => (
                  <div
                    key={sig.nodeId}
                    className="p-4 bg-gray-800/50 rounded-lg border border-gray-700"
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-2">
                          <Key className="w-4 h-4 text-purple-400" />
                          <span className="font-medium text-gray-200">Node: {sig.nodeId.substring(0, 20)}...</span>
                        </div>
                        <div className="space-y-1 text-xs text-gray-400">
                          <div>Algorithm: {sig.algorithm}</div>
                          <div>Signed: {new Date(sig.timestamp).toLocaleString()}</div>
                          {sig.verified !== undefined && (
                            <div className={`flex items-center gap-1 ${sig.verified ? 'text-green-400' : 'text-red-400'}`}>
                              {sig.verified ? (
                                <>
                                  <CheckCircle className="w-3 h-3" />
                                  Verified
                                </>
                              ) : (
                                <>
                                  <AlertCircle className="w-3 h-3" />
                                  Invalid
                                </>
                              )}
                            </div>
                          )}
                        </div>
                      </div>
                      <button
                        onClick={() => verifySignature(sig)}
                        className="px-3 py-1 bg-purple-600 hover:bg-purple-700 rounded text-sm transition-colors"
                      >
                        Verify
                      </button>
                    </div>
                  </div>
                ))
              ) : (
                <div className="text-center py-12 text-gray-500">
                  <Key className="w-16 h-16 mx-auto mb-4 opacity-50" />
                  <p>No digital signatures found</p>
                  <p className="text-sm mt-2">Sign nodes and edges to verify their authenticity</p>
                </div>
              )}
            </div>
          </Card>
        )}
      </div>
    </div>
  );
}

