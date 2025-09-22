'use client'

import React from 'react'
import CodeIDE from '@/components/ui/CodeIDE'

export default function CodePage() {
  const handleFileSave = async (content: string, nodeId: string) => {
    console.log(`File saved: ${nodeId}`, { contentLength: content.length })
    // In a real app, you might want to show a toast notification or update some global state
  }

  return (
    <div className="h-screen flex flex-col">
      {/* Header */}
      <header className="flex-shrink-0 bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-700 px-6 py-4">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">
              Code Editor
            </h1>
            <p className="text-gray-600 dark:text-gray-400 mt-1">
              Edit project files directly through the Living Codex node system
            </p>
          </div>
          
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
              <div className="w-2 h-2 bg-green-500 rounded-full"></div>
              <span>File system connected</span>
            </div>
            
            <a
              href="/"
              className="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100 transition-colors"
            >
              ← Back to Dashboard
            </a>
          </div>
        </div>
      </header>

      {/* IDE */}
      <main className="flex-1 overflow-hidden">
        <CodeIDE onFileSave={handleFileSave} />
      </main>
    </div>
  )
}



