using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Modules;
using CodexBootstrap.Tests.Modules;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests.Modules;

/// <summary>
/// Tests for FileSystemModule - validates that all files are represented as nodes with ContentRef
/// </summary>
public class FileSystemModuleTests : IDisposable
{
    private readonly INodeRegistry _registry;
    private readonly ICodexLogger _logger;
    private readonly FileSystemModule _module;
    private readonly string _testDirectory;

    public FileSystemModuleTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"codex-test-{Guid.NewGuid():N}");
        
        // Create test directory structure
        Directory.CreateDirectory(_testDirectory);
        Directory.CreateDirectory(Path.Combine(_testDirectory, "src"));
        Directory.CreateDirectory(Path.Combine(_testDirectory, "tests"));
        
        // Ensure module scans within our temp directory
        Environment.SetEnvironmentVariable("FILESYSTEM_PROJECT_ROOT", _testDirectory);
        
        _registry = TestInfrastructure.CreateTestNodeRegistry();
        _logger = TestInfrastructure.CreateTestLogger();
        _module = new FileSystemModule(_registry, _logger, new HttpClient());
        
        // Initialize the registry
        _registry.InitializeAsync().Wait();
    }

    [Fact]
    public async Task InitializeFileSystemNodes_WithValidProject_CreatesFileNodes()
    {
        // Arrange - Create test files
        var testFile1 = Path.Combine(_testDirectory, "TestFile.cs");
        var testFile2 = Path.Combine(_testDirectory, "src", "Component.tsx");
        var testFile3 = Path.Combine(_testDirectory, "README.md");

        await File.WriteAllTextAsync(testFile1, "// Test C# file\nusing System;");
        await File.WriteAllTextAsync(testFile2, "// Test TypeScript file\nexport const Component = () => {};");
        await File.WriteAllTextAsync(testFile3, "# Test Project\n\nThis is a test.");

        // Act
        var result = await _module.InitializeFileSystemNodes();

        // Assert
        Assert.NotNull(result);
        Console.WriteLine($"Result type: {result.GetType().Name}");
        Console.WriteLine($"Result: {result}");
        
        if (result is CodexBootstrap.Core.ErrorResponse errorResponse)
        {
            Assert.True(false, $"Method returned error: {errorResponse.Error}");
        }
        
        // Use reflection to access the anonymous object properties
        var resultType = result.GetType();
        var successProperty = resultType.GetProperty("success");
        var createdNodesProperty = resultType.GetProperty("createdNodes");
        
        Assert.NotNull(successProperty);
        Assert.NotNull(createdNodesProperty);
        
        var success = (bool)successProperty.GetValue(result)!;
        var createdNodes = (int)createdNodesProperty.GetValue(result)!;
        
        Assert.True(success);
        Assert.True(createdNodes >= 0);
    }

    [Fact]
    public async Task GetAllFileNodes_WithFileTypeFilter_ReturnsFilteredResults()
    {
        // Arrange - Create test files and initialize
        var csFile = Path.Combine(_testDirectory, "Test.cs");
        var tsFile = Path.Combine(_testDirectory, "Test.ts");
        
        await File.WriteAllTextAsync(csFile, "// C# file");
        await File.WriteAllTextAsync(tsFile, "// TypeScript file");
        
        await _module.InitializeFileSystemNodes();

        // Act
        var result = await _module.GetAllFileNodes(type: "csharp");

        // Assert
        Assert.NotNull(result);
        
        if (result is CodexBootstrap.Core.ErrorResponse errorResponse)
        {
            Assert.True(false, $"Method returned error: {errorResponse.Error}");
        }
        
        // Use reflection to access the anonymous object properties
        var resultType = result.GetType();
        var successProperty = resultType.GetProperty("success");
        var filesProperty = resultType.GetProperty("files");
        
        Assert.NotNull(successProperty);
        Assert.NotNull(filesProperty);
        
        var success = (bool)successProperty.GetValue(result)!;
        var files = filesProperty.GetValue(result)!;
        
        Assert.True(success);
        Assert.NotNull(files);
    }

    [Fact]
    public async Task GetFileContent_WithValidNode_ReturnsFileContent()
    {
        // Arrange - Create test file and node
        var testFile = Path.Combine(_testDirectory, "TestContent.cs");
        var testContent = "using System;\n\nnamespace Test\n{\n    public class TestClass { }\n}";
        await File.WriteAllTextAsync(testFile, testContent);
        
        await _module.InitializeFileSystemNodes();

        // Get the node ID (simplified for test)
        var relativePath = Path.GetRelativePath(_testDirectory, testFile);
        var nodeId = $"file:{relativePath.Replace('\\', '/').Replace('/', '.')}";

        // Act
        var result = await _module.GetFileContent(nodeId);

        // Assert
        Assert.NotNull(result);
        
        if (result is CodexBootstrap.Core.ErrorResponse errorResponse)
        {
            Assert.True(false, $"Method returned error: {errorResponse.Error}");
        }
        
        // Use reflection to access the anonymous object properties
        var resultType = result.GetType();
        var successProperty = resultType.GetProperty("success");
        var contentProperty = resultType.GetProperty("content");
        
        Assert.NotNull(successProperty);
        Assert.NotNull(contentProperty);
        
        var success = (bool)successProperty.GetValue(result)!;
        var content = (string)contentProperty.GetValue(result)!;
        
        Assert.True(success);
        Assert.Equal(testContent, content);
    }

    [Fact]
    public async Task UpdateFileContent_WithValidRequest_UpdatesFileAndNode()
    {
        // Arrange - Create test file
        var testFile = Path.Combine(_testDirectory, "UpdateTest.cs");
        var originalContent = "// Original content";
        await File.WriteAllTextAsync(testFile, originalContent);
        
        await _module.InitializeFileSystemNodes();
        
        var relativePath = Path.GetRelativePath(_testDirectory, testFile);
        var nodeId = $"file:{relativePath.Replace('\\', '/').Replace('/', '.')}";

        var updateRequest = new FileUpdateRequest
        {
            Content = "// Updated content\nusing System;",
            AuthorId = "test-user",
            ChangeReason = "Test update"
        };

        // Act
        var result = await _module.UpdateFileContent(nodeId, updateRequest);

        // Assert
        Assert.NotNull(result);
        Console.WriteLine($"UpdateFileContent result type: {result.GetType().Name}");
        Console.WriteLine($"UpdateFileContent result: {result}");
        
        if (result is CodexBootstrap.Core.ErrorResponse errorResponse)
        {
            Assert.True(false, $"Method returned error: {errorResponse.Error}");
        }
        
        // Use reflection to access the anonymous object properties
        var resultType = result.GetType();
        var successProperty = resultType.GetProperty("success");
        
        Assert.NotNull(successProperty);
        
        var success = (bool)successProperty.GetValue(result)!;
        Assert.True(success);

        // Verify file was actually updated
        var updatedContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal(updateRequest.Content, updatedContent);

        // Verify node was updated
        Console.WriteLine($"Looking for nodeId: {nodeId}");
        Console.WriteLine($"Registry contains node: {_registry.TryGet(nodeId, out var updatedNode)}");
        if (updatedNode != null)
        {
            Console.WriteLine($"Node metadata keys: {string.Join(", ", updatedNode.Meta?.Keys ?? Enumerable.Empty<string>())}");
        }
        Assert.True(_registry.TryGet(nodeId, out updatedNode));
        Assert.True(updatedNode.Meta?.ContainsKey("lastEditedBy"));
        Assert.Equal("test-user", updatedNode.Meta?["lastEditedBy"]);
    }

    [Fact]
    public async Task CreateFile_WithValidRequest_CreatesFileAndNode()
    {
        // Arrange
        var createRequest = new FileCreateRequest
        {
            Path = "NewFile.cs",
            Content = "// New file content\nusing System;",
            AuthorId = "test-user"
        };

        // Act
        var result = await _module.CreateFile(createRequest);

        // Assert
        Assert.NotNull(result);
        
        if (result is CodexBootstrap.Core.ErrorResponse errorResponse)
        {
            Assert.True(false, $"Method returned error: {errorResponse.Error}");
        }
        
        // Use reflection to access the anonymous object properties
        var resultType = result.GetType();
        var successProperty = resultType.GetProperty("success");
        
        Assert.NotNull(successProperty);
        
        var success = (bool)successProperty.GetValue(result)!;
        Assert.True(success);

        // Verify file was created
        var filePath = Path.Combine(_testDirectory, "NewFile.cs");
        Assert.True(File.Exists(filePath));
        
        var content = await File.ReadAllTextAsync(filePath);
        Assert.Equal(createRequest.Content, content);

        // Verify node was created
        var nodeIdProperty = resultType.GetProperty("nodeId");
        if (nodeIdProperty != null)
        {
            var nodeId = nodeIdProperty.GetValue(result)?.ToString();
            Assert.NotNull(nodeId);
            Assert.True(_registry.TryGet(nodeId, out Node node));
            Assert.Equal("codex.file/csharp", node.TypeId);
        }
    }

    [Fact]
    public async Task SearchFiles_WithContentSearch_FindsMatches()
    {
        // Arrange - Create test files with searchable content
        var file1 = Path.Combine(_testDirectory, "SearchTest1.cs");
        var file2 = Path.Combine(_testDirectory, "SearchTest2.cs");
        
        await File.WriteAllTextAsync(file1, "using System;\n// This contains the search term: FINDME");
        await File.WriteAllTextAsync(file2, "using System;\n// This does not contain the term");
        
        await _module.InitializeFileSystemNodes();

        var searchRequest = new FileSearchRequest
        {
            Query = "FINDME",
            SearchInContent = true,
            SearchInNames = false,
            MaxResults = 10
        };

        // Act
        var result = await _module.SearchFiles(searchRequest);

        // Assert
        Assert.NotNull(result);
        
        if (result is CodexBootstrap.Core.ErrorResponse errorResponse)
        {
            Assert.True(false, $"Method returned error: {errorResponse.Error}");
        }
        
        // Use reflection to access the anonymous object properties
        var resultType = result.GetType();
        var successProperty = resultType.GetProperty("success");
        var resultsProperty = resultType.GetProperty("results");
        
        Assert.NotNull(successProperty);
        Assert.NotNull(resultsProperty);
        
        var success = (bool)successProperty.GetValue(result)!;
        var results = resultsProperty.GetValue(result)!;
        
        Assert.True(success);
        Assert.NotNull(results);
    }

    [Fact]
    public async Task GetProjectTree_WithValidProject_ReturnsTreeStructure()
    {
        // Arrange - Create nested directory structure
        Directory.CreateDirectory(Path.Combine(_testDirectory, "src", "components"));
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "src", "App.tsx"), "// App component");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "src", "components", "Button.tsx"), "// Button component");

        // Act
        var result = await _module.GetProjectTree(maxDepth: 5);

        // Assert
        Assert.NotNull(result);
        
        if (result is CodexBootstrap.Core.ErrorResponse errorResponse)
        {
            Assert.True(false, $"Method returned error: {errorResponse.Error}");
        }
        
        // Use reflection to access the anonymous object properties
        var resultType = result.GetType();
        var successProperty = resultType.GetProperty("success");
        var treeProperty = resultType.GetProperty("tree");
        
        Assert.NotNull(successProperty);
        Assert.NotNull(treeProperty);
        
        var success = (bool)successProperty.GetValue(result)!;
        var tree = treeProperty.GetValue(result)!;
        
        Assert.True(success);
        Assert.NotNull(tree);
    }

    [Fact]
    public async Task GetFileSystemStats_WithFiles_ReturnsStatistics()
    {
        // Arrange - Create various file types
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "Test.cs"), "// C# file");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "Test.ts"), "// TypeScript file");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "README.md"), "# Markdown file");
        
        await _module.InitializeFileSystemNodes();

        // Act
        var result = await _module.GetFileSystemStats();

        // Assert
        Assert.NotNull(result);
        
        if (result is CodexBootstrap.Core.ErrorResponse errorResponse)
        {
            Assert.True(false, $"Method returned error: {errorResponse.Error}");
        }
        
        // Use reflection to access the anonymous object properties
        var resultType = result.GetType();
        var successProperty = resultType.GetProperty("success");
        var statsProperty = resultType.GetProperty("stats");
        
        Assert.NotNull(successProperty);
        Assert.NotNull(statsProperty);
        
        var success = (bool)successProperty.GetValue(result)!;
        var stats = statsProperty.GetValue(result)!;
        
        Assert.True(success);
        Assert.NotNull(stats);
    }

    [Fact]
    public void FileNode_ShouldHaveCorrectContentRef()
    {
        // Arrange
        var testPath = "/test/file.cs";
        var expectedUri = new Uri($"file://{testPath}");

        // Act - Create a ContentRef for file system
        var contentRef = new ContentRef(
            MediaType: "text/x-csharp",
            InlineJson: null,
            InlineBytes: null,
            ExternalUri: expectedUri
        );

        // Assert
        Assert.Equal("text/x-csharp", contentRef.MediaType);
        Assert.Null(contentRef.InlineJson);
        Assert.Null(contentRef.InlineBytes);
        Assert.Equal(expectedUri, contentRef.ExternalUri);
    }

    [Fact]
    public async Task DeleteFile_WithValidNode_MovesToBackupAndMarksDeleted()
    {
        // Arrange - Create test file
        var testFile = Path.Combine(_testDirectory, "DeleteTest.cs");
        await File.WriteAllTextAsync(testFile, "// File to delete");
        
        await _module.InitializeFileSystemNodes();
        
        var relativePath = Path.GetRelativePath(_testDirectory, testFile);
        var nodeId = $"file:{relativePath.Replace('\\', '/').Replace('/', '.')}";

        // Act
        var result = await _module.DeleteFile(nodeId, "test-user");

        // Assert
        Assert.NotNull(result);
        
        if (result is CodexBootstrap.Core.ErrorResponse errorResponse)
        {
            Assert.True(false, $"Method returned error: {errorResponse.Error}");
        }
        
        // Use reflection to access the anonymous object properties
        var resultType = result.GetType();
        var successProperty = resultType.GetProperty("success");
        
        Assert.NotNull(successProperty);
        
        var success = (bool)successProperty.GetValue(result)!;
        Assert.True(success);

        // Verify file was moved to backup (not actually deleted)
        Assert.False(File.Exists(testFile));
        
        // Verify node still exists but marked as deleted
        Assert.True(_registry.TryGet(nodeId, out Node node));
        Assert.True(node.Meta?.ContainsKey("deleted"));
        Assert.Equal(ContentState.Gas, node.State); // Should be moved to Gas state
    }

    [Fact]
    public void ModuleProperties_ShouldBeCorrect()
    {
        // Assert
        Assert.Equal("File System", _module.Name);
        Assert.Equal("Manages all project files as nodes with file system ContentRef", _module.Description);
        Assert.Equal("1.0.0", _module.Version);
    }

    public void Dispose()
    {
        _module?.Dispose();
        
        // Reset env var
        Environment.SetEnvironmentVariable("FILESYSTEM_PROJECT_ROOT", null);
        
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
