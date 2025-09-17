using CodexBootstrap.Hosting;

BootstrapEnvironment.Initialize();

var builder = WebApplication.CreateBuilder(args);
CodexBootstrapHost.ConfigureBuilder(builder, args);

var app = builder.Build();
var hostingUrl = CodexBootstrapHost.ConfigureApp(app);

Console.WriteLine($"Starting Living Codex on {hostingUrl}");
app.Run(hostingUrl);
