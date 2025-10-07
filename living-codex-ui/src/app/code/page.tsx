import React from 'react'
import Link from 'next/link'
import CodeIDE from '@/components/ui/CodeIDE'

interface FileNode {
  id: string
  name: string
  type: string
  relativePath: string
  absolutePath: string
  size: number
  lastModified: string
}

async function getFileNodes(): Promise<FileNode[]> {
  try {
    const allFiles: FileNode[] = []
    let offset = 0
    const limit = 1000
    let hasMoreFiles = true

    console.log('Starting to fetch all files with pagination...')

    while (hasMoreFiles) {
      const url = `http://localhost:5002/filesystem/files?limit=${limit}&offset=${offset}`
      console.log(`Fetching files: offset=${offset}, limit=${limit}`)
      
      const response = await fetch(url, {
        cache: 'no-store', // Always fetch fresh data
        headers: {
          'Accept': 'application/json',
        },
      })

      if (!response.ok) {
        console.error(`Failed to fetch files at offset ${offset}: ${response.status} ${response.statusText}`)
        break
      }

      const data = await response.json()
      
      if (data && data.success && Array.isArray(data.files)) {
        const files = data.files as FileNode[]
        allFiles.push(...files)
        
        console.log(`Fetched ${files.length} files (total so far: ${allFiles.length})`)
        
        // If we got fewer files than the limit, we've reached the end
        if (files.length < limit) {
          hasMoreFiles = false
          console.log('Reached end of files')
        } else {
          offset += limit
        }
      } else {
        console.error('Invalid data format received from server:', data)
        break
      }
    }

    console.log(`Finished fetching all files. Total: ${allFiles.length}`)
    return allFiles
  } catch (error) {
    console.error('Error fetching file nodes:', error)
    return []
  }
}

export default async function CodePage() {
  const fileNodes = await getFileNodes()
  const fileCount = fileNodes.filter(n => n.type && n.type.startsWith('codex.file')).length
  // Count actual directories by looking at unique first-level paths
  const folderCount = new Set(fileNodes.map(n => n.relativePath.split('/')[0]).filter(folder => folder && !folder.startsWith('.'))).size

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
              <span className="ml-2 text-xs">
                {fileCount} files • {folderCount} folders
              </span>
            </div>
            
            <Link
              href="/"
              className="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100 transition-colors"
            >
              ← Back to Dashboard
            </Link>
          </div>
        </div>
      </header>

      {/* IDE */}
      <main className="flex-1 overflow-hidden">
        <CodeIDE initialFileNodes={fileNodes} />
      </main>
    </div>
  )
}



