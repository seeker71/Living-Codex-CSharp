using FluentAssertions;
using LivingCodexMobile.Models;
using LivingCodexMobile.Services;
using LivingCodexMobile.ViewModels;
using Moq;
using System.Collections.ObjectModel;
using Xunit;

namespace LivingCodexMobile.Tests.ViewModels;

public class DashboardViewModelTests
{
    private readonly Mock<IApiService> _apiServiceMock;
    private readonly Mock<IConceptService> _conceptServiceMock;
    private readonly Mock<ISignalRService> _signalRServiceMock;
    private readonly Mock<IAuthenticationService> _authServiceMock;
    private readonly Mock<IEnergyService> _energyServiceMock;
    private readonly DashboardViewModel _viewModel;

    public DashboardViewModelTests()
    {
        _apiServiceMock = new Mock<IApiService>();
        _conceptServiceMock = new Mock<IConceptService>();
        _signalRServiceMock = new Mock<ISignalRService>();
        _authServiceMock = new Mock<IAuthenticationService>();
        _energyServiceMock = new Mock<IEnergyService>();

        _viewModel = new DashboardViewModel(
            _apiServiceMock.Object,
            _conceptServiceMock.Object,
            _signalRServiceMock.Object,
            _authServiceMock.Object,
            _energyServiceMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Assert
        _viewModel.Title.Should().Be("Dashboard");
        _viewModel.RecentConcepts.Should().NotBeNull();
        _viewModel.RecentContributions.Should().NotBeNull();
        _viewModel.RefreshCommand.Should().NotBeNull();
        _viewModel.ViewConceptCommand.Should().NotBeNull();
        _viewModel.ViewContributionCommand.Should().NotBeNull();
        _viewModel.LogoutCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task InitializeAsync_ShouldLoadUserAndConnectToSignalR()
    {
        // Arrange
        var user = new User
        {
            Id = "123",
            Username = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User"
        };

        _authServiceMock.Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(user);

        _signalRServiceMock.Setup(x => x.ConnectAsync())
            .Returns(Task.CompletedTask);

        _energyServiceMock.Setup(x => x.GetCollectiveEnergyAsync())
            .ReturnsAsync(100.0);

        _energyServiceMock.Setup(x => x.GetContributorEnergyAsync(It.IsAny<string>()))
            .ReturnsAsync(50.0);

        _conceptServiceMock.Setup(x => x.GetConceptsAsync(It.IsAny<ConceptQuery>()))
            .ReturnsAsync(new List<Concept>());

        _energyServiceMock.Setup(x => x.GetRecentContributionsAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Contribution>());

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        _viewModel.CurrentUser.Should().BeEquivalentTo(user);
        _signalRServiceMock.Verify(x => x.ConnectAsync(), Times.Once);
    }

    [Fact]
    public async Task RefreshDataAsync_ShouldLoadAllData()
    {
        // Arrange
        var user = new User { Id = "123", Username = "testuser" };
        _viewModel.CurrentUser = user;

        var concepts = new List<Concept>
        {
            new Concept { Id = "1", Name = "Test Concept 1" },
            new Concept { Id = "2", Name = "Test Concept 2" }
        };

        var contributions = new List<Contribution>
        {
            new Contribution { Id = "1", Title = "Test Contribution 1" },
            new Contribution { Id = "2", Title = "Test Contribution 2" }
        };

        _energyServiceMock.Setup(x => x.GetCollectiveEnergyAsync())
            .ReturnsAsync(100.0);

        _energyServiceMock.Setup(x => x.GetContributorEnergyAsync("123"))
            .ReturnsAsync(50.0);

        _conceptServiceMock.Setup(x => x.GetConceptsAsync(It.IsAny<ConceptQuery>()))
            .ReturnsAsync(concepts);

        _energyServiceMock.Setup(x => x.GetRecentContributionsAsync("123", 5))
            .ReturnsAsync(contributions);

        // Act
        _viewModel.RefreshCommand.Execute(null);

        // Assert
        _viewModel.CollectiveEnergy.Should().Be(100.0);
        _viewModel.ContributorEnergy.Should().Be(50.0);
        _viewModel.RecentConcepts.Should().HaveCount(2);
        _viewModel.RecentContributions.Should().HaveCount(2);
    }

    [Fact]
    public async Task RefreshDataAsync_WhenNotAuthenticated_ShouldNotLoadUserData()
    {
        // Arrange
        _viewModel.CurrentUser = null;

        _energyServiceMock.Setup(x => x.GetCollectiveEnergyAsync())
            .ReturnsAsync(100.0);

        _conceptServiceMock.Setup(x => x.GetConceptsAsync(It.IsAny<ConceptQuery>()))
            .ReturnsAsync(new List<Concept>());

        // Act
        _viewModel.RefreshCommand.Execute(null);

        // Assert
        _viewModel.CollectiveEnergy.Should().Be(100.0);
        _viewModel.ContributorEnergy.Should().Be(0.0);
        _energyServiceMock.Verify(x => x.GetContributorEnergyAsync(It.IsAny<string>()), Times.Never);
        _energyServiceMock.Verify(x => x.GetRecentContributionsAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RefreshDataAsync_WhenErrorOccurs_ShouldHandleError()
    {
        // Arrange
        _energyServiceMock.Setup(x => x.GetCollectiveEnergyAsync())
            .ThrowsAsync(new Exception("API Error"));

        // Act
        _viewModel.RefreshCommand.Execute(null);

        // Assert
        _viewModel.IsBusy.Should().BeFalse();
    }

}
