# Living Codex Mobile App Test Suite

This directory contains comprehensive tests for the Living Codex mobile application, ensuring all features work correctly and the UI is functional.

## Test Structure

### Test Categories

1. **Unit Tests** (`Services/`, `ViewModels/`)
   - Test individual service methods
   - Test ViewModel logic and commands
   - Test data models and validation
   - Mock external dependencies

2. **Integration Tests** (`Integration/`)
   - Test service interactions
   - Test API communication
   - Test data flow between components
   - Use real or mock HTTP clients

3. **UI Tests** (`UI/`)
   - Test page navigation
   - Test button interactions
   - Test input field validation
   - Test ViewModel binding

4. **API Tests** (`API/`)
   - Test API endpoint availability
   - Test request/response handling
   - Test error scenarios
   - Test authentication flows

### Test Files

- `Services/` - Unit tests for service classes
- `ViewModels/` - Unit tests for ViewModel classes
- `UI/` - UI interaction tests
- `Integration/` - Integration tests
- `API/` - API endpoint tests
- `TestData/` - Test data factories and helpers
- `UI TestHelpers/` - Helper classes for UI testing

## Running Tests

### Prerequisites

1. .NET 6.0 or later
2. Living Codex server running (for integration tests)
3. All dependencies restored

### Quick Start

```bash
# Run all tests
./run-tests.sh

# Or run with dotnet
dotnet test
```

### Individual Test Categories

```bash
# Run only unit tests
dotnet test --filter "Category=Unit"

# Run only integration tests
dotnet test --filter "Category=Integration"

# Run only UI tests
dotnet test --filter "Category=UI"

# Run only API tests
dotnet test --filter "Category=API"
```

### Specific Test Classes

```bash
# Run specific test class
dotnet test --filter "ClassName=ApiServiceTests"

# Run specific test method
dotnet test --filter "MethodName=GetAsync_ValidRequest_ReturnsData"
```

## Test Configuration

### Environment Variables

- `API_BASE_URL` - Base URL for API tests (default: http://localhost:5002)
- `TEST_TIMEOUT` - Test timeout in seconds (default: 30)
- `ENABLE_MOCKING` - Enable mock services (default: true)

### Test Data

Test data is generated using `TestDataFactory` class:
- `CreateTestUser()` - Creates test user data
- `CreateTestConcept()` - Creates test concept data
- `CreateTestNewsItem()` - Creates test news item data
- `CreateTestApiResponse()` - Creates test API responses

## Test Coverage

### Services Tested

- ✅ `ApiService` - Generic API communication
- ✅ `AuthenticationService` - User authentication
- ✅ `NewsFeedService` - News feed management
- ✅ `ConceptService` - Concept management
- ✅ `NodeExplorerService` - Node/edge exploration
- ✅ `MediaRendererService` - Content rendering
- ✅ `EnergyService` - Energy and contribution tracking

### ViewModels Tested

- ✅ `DashboardViewModel` - Main dashboard logic
- ✅ `LoginViewModel` - Login/registration logic
- ✅ `NewsFeedViewModel` - News feed management
- ✅ `ConceptDiscoveryViewModel` - Concept discovery
- ✅ `NodeExplorerViewModel` - Node exploration
- ✅ `OnboardingViewModel` - User onboarding

### UI Components Tested

- ✅ Page navigation
- ✅ Button interactions
- ✅ Input field validation
- ✅ Data binding
- ✅ Command execution
- ✅ Error handling

### API Endpoints Tested

- ✅ Authentication endpoints
- ✅ News feed endpoints
- ✅ Concept management endpoints
- ✅ Node/edge exploration endpoints
- ✅ Energy tracking endpoints

## Test Patterns

### Service Testing

```csharp
[Fact]
public async Task ServiceMethod_ValidInput_ReturnsExpectedResult()
{
    // Arrange
    var mockDependency = new Mock<IDependency>();
    var service = new Service(mockDependency.Object);
    
    // Act
    var result = await service.MethodAsync(input);
    
    // Assert
    result.Should().NotBeNull();
    result.Property.Should().Be(expectedValue);
}
```

### ViewModel Testing

```csharp
[Fact]
public void ViewModel_Command_CanExecute()
{
    // Arrange
    var viewModel = new ViewModel();
    
    // Act
    var canExecute = viewModel.Command.CanExecute(null);
    
    // Assert
    canExecute.Should().BeTrue();
}
```

### UI Testing

```csharp
[Fact]
public void Page_ButtonClick_ExecutesCommand()
{
    // Arrange
    var page = new TestPage();
    var viewModel = PageTestHelper.CreateViewModel();
    page.BindingContext = viewModel;
    
    // Act
    page.Button.Command.Execute(null);
    
    // Assert
    viewModel.Property.Should().Be(expectedValue);
}
```

## Mocking Strategy

### External Dependencies

- HTTP clients
- File system operations
- Network requests
- External APIs

### Internal Services

- Authentication services
- Data services
- Logging services
- Error handling services

## Test Data Management

### Test Data Factory

Use `TestDataFactory` to create consistent test data:

```csharp
var user = TestDataFactory.CreateTestUser();
var concept = TestDataFactory.CreateTestConcept();
var newsItem = TestDataFactory.CreateTestNewsItem();
```

### Mock Responses

Use `MockResponseFactory` for API responses:

```csharp
var mockResponse = MockResponseFactory.CreateSuccessResponse(data);
var mockErrorResponse = MockResponseFactory.CreateErrorResponse("Error message");
```

## Continuous Integration

### GitHub Actions

Tests run automatically on:
- Pull requests
- Push to main branch
- Scheduled runs

### Test Reports

- Test results are published as artifacts
- Coverage reports are generated
- Performance metrics are tracked

## Troubleshooting

### Common Issues

1. **Tests failing due to server not running**
   - Start the Living Codex server
   - Check server URL configuration

2. **Mock setup issues**
   - Verify mock configurations
   - Check service registrations

3. **Test data issues**
   - Use `TestDataFactory` for consistent data
   - Verify model constructors

4. **UI test failures**
   - Check XAML bindings
   - Verify ViewModel properties

### Debug Mode

Run tests in debug mode for detailed output:

```bash
dotnet test --verbosity detailed --logger "console;verbosity=detailed"
```

## Contributing

### Adding New Tests

1. Create test class in appropriate directory
2. Follow naming convention: `{ClassName}Tests`
3. Use `[Fact]` for test methods
4. Follow Arrange-Act-Assert pattern
5. Add appropriate test categories

### Test Naming

- Test methods: `{MethodName}_{Scenario}_{ExpectedResult}`
- Test classes: `{ClassName}Tests`
- Test categories: `Unit`, `Integration`, `UI`, `API`

### Best Practices

1. **Isolation** - Each test should be independent
2. **Clarity** - Test names should be descriptive
3. **Coverage** - Test both success and failure scenarios
4. **Maintainability** - Use helper methods and factories
5. **Performance** - Keep tests fast and efficient

## Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/moq/moq4)
- [.NET Testing Guide](https://docs.microsoft.com/en-us/dotnet/core/testing/)
