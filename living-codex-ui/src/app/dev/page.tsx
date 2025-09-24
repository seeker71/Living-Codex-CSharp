'use client';

import { HotReloadDashboard } from '@/components/dev/HotReloadDashboard';
import { useAuth } from '@/contexts/AuthContext';
import { useEffect } from 'react';
import { useRouter } from 'next/navigation';

export default function DevPage() {
  const { user, isAuthenticated } = useAuth();
  const router = useRouter();

  // Redirect non-authenticated users
  useEffect(() => {
    if (!isAuthenticated) {
      router.push('/auth');
    }
  }, [isAuthenticated, router]);

  if (!isAuthenticated) {
    return null;
  }

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <div className="max-w-6xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">🛠️ Developer Dashboard</h1>
          <p className="text-gray-600 dark:text-gray-300">
            Real-time hot-reload monitoring, AI-driven component generation, and development tools
          </p>
        </div>

        {/* Hot Reload Dashboard */}
        <div className="mb-8">
          <HotReloadDashboard />
        </div>

        {/* Development Tools */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* AI Tools */}
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">🤖 AI Development Tools</h3>
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-3">
                <div className="text-center p-4 bg-blue-50 rounded-lg">
                  <div className="text-2xl mb-2">🎨</div>
                  <div className="text-sm font-medium text-blue-900">Component Generation</div>
                  <div className="text-xs text-blue-700">GPT-5 Codex</div>
                </div>
                <div className="text-center p-4 bg-green-50 rounded-lg">
                  <div className="text-2xl mb-2">🧠</div>
                  <div className="text-sm font-medium text-green-900">Concept Extraction</div>
                  <div className="text-xs text-green-700">GPT-5 Mini</div>
                </div>
              </div>
              
              <div className="text-sm text-gray-600">
                <p className="mb-2">Available AI Providers:</p>
                <div className="flex flex-wrap gap-2">
                  <span className="px-2 py-1 bg-blue-100 text-blue-800 rounded-md text-xs">OpenAI</span>
                  <span className="px-2 py-1 bg-purple-100 text-purple-800 rounded-md text-xs">Cursor</span>
                  <span className="px-2 py-1 bg-gray-100 text-gray-800 rounded-md text-xs">Ollama</span>
                </div>
              </div>
            </div>
          </div>

          {/* System Info */}
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">⚙️ System Information</h3>
            <div className="space-y-3">
              <div className="flex justify-between">
                <span className="text-gray-600 dark:text-gray-300">Backend Mode</span>
                <span className="font-medium text-green-600">Hot Reload (dotnet watch)</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-600 dark:text-gray-300">Frontend Mode</span>
                <span className="font-medium text-green-600">Next.js Dev Server</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-600 dark:text-gray-300">Spec Watching</span>
                <span className="font-medium text-blue-600">Enabled</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-600 dark:text-gray-300">AI Integration</span>
                <span className="font-medium text-purple-600">Multi-Provider</span>
              </div>
            </div>
          </div>
        </div>

        {/* Quick Actions */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">⚡ Quick Actions</h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
            <button
              onClick={() => window.open('/news', '_blank')}
              className="p-4 text-center bg-blue-50 hover:bg-blue-100 rounded-lg transition-colors"
            >
              <div className="text-2xl mb-1">📰</div>
              <div className="text-sm font-medium">Test News Page</div>
            </button>
            <button
              onClick={() => window.open('/people', '_blank')}
              className="p-4 text-center bg-green-50 hover:bg-green-100 rounded-lg transition-colors"
            >
              <div className="text-2xl mb-1">🌍</div>
              <div className="text-sm font-medium">Test People Page</div>
            </button>
            <button
              onClick={() => window.open('/create', '_blank')}
              className="p-4 text-center bg-purple-50 hover:bg-purple-100 rounded-lg transition-colors"
            >
              <div className="text-2xl mb-1">✨</div>
              <div className="text-sm font-medium">Test Create Page</div>
            </button>
            <button
              onClick={() => window.open('/ontology', '_blank')}
              className="p-4 text-center bg-orange-50 hover:bg-orange-100 rounded-lg transition-colors"
            >
              <div className="text-2xl mb-1">🧠</div>
              <div className="text-sm font-medium">Test Ontology</div>
            </button>
          </div>
        </div>

        {/* Development Notes */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">📝 Development Notes</h3>
          <div className="text-sm text-gray-600 space-y-2">
            <p>
              <strong>Hot Reload Infrastructure:</strong> Both backend (dotnet watch) and frontend (Next.js) 
              support hot reloading. The system can detect file changes and trigger AI-driven component regeneration.
            </p>
            <p>
              <strong>AI Integration:</strong> Components can be regenerated using GPT-5 Codex for code generation 
              and GPT-5 Mini for analysis tasks. Cursor API is also supported as a secondary provider.
            </p>
            <p>
              <strong>Spec-Driven Development:</strong> Changes to specification files can automatically 
              trigger UI component updates, enabling true spec-to-code workflows.
            </p>
            <p className="text-xs text-gray-500 mt-3">
              💡 This dashboard demonstrates the self-modifying UI capabilities of the Living Codex system.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
