using LivingCodexMobile.Views;
using LivingCodexMobile.Services;

namespace LivingCodexMobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		RegisterRoutes();
		SetFlyoutContent();
	}

	private void SetFlyoutContent()
	{
		var flyoutMenu = new FlyoutMenuPage(
			Application.Current.Handler.MauiContext.Services.GetRequiredService<IAuthenticationService>());
		FlyoutContent = flyoutMenu;
	}

	private void RegisterRoutes()
	{
		// Main routes
		Routing.RegisterRoute("login", typeof(MainPage));
		Routing.RegisterRoute("main", typeof(DashboardPage));
		Routing.RegisterRoute("onboarding", typeof(OnboardingPage));
		
		// Tab routes
		Routing.RegisterRoute("dashboard", typeof(DashboardPage));
		Routing.RegisterRoute("news", typeof(NewsFeedPage));
		Routing.RegisterRoute("concepts", typeof(ConceptDiscoveryPage));
		Routing.RegisterRoute("explore", typeof(NodeExplorerPage));
		
		// Detail routes with parameters
		Routing.RegisterRoute("conceptdetail", typeof(ConceptDetailPage));
		Routing.RegisterRoute("newsdetail", typeof(NewsDetailPage));
		Routing.RegisterRoute("nodedetail", typeof(NodeDetailPage));
		Routing.RegisterRoute("edgedetail", typeof(EdgeDetailPage));
		
		// Parameterized routes
		Routing.RegisterRoute("concept/{id}", typeof(ConceptDetailPage));
		Routing.RegisterRoute("news/{id}", typeof(NewsDetailPage));
		Routing.RegisterRoute("node/{id}", typeof(NodeDetailPage));
		Routing.RegisterRoute("edge/{id}", typeof(EdgeDetailPage));
	}
}
