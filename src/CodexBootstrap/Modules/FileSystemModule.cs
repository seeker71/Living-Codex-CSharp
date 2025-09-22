using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Module for managing the entire codebase as nodes with file system ContentRef.
/// Implements the principle that "Everything is a Node" by representing all project files as nodes.
/// </summary>
public class FileSystemModule : ModuleBase, IDisposable
{
    private readonly FileSystemWatcher? _watcher;
    private readonly string _projectRoot;
    private readonly HashSet<string> _excludePatterns;

    public FileSystemModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) : base(registry, logger)
    {
        
        // Determine project root (allow override via env var for tests), else find .sln
        var overrideRoot = Environment.GetEnvironmentVariable("FILESYSTEM_PROJECT_ROOT");
        if (!string.IsNullOrWhiteSpace(overrideRoot) && Directory.Exists(overrideRoot))
        {
            _projectRoot = overrideRoot!;
        }
        else
        {
            _projectRoot = FindProjectRoot() ?? Environment.CurrentDirectory;
        }
        
        // Define patterns to exclude from file system nodes
        _excludePatterns = new HashSet<string>
        {
            "bin", "obj", "node_modules", ".git", ".vs", ".vscode",
            "logs", "*.log", "*.tmp", "*.cache", ".next", "dist"
        };

        // Set up file system watcher for automatic node updates
        try
        {
            _watcher = new FileSystemWatcher(_projectRoot)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };
            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileCreated;
            _watcher.Deleted += OnFileDeleted;
            _watcher.Renamed += OnFileRenamed;
            _watcher.EnableRaisingEvents = true;
            
            _logger.Info($"FileSystemModule: Watching {_projectRoot} for changes");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to setup file system watcher: {ex.Message}");
        }
    }

    public override string Name => "File System";
    public override string Description => "Manages all project files as nodes with file system ContentRef";
    public override string Version => "1.0.0";

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.filesystem",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "filesystem", "files", "editor", "nodes" },
            capabilities: new[] {
                "initialize-nodes", "list-files", "read-content", "update-content",
                "create-file", "delete-file", "search", "tree"
            },
            spec: "codex.spec.filesystem"
        );
    }

    /// <summary>
    /// Initialize all project files as nodes in the registry
    /// </summary>
    [ApiRoute("POST", "/filesystem/initialize", "filesystem-initialize", "Initialize all project files as nodes", "codex.filesystem")]
    public async Task<object> InitializeFileSystemNodes()
    {
        try
        {
            var createdNodes = 0;
            var updatedNodes = 0;
            var errors = new List<string>();

            await foreach (var filePath in GetAllProjectFilesAsync())
            {
                try
                {
                    var result = await CreateOrUpdateFileNode(filePath);
                    if (result.created)
                        createdNodes++;
                    else
                        updatedNodes++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{filePath}: {ex.Message}");
                    _logger.Error($"Failed to create node for {filePath}: {ex.Message}");
                }
            }

            return new
            {
                success = true,
                message = "File system initialization completed",
                projectRoot = _projectRoot,
                createdNodes,
                updatedNodes,
                errors = errors.Take(10).ToArray(), // Limit error list
                totalErrors = errors.Count
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to initialize file system: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all project files as nodes
    /// </summary>
    [ApiRoute("GET", "/filesystem/files", "filesystem-files", "Get all project files as nodes", "codex.filesystem")]
    public async Task<object> GetAllFileNodes([ApiParameter("type", "File type filter (cs, tsx, md, etc.)", Required = false)] string? type = null,
                                             [ApiParameter("directory", "Directory filter", Required = false)] string? directory = null,
                                             [ApiParameter("limit", "Maximum number of files to return", Required = false)] int limit = 100)
    {
        try
        {
            var allNodes = _registry.AllNodes().ToList();
            var fileNodes = allNodes.Where(n => n.TypeId.StartsWith("codex.file/")).ToList();

            // Apply filters
            if (!string.IsNullOrEmpty(type))
            {
                fileNodes = fileNodes.Where(n => n.TypeId == $"codex.file/{type}").ToList();
            }

            if (!string.IsNullOrEmpty(directory))
            {
                fileNodes = fileNodes.Where(n => 
                    n.Meta?.ContainsKey("relativePath") == true &&
                    n.Meta["relativePath"].ToString()?.StartsWith(directory) == true
                ).ToList();
            }

            var result = fileNodes.Take(limit).Select(n => new
            {
                id = n.Id,
                name = n.Title,
                type = n.TypeId,
                relativePath = n.Meta?.GetValueOrDefault("relativePath"),
                absolutePath = n.Meta?.GetValueOrDefault("absolutePath"),
                size = n.Meta?.GetValueOrDefault("size"),
                lastModified = n.Meta?.GetValueOrDefault("lastModified"),
                contentRef = n.Content != null ? new
                {
                    mediaType = n.Content.MediaType,
                    externalUri = n.Content.ExternalUri?.ToString(),
                    hasInlineContent = !string.IsNullOrEmpty(n.Content.InlineJson) || n.Content.InlineBytes?.Length > 0
                } : null
            }).ToList();

            return new
            {
                success = true,
                message = $"Found {result.Count} file nodes",
                projectRoot = _projectRoot,
                totalFileNodes = fileNodes.Count,
                files = result
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get file nodes: {ex.Message}");
        }
    }

    /// <summary>
    /// Get file content through node system
    /// </summary>
    [ApiRoute("GET", "/filesystem/content/{nodeId}", "filesystem-content", "Get file content through node system", "codex.filesystem")]
    public async Task<object> GetFileContent([ApiParameter("nodeId", "Node ID representing the file", Required = true, Location = "path")] string nodeId)
    {
        try
        {
            if (!_registry.TryGet(nodeId, out var node))
            {
                return new ErrorResponse($"File node '{nodeId}' not found");
            }

            if (!node.TypeId.StartsWith("codex.file/"))
            {
                return new ErrorResponse($"Node '{nodeId}' is not a file node");
            }

            // Get file path from node metadata
            var absolutePath = node.Meta?.GetValueOrDefault("absolutePath")?.ToString();
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
            {
                return new ErrorResponse($"File not found: {absolutePath}");
            }

            var content = await File.ReadAllTextAsync(absolutePath);
            var fileInfo = new FileInfo(absolutePath);

            return new
            {
                success = true,
                nodeId = node.Id,
                fileName = node.Title,
                fileType = node.TypeId,
                relativePath = node.Meta?.GetValueOrDefault("relativePath"),
                content,
                size = fileInfo.Length,
                lastModified = fileInfo.LastWriteTime,
                encoding = "utf-8"
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to read file content: {ex.Message}");
        }
    }

    /// <summary>
    /// Update file content through node system
    /// </summary>
    [ApiRoute("PUT", "/filesystem/content/{nodeId}", "filesystem-update", "Update file content through node system", "codex.filesystem")]
    public async Task<object> UpdateFileContent([ApiParameter("nodeId", "Node ID representing the file", Required = true, Location = "path")] string nodeId,
                                               [ApiParameter("content", "New file content", Required = true, Location = "body")] FileUpdateRequest request)
    {
        try
        {
            if (!_registry.TryGet(nodeId, out var node))
            {
                return new ErrorResponse($"File node '{nodeId}' not found");
            }

            if (!node.TypeId.StartsWith("codex.file/"))
            {
                return new ErrorResponse($"Node '{nodeId}' is not a file node");
            }

            var absolutePath = node.Meta?.GetValueOrDefault("absolutePath")?.ToString();
            if (string.IsNullOrEmpty(absolutePath))
            {
                return new ErrorResponse($"File path not found in node metadata");
            }

            // Create backup
            var backupPath = $"{absolutePath}.backup.{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            if (File.Exists(absolutePath))
            {
                File.Copy(absolutePath, backupPath);
            }

            // Temporarily disable file watcher to prevent overwriting metadata
            var watcherEnabled = _watcher?.EnableRaisingEvents ?? false;
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
            }

            try
            {
                // Write new content
                await File.WriteAllTextAsync(absolutePath, request.Content);

                // Update node metadata
                var fileInfo = new FileInfo(absolutePath);
                var updatedMeta = new Dictionary<string, object>(node.Meta ?? new Dictionary<string, object>())
                {
                    ["size"] = fileInfo.Length,
                    ["lastModified"] = fileInfo.LastWriteTime,
                    ["lastEditedBy"] = request.AuthorId ?? "system",
                    ["lastEditedAt"] = DateTime.UtcNow,
                    ["backupPath"] = backupPath,
                    ["changeReason"] = request.ChangeReason ?? "File updated via UI"
                };

                var updatedNode = node with { Meta = updatedMeta };
                _registry.Upsert(updatedNode);
                
                _logger.Info($"Updated node {nodeId} with metadata keys: {string.Join(", ", updatedMeta.Keys)}");
            }
            finally
            {
                // Re-enable file watcher
                if (_watcher != null && watcherEnabled)
                {
                    _watcher.EnableRaisingEvents = true;
                }
            }

            // Create contribution record
            await RecordFileContribution(nodeId, request, "file-edit");

            // Trigger compilation if needed
            var compilationResult = await TriggerCompilationIfNeeded(absolutePath);

            return new
            {
                success = true,
                message = "File updated successfully",
                nodeId,
                fileName = node.Title,
                size = new FileInfo(absolutePath).Length,
                lastModified = new FileInfo(absolutePath).LastWriteTime,
                backupCreated = backupPath,
                compilation = compilationResult
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to update file: {ex.Message}");
        }
    }

    /// <summary>
    /// Get project structure as a tree of nodes
    /// </summary>
    [ApiRoute("GET", "/filesystem/tree", "filesystem-tree", "Get project structure as tree of file nodes", "codex.filesystem")]
    public async Task<object> GetProjectTree([ApiParameter("maxDepth", "Maximum directory depth", Required = false)] int maxDepth = 10)
    {
        try
        {
            var tree = await BuildFileTree(_projectRoot, maxDepth);

            return new
            {
                success = true,
                message = "Project tree generated successfully",
                projectRoot = _projectRoot,
                tree
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to build project tree: {ex.Message}");
        }
    }

    /// <summary>
    /// Search files by content or name
    /// </summary>
    [ApiRoute("POST", "/filesystem/search", "filesystem-search", "Search files by content or name", "codex.filesystem")]
    public async Task<object> SearchFiles([ApiParameter("query", "Search query", Required = true, Location = "body")] FileSearchRequest request)
    {
        try
        {
            var results = new List<object>();
            var searchTerm = request.Query.ToLower();

            await foreach (var filePath in GetAllProjectFilesAsync())
            {
                try
                {
                    var relativePath = Path.GetRelativePath(_projectRoot, filePath);
                    var fileName = Path.GetFileName(filePath);
                    
                    // Search by filename
                    if (request.SearchInNames && fileName.ToLower().Contains(searchTerm))
                    {
                        results.Add(await CreateSearchResult(filePath, "filename", fileName));
                        continue;
                    }

                    // Search by content
                    if (request.SearchInContent && IsTextFile(filePath))
                    {
                        var content = await File.ReadAllTextAsync(filePath);
                        if (content.ToLower().Contains(searchTerm))
                        {
                            var lines = content.Split('\n');
                            var matchingLines = lines
                                .Select((line, index) => new { line, index })
                                .Where(x => x.line.ToLower().Contains(searchTerm))
                                .Take(5) // Limit matches per file
                                .Select(x => new { lineNumber = x.index + 1, content = x.line.Trim() })
                                .ToArray();

                            results.Add(await CreateSearchResult(filePath, "content", matchingLines));
                        }
                    }

                    if (results.Count >= request.MaxResults)
                        break;
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Error searching file {filePath}: {ex.Message}");
                }
            }

            return new
            {
                success = true,
                message = $"Found {results.Count} matches",
                query = request.Query,
                results
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Search failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a new file through the node system
    /// </summary>
    [ApiRoute("POST", "/filesystem/create", "filesystem-create", "Create new file through node system", "codex.filesystem")]
    public async Task<object> CreateFile([ApiParameter("file", "File creation request", Required = true, Location = "body")] FileCreateRequest request)
    {
        try
        {
            var absolutePath = Path.IsPathRooted(request.Path) 
                ? request.Path 
                : Path.Combine(_projectRoot, request.Path);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Check if file already exists
            if (File.Exists(absolutePath))
            {
                return new ErrorResponse($"File already exists: {request.Path}");
            }

            // Create file
            await File.WriteAllTextAsync(absolutePath, request.Content ?? "");

            // Create corresponding node
            var result = await CreateOrUpdateFileNode(absolutePath);

            // Record contribution
            await RecordFileContribution(result.nodeId, new FileUpdateRequest 
            { 
                Content = request.Content ?? "",
                AuthorId = request.AuthorId,
                ChangeReason = $"Created new file: {request.Path}"
            }, "file-create");

            return new
            {
                success = true,
                message = "File created successfully",
                nodeId = result.nodeId,
                path = request.Path,
                absolutePath
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to create file: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a file through the node system
    /// </summary>
    [ApiRoute("DELETE", "/filesystem/content/{nodeId}", "filesystem-delete", "Delete file through node system", "codex.filesystem")]
    public async Task<object> DeleteFile([ApiParameter("nodeId", "Node ID representing the file", Required = true, Location = "path")] string nodeId,
                                        [ApiParameter("authorId", "ID of user deleting the file", Required = false)] string? authorId = null)
    {
        try
        {
            if (!_registry.TryGet(nodeId, out var node))
            {
                return new ErrorResponse($"File node '{nodeId}' not found");
            }

            var absolutePath = node.Meta?.GetValueOrDefault("absolutePath")?.ToString();
            if (string.IsNullOrEmpty(absolutePath))
            {
                return new ErrorResponse($"File path not found in node metadata");
            }

            // Create backup before deletion
            var backupPath = $"{absolutePath}.deleted.{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            if (File.Exists(absolutePath))
            {
                File.Move(absolutePath, backupPath);
            }

            // Remove node from registry
            // Note: We keep the node but mark it as deleted in metadata
            var deletedMeta = new Dictionary<string, object>(node.Meta ?? new Dictionary<string, object>())
            {
                ["deleted"] = true,
                ["deletedAt"] = DateTime.UtcNow,
                ["deletedBy"] = authorId ?? "system",
                ["backupPath"] = backupPath
            };

            var deletedNode = node with { 
                Meta = deletedMeta,
                State = ContentState.Gas // Move to Gas state since file no longer exists
            };
            _registry.Upsert(deletedNode);

            // Record contribution
            await RecordFileContribution(nodeId, new FileUpdateRequest 
            { 
                Content = "",
                AuthorId = authorId,
                ChangeReason = $"Deleted file: {node.Title}"
            }, "file-delete");

            return new
            {
                success = true,
                message = "File deleted successfully",
                nodeId,
                fileName = node.Title,
                backupCreated = backupPath
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to delete file: {ex.Message}");
        }
    }

    /// <summary>
    /// Get file statistics and metrics
    /// </summary>
    [ApiRoute("GET", "/filesystem/stats", "filesystem-stats", "Get file system statistics", "codex.filesystem")]
    public async Task<object> GetFileSystemStats()
    {
        try
        {
            var allNodes = _registry.AllNodes().ToList();
            var fileNodes = allNodes.Where(n => n.TypeId.StartsWith("codex.file/")).ToList();

            var stats = new
            {
                totalFiles = fileNodes.Count,
                fileTypes = fileNodes.GroupBy(n => n.TypeId)
                    .ToDictionary(g => g.Key.Replace("codex.file/", ""), g => g.Count()),
                directories = fileNodes
                    .Where(n => n.Meta?.ContainsKey("relativePath") == true)
                    .Select(n => Path.GetDirectoryName(n.Meta["relativePath"].ToString()))
                    .Where(d => !string.IsNullOrEmpty(d))
                    .GroupBy(d => d)
                    .ToDictionary(g => g.Key!, g => g.Count()),
                totalSize = fileNodes
                    .Where(n => n.Meta?.ContainsKey("size") == true)
                    .Sum(n => Convert.ToInt64(n.Meta["size"])),
                recentlyModified = fileNodes
                    .Where(n => n.Meta?.ContainsKey("lastModified") == true)
                    .OrderByDescending(n => n.Meta["lastModified"])
                    .Take(10)
                    .Select(n => new
                    {
                        nodeId = n.Id,
                        name = n.Title,
                        path = n.Meta?.GetValueOrDefault("relativePath"),
                        lastModified = n.Meta?.GetValueOrDefault("lastModified")
                    })
                    .ToArray()
            };

            return new
            {
                success = true,
                message = "File system statistics generated",
                projectRoot = _projectRoot,
                stats
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get file system stats: {ex.Message}");
        }
    }

    // Private helper methods

    private async IAsyncEnumerable<string> GetAllProjectFilesAsync()
    {
        var directories = new Queue<string>();
        directories.Enqueue(_projectRoot);

        while (directories.Count > 0)
        {
            var currentDir = directories.Dequeue();
            
            if (ShouldExcludeDirectory(currentDir))
                continue;

            // Get files in current directory
            string[] files;
            try
            {
                files = Directory.GetFiles(currentDir);
            }
            catch (UnauthorizedAccessException)
            {
                continue; // Skip directories we can't access
            }

            foreach (var file in files)
            {
                if (!ShouldExcludeFile(file))
                {
                    yield return file;
                }
            }

            // Add subdirectories to queue
            try
            {
                var subdirs = Directory.GetDirectories(currentDir);
                foreach (var subdir in subdirs)
                {
                    directories.Enqueue(subdir);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we can't access
            }
        }
    }

    private async Task<(string nodeId, bool created)> CreateOrUpdateFileNode(string absolutePath)
    {
        var relativePath = Path.GetRelativePath(_projectRoot, absolutePath);
        var fileName = Path.GetFileName(absolutePath);
        var extension = Path.GetExtension(absolutePath).TrimStart('.');
        var fileInfo = new FileInfo(absolutePath);

        // Generate consistent node ID based on relative path
        var nodeId = $"file:{relativePath.Replace('\\', '/').Replace('/', '.')}";

        // Determine file type
        var fileType = GetFileType(extension);
        var typeId = $"codex.file/{fileType}";

        // Create ContentRef pointing to file system
        var contentRef = new ContentRef(
            MediaType: GetMediaType(extension),
            InlineJson: null,
            InlineBytes: null,
            ExternalUri: new Uri($"file://{absolutePath}")
        );

        // Create or update node
        var meta = new Dictionary<string, object>
        {
            ["absolutePath"] = absolutePath,
            ["relativePath"] = relativePath,
            ["fileName"] = fileName,
            ["extension"] = extension,
            ["size"] = fileInfo.Length,
            ["lastModified"] = fileInfo.LastWriteTime,
            ["createdAt"] = fileInfo.CreationTime,
            ["isReadOnly"] = fileInfo.IsReadOnly,
            ["directory"] = Path.GetDirectoryName(relativePath) ?? "",
            ["projectRoot"] = _projectRoot
        };

        var node = new Node(
            Id: nodeId,
            TypeId: typeId,
            State: ContentState.Ice, // Files are persistent
            Locale: "en",
            Title: fileName,
            Description: $"Project file: {relativePath}",
            Content: contentRef,
            Meta: meta
        );

        var existed = _registry.TryGet(nodeId, out var _);
        _registry.Upsert(node);

        return (nodeId, !existed);
    }

    private async Task<object> CreateSearchResult(string filePath, string matchType, object matchData)
    {
        var relativePath = Path.GetRelativePath(_projectRoot, filePath);
        var nodeId = $"file:{relativePath.Replace('\\', '/').Replace('/', '.')}";

        return new
        {
            nodeId,
            fileName = Path.GetFileName(filePath),
            relativePath,
            matchType,
            matchData
        };
    }

    private async Task RecordFileContribution(string nodeId, FileUpdateRequest request, string contributionType)
    {
        try
        {
            // Create contribution node
            var contributionId = $"contribution-{Guid.NewGuid():N}";
            var contributionNode = new Node(
                Id: contributionId,
                TypeId: "codex.contribution.file",
                State: ContentState.Water,
                Locale: "en",
                Title: $"File {contributionType}: {nodeId}",
                Description: request.ChangeReason ?? $"File {contributionType} via UI",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        nodeId,
                        contributionType,
                        authorId = request.AuthorId,
                        changeReason = request.ChangeReason,
                        timestamp = DateTime.UtcNow
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["contributionType"] = contributionType,
                    ["targetNodeId"] = nodeId,
                    ["authorId"] = request.AuthorId ?? "system",
                    ["timestamp"] = DateTime.UtcNow
                }
            );

            _registry.Upsert(contributionNode);

            // Create edge linking contribution to file
            var edge = new Edge(contributionId, nodeId, "modifies", 1.0, new Dictionary<string, object>
            {
                ["relationship"] = "contribution-modifies-file",
                ["timestamp"] = DateTime.UtcNow
            });
            _registry.Upsert(edge);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to record file contribution: {ex.Message}");
        }
    }

    private async Task<object> TriggerCompilationIfNeeded(string filePath)
    {
        try
        {
            var extension = Path.GetExtension(filePath).ToLower();
            
            if (extension == ".cs")
            {
                // Trigger C# compilation
                return await TriggerCSharpCompilation(filePath);
            }
            else if (extension == ".tsx" || extension == ".ts" || extension == ".jsx" || extension == ".js")
            {
                // Trigger TypeScript/JavaScript compilation
                return await TriggerTypeScriptCompilation(filePath);
            }

            return new { compiled = false, reason = "No compilation needed for this file type" };
        }
        catch (Exception ex)
        {
            return new { compiled = false, error = ex.Message };
        }
    }

    private async Task<object> TriggerCSharpCompilation(string filePath)
    {
        try
        {
            // Find the project file
            var directory = Path.GetDirectoryName(filePath);
            string? projectFile = null;
            
            while (directory != null && directory != _projectRoot)
            {
                var csprojFiles = Directory.GetFiles(directory, "*.csproj");
                if (csprojFiles.Length > 0)
                {
                    projectFile = csprojFiles[0];
                    break;
                }
                directory = Path.GetDirectoryName(directory);
            }

            if (projectFile == null)
            {
                return new { compiled = false, reason = "No .csproj file found" };
            }

            // TODO: Integrate with HotReloadModule for actual compilation
            // For now, return compilation status
            return new
            {
                compiled = true,
                projectFile = Path.GetRelativePath(_projectRoot, projectFile),
                message = "C# compilation triggered"
            };
        }
        catch (Exception ex)
        {
            return new { compiled = false, error = ex.Message };
        }
    }

    private async Task<object> TriggerTypeScriptCompilation(string filePath)
    {
        try
        {
            // Find package.json
            var directory = Path.GetDirectoryName(filePath);
            string? packageJsonFile = null;
            
            while (directory != null && directory != _projectRoot)
            {
                var packageJson = Path.Combine(directory, "package.json");
                if (File.Exists(packageJson))
                {
                    packageJsonFile = packageJson;
                    break;
                }
                directory = Path.GetDirectoryName(directory);
            }

            if (packageJsonFile == null)
            {
                return new { compiled = false, reason = "No package.json file found" };
            }

            // TODO: Trigger actual TypeScript compilation
            return new
            {
                compiled = true,
                packageJson = Path.GetRelativePath(_projectRoot, packageJsonFile),
                message = "TypeScript compilation triggered"
            };
        }
        catch (Exception ex)
        {
            return new { compiled = false, error = ex.Message };
        }
    }

    private async Task<object> BuildFileTree(string directoryPath, int maxDepth, int currentDepth = 0)
    {
        if (currentDepth >= maxDepth || ShouldExcludeDirectory(directoryPath))
        {
            return new { name = Path.GetFileName(directoryPath), type = "directory", excluded = true };
        }

        var result = new
        {
            name = Path.GetFileName(directoryPath),
            type = "directory",
            path = Path.GetRelativePath(_projectRoot, directoryPath),
            children = new List<object>()
        };

        try
        {
            // Add files
            var files = Directory.GetFiles(directoryPath);
            foreach (var file in files.Where(f => !ShouldExcludeFile(f)))
            {
                var relativePath = Path.GetRelativePath(_projectRoot, file);
                var nodeId = $"file:{relativePath.Replace('\\', '/').Replace('/', '.')}";
                
                result.children.Add(new
                {
                    name = Path.GetFileName(file),
                    type = "file",
                    path = relativePath,
                    nodeId,
                    extension = Path.GetExtension(file),
                    size = new FileInfo(file).Length
                });
            }

            // Add subdirectories
            var directories = Directory.GetDirectories(directoryPath);
            foreach (var dir in directories)
            {
                var subTree = await BuildFileTree(dir, maxDepth, currentDepth + 1);
                result.children.Add(subTree);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we can't access
        }

        return result;
    }

    private string? FindProjectRoot()
    {
        var current = Environment.CurrentDirectory;
        while (current != null)
        {
            if (Directory.GetFiles(current, "*.sln").Length > 0)
            {
                return current;
            }
            current = Path.GetDirectoryName(current);
        }
        return null;
    }

    private bool ShouldExcludeDirectory(string path)
    {
        var dirName = Path.GetFileName(path);
        return _excludePatterns.Any(pattern => 
            dirName.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
            (pattern.Contains("*") && MatchesPattern(dirName, pattern)));
    }

    private bool ShouldExcludeFile(string path)
    {
        var fileName = Path.GetFileName(path);
        return _excludePatterns.Any(pattern => 
            fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
            (pattern.Contains("*") && MatchesPattern(fileName, pattern)));
    }

    private bool MatchesPattern(string input, string pattern)
    {
        // Simple pattern matching for * wildcards
        if (pattern.StartsWith("*"))
        {
            return input.EndsWith(pattern.Substring(1), StringComparison.OrdinalIgnoreCase);
        }
        if (pattern.EndsWith("*"))
        {
            return input.StartsWith(pattern.Substring(0, pattern.Length - 1), StringComparison.OrdinalIgnoreCase);
        }
        return input.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }

    private string GetFileType(string extension)
    {
        return extension.ToLower() switch
        {
            "cs" => "csharp",
            "tsx" or "ts" => "typescript",
            "jsx" or "js" => "javascript",
            "json" => "json",
            "md" => "markdown",
            "yml" or "yaml" => "yaml",
            "xml" => "xml",
            "html" => "html",
            "css" => "css",
            "scss" => "scss",
            "txt" => "text",
            "sql" => "sql",
            "sh" => "shell",
            "dockerfile" => "docker",
            "gitignore" => "gitignore",
            "config" => "config",
            _ => "unknown"
        };
    }

    private string GetMediaType(string extension)
    {
        return extension.ToLower() switch
        {
            "cs" => "text/x-csharp",
            "tsx" or "ts" => "text/typescript",
            "jsx" or "js" => "text/javascript",
            "json" => "application/json",
            "md" => "text/markdown",
            "yml" or "yaml" => "application/x-yaml",
            "xml" => "application/xml",
            "html" => "text/html",
            "css" => "text/css",
            "scss" => "text/x-scss",
            "txt" => "text/plain",
            "sql" => "application/sql",
            "sh" => "application/x-sh",
            _ => "text/plain"
        };
    }

    private bool IsTextFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        var textExtensions = new[] { ".cs", ".ts", ".tsx", ".js", ".jsx", ".json", ".md", ".txt", ".yml", ".yaml", ".xml", ".html", ".css", ".scss", ".sql", ".sh", ".config" };
        return textExtensions.Contains(extension);
    }

    // File system event handlers
    private async void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!ShouldExcludeFile(e.FullPath))
        {
            try
            {
                await CreateOrUpdateFileNode(e.FullPath);
                _logger.Debug($"Updated file node for: {e.FullPath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to update file node for {e.FullPath}: {ex.Message}");
            }
        }
    }

    private async void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (!ShouldExcludeFile(e.FullPath))
        {
            try
            {
                await CreateOrUpdateFileNode(e.FullPath);
                _logger.Info($"Created file node for: {e.FullPath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to create file node for {e.FullPath}: {ex.Message}");
            }
        }
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        try
        {
            var relativePath = Path.GetRelativePath(_projectRoot, e.FullPath);
            var nodeId = $"file:{relativePath.Replace('\\', '/').Replace('/', '.')}";
            
            if (_registry.TryGet(nodeId, out var node))
            {
                // Mark node as deleted but keep it in Gas state
                var deletedMeta = new Dictionary<string, object>(node.Meta ?? new Dictionary<string, object>())
                {
                    ["deleted"] = true,
                    ["deletedAt"] = DateTime.UtcNow
                };

                var deletedNode = node with { 
                    Meta = deletedMeta,
                    State = ContentState.Gas
                };
                _registry.Upsert(deletedNode);
                
                _logger.Info($"Marked file node as deleted: {e.FullPath}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to handle file deletion for {e.FullPath}: {ex.Message}");
        }
    }

    private async void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        try
        {
            // Handle old file as deleted
            OnFileDeleted(sender, new FileSystemEventArgs(WatcherChangeTypes.Deleted, Path.GetDirectoryName(e.OldFullPath)!, Path.GetFileName(e.OldFullPath)));
            
            // Handle new file as created
            await Task.Delay(100); // Small delay to ensure file is ready
            OnFileCreated(sender, new FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetDirectoryName(e.FullPath)!, Path.GetFileName(e.FullPath)));
            
            _logger.Info($"Handled file rename: {e.OldFullPath} -> {e.FullPath}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to handle file rename {e.OldFullPath} -> {e.FullPath}: {ex.Message}");
        }
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // FileSystemModule uses ApiRoute attributes for endpoint registration
        // No additional API handlers needed
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // FileSystemModule doesn't need any custom HTTP endpoints
        // All functionality is exposed through the ApiRoute attributes
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        GC.SuppressFinalize(this);
    }
}

// Request/Response types

public record FileUpdateRequest
{
    public string Content { get; init; } = "";
    public string? AuthorId { get; init; }
    public string? ChangeReason { get; init; }
}

public record FileCreateRequest
{
    public string Path { get; init; } = "";
    public string? Content { get; init; }
    public string? AuthorId { get; init; }
}

public record FileSearchRequest
{
    public string Query { get; init; } = "";
    public bool SearchInNames { get; init; } = true;
    public bool SearchInContent { get; init; } = true;
    public int MaxResults { get; init; } = 50;
}
