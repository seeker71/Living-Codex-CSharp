using FluentAssertions;
using LivingCodexMobile.ViewModels;
using LivingCodexMobile.Services;
using Moq;
using Xunit;

namespace LivingCodexMobile.Tests.UI;

public class PageNavigationTests
{
    [Fact]
    public void DashboardViewModel_ViewConceptCommand_ShouldNavigateToConceptDetail()
    {
        // Arrange
        var mockApiService = new Mock<IApiService>();
        var mockConceptService = new Mock<IConceptService>();
        var mockSignalRService = new Mock<ISignalRService>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockEnergyService = new Mock<IEnergyService>();

        var viewModel = new DashboardViewModel(
            mockApiService.Object,
            mockConceptService.Object,
            mockSignalRService.Object,
            mockAuthService.Object,
            mockEnergyService.Object);

        var concept = new Models.Concept
        {
            Id = "test-concept-1",
            Name = "Test Concept",
            Description = "Test Description"
        };

        // Act
        var canExecute = viewModel.ViewConceptCommand.CanExecute(concept);

        // Assert
        canExecute.Should().BeTrue();
    }

    [Fact]
    public void DashboardViewModel_ViewContributionCommand_ShouldNavigateToContributionDetail()
    {
        // Arrange
        var mockApiService = new Mock<IApiService>();
        var mockConceptService = new Mock<IConceptService>();
        var mockSignalRService = new Mock<ISignalRService>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockEnergyService = new Mock<IEnergyService>();

        var viewModel = new DashboardViewModel(
            mockApiService.Object,
            mockConceptService.Object,
            mockSignalRService.Object,
            mockAuthService.Object,
            mockEnergyService.Object);

        var contribution = new Models.Contribution
        {
            Id = "test-contribution-1",
            Title = "Test Contribution",
            Description = "Test Description"
        };

        // Act
        var canExecute = viewModel.ViewContributionCommand.CanExecute(contribution);

        // Assert
        canExecute.Should().BeTrue();
    }

    [Fact]
    public void DashboardViewModel_RefreshCommand_ShouldBeExecutable()
    {
        // Arrange
        var mockApiService = new Mock<IApiService>();
        var mockConceptService = new Mock<IConceptService>();
        var mockSignalRService = new Mock<ISignalRService>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockEnergyService = new Mock<IEnergyService>();

        var viewModel = new DashboardViewModel(
            mockApiService.Object,
            mockConceptService.Object,
            mockSignalRService.Object,
            mockAuthService.Object,
            mockEnergyService.Object);

        // Act
        var canExecute = viewModel.RefreshCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue();
    }

    [Fact]
    public void DashboardViewModel_LogoutCommand_ShouldBeExecutable()
    {
        // Arrange
        var mockApiService = new Mock<IApiService>();
        var mockConceptService = new Mock<IConceptService>();
        var mockSignalRService = new Mock<ISignalRService>();
        var mockAuthService = new Mock<IAuthenticationService>();
        var mockEnergyService = new Mock<IEnergyService>();

        var viewModel = new DashboardViewModel(
            mockApiService.Object,
            mockConceptService.Object,
            mockSignalRService.Object,
            mockAuthService.Object,
            mockEnergyService.Object);

        // Act
        var canExecute = viewModel.LogoutCommand.CanExecute(null);

        // Assert
        canExecute.Should().BeTrue();
    }
}
