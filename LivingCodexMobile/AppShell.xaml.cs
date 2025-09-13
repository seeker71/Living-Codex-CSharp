using LivingCodexMobile.Views;

namespace LivingCodexMobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		RegisterRoutes();
	}

	private void RegisterRoutes()
	{
		Routing.RegisterRoute("login", typeof(MainPage));
		Routing.RegisterRoute("main", typeof(DashboardPage));
		// Add more routes as needed
		// Routing.RegisterRoute("concept/{id}", typeof(ConceptDetailPage));
		// Routing.RegisterRoute("contribution/{id}", typeof(ContributionDetailPage));
	}
}
