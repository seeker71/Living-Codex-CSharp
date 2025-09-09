using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using CodexBootstrap.Modules;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.WriteIndented = true;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

// Registries & engines
builder.Services.AddSingleton<NodeRegistry>();
builder.Services.AddSingleton<ModuleRegistry>();
builder.Services.AddSingleton<ApiRouter>();
builder.Services.AddSingleton<SpecRegistry>();
builder.Services.AddSingleton<SpecComposer>();
builder.Services.AddSingleton<BreathEngine>();

// Phase & resonance
builder.Services.AddSingleton<PhaseEngine>();
builder.Services.AddSingleton<IResonanceChecker, TrivialResonanceChecker>();

// Adapters
builder.Services.AddSingleton<AdapterRegistry>();
builder.Services.AddSingleton<IAdapterRegistry>(sp => sp.GetRequiredService<AdapterRegistry>());

// Synthesizers / generators / validators
builder.Services.AddSingleton<ISynthesizer>(sp => new EchoSynthesizer(sp.GetRequiredService<IAdapterRegistry>()));
builder.Services.AddSingleton<ISpecGenerator, BasicSpecGenerator>();
builder.Services.AddSingleton<IValidator, NoopValidator>();

var app = builder.Build();

// Register built-in adapters
var adapters = app.Services.GetRequiredService<AdapterRegistry>();
adapters.Register(new FileAdapter());
adapters.Register(new HttpAdapter());
adapters.Register(new HttpAdapterWithScheme("https"));

// Bootstrap core atoms & spec
app.MapGet("/core/atoms", () => CoreAtomsSeed.Atoms());
var coreSpec = CoreSpecs.CoreModuleSpec();
app.MapGet("/core/spec", () => coreSpec);

// Resolve services
var modules = app.Services.GetRequiredService<ModuleRegistry>();
var nodes = app.Services.GetRequiredService<NodeRegistry>();
var router = app.Services.GetRequiredService<ApiRouter>();
var specs = app.Services.GetRequiredService<SpecRegistry>();
var composer = app.Services.GetRequiredService<SpecComposer>();
var breath = app.Services.GetRequiredService<BreathEngine>();

// Load built‑in module(s)
var hello = new HelloModule();
modules.Register(hello);
hello.Register(router, nodes);

// Optional: load external modules from ./modules/*.dll
var moduleDir = Path.Combine(AppContext.BaseDirectory, "modules");
if (Directory.Exists(moduleDir))
{
    foreach (var dll in Directory.GetFiles(moduleDir, "*.dll"))
    {
        try
        {
            var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
            foreach (var t in asm.GetTypes())
            {
                if (typeof(IModule).IsAssignableFrom(t) && !t.IsAbstract && Activator.CreateInstance(t) is IModule m)
                {
                    modules.Register(m);
                    m.Register(router, nodes);
                }
            }
        }
        catch { /* ignore bad modules for now */ }
    }
}

// ---- Nodes/Edges minimal wire API ----
app.MapGet("/nodes/{id}", (string id) => nodes.TryGet(id, out var n) ? Results.Ok(n) : Results.NotFound());
app.MapPost("/nodes", (Node n) => { nodes.Upsert(n); return Results.Ok(n); });
app.MapGet("/edges", () => nodes.AllEdges());
app.MapPost("/edges", (Edge e) => { nodes.Upsert(e); return Results.Ok(e); });

// Hydrate (Gas/Ice → Water on demand)
app.MapPost("/hydrate/{id}", async (string id, [FromServices] ISynthesizer synth) =>
{
    if (!nodes.TryGet(id, out var n)) return Results.NotFound();
    var hydrated = await synth.SynthesizeAsync(n, nodes);
    nodes.Upsert(hydrated);
    return Results.Ok(hydrated);
});

// Module discovery & specs
app.MapGet("/modules", () => modules.All().Select(m => m.Spec));
app.MapGet("/modules/{id}", (string id) => modules.TryGet(id, out var m) ? Results.Ok(m.Spec) : Results.NotFound());

// Dynamic API route — self‑describing invocation
app.MapPost("/route", async (DynamicCall req) =>
{
    if (!modules.TryGet(req.ModuleId, out var m)) return Results.NotFound($"Module {req.ModuleId} not found");
    if (!router.TryGetHandler(req.ModuleId, req.Api, out var handler)) return Results.NotFound($"API {req.Api} not found in module {req.ModuleId}");
    var result = await handler(req.Args);
    return Results.Ok(result);
});

// -------------- Spec/Atoms → Specs → Prototypes (Breath) --------------
app.MapPost("/spec/atoms", (ModuleAtoms atoms) => { specs.UpsertAtoms(atoms); return Results.Ok(atoms); });
app.MapPost("/spec/compose", (ModuleAtoms atoms) => Results.Ok(composer.Compose(atoms)));
app.MapGet("/spec/atoms/{id}", (string id) => specs.TryGetAtoms(id, out var a) ? Results.Ok(a) : Results.NotFound());
app.MapGet("/spec/{id}", (string id) => specs.TryGetSpec(id, out var s) ? Results.Ok(s) : Results.NotFound());
app.MapPost("/breath/expand/{id}", (string id) => breath.Expand(id));
app.MapPost("/breath/validate/{id}", (string id) => breath.Validate(id));
app.MapPost("/breath/contract/{id}", (string id) => breath.Contract(id));
app.MapGet("/plan/{id}", (string id) => Results.Ok(specs.Topology(id)));
app.MapGet("/openapi/{id}", (string id) => { if (!specs.TryGetSpec(id, out var s)) return Results.NotFound(); var doc = OpenApiHelper.FromModuleSpec(s); return Results.Ok(doc); });

// --- One‑shot: from atoms to prototype+delta in one call ---
app.MapPost("/oneshot/apply", (ModuleAtoms atoms, [FromServices] SpecRegistry specs, [FromServices] SpecComposer composer, [FromServices] BreathEngine breath) =>
{
    specs.UpsertAtoms(atoms);
    specs.UpsertSpec(composer.Compose(atoms));
    breath.Expand(atoms.Id);
    breath.Validate(atoms.Id);
    return breath.Contract(atoms.Id);
});

app.MapPost("/oneshot/{id}", (string id, [FromServices] SpecRegistry specs, [FromServices] BreathEngine breath) =>
{
    if (!specs.TryGetAtoms(id, out _)) return Results.NotFound();
    breath.Expand(id);
    breath.Validate(id);
    return breath.Contract(id);
});

// --- Reflect spec ⇄ node graph ---
app.MapGet("/reflect/spec/{id}", (string id, [FromServices] SpecRegistry specs, [FromServices] SpecReflector refl, [FromServices] NodeRegistry reg) =>
{
    if (!specs.TryGetSpec(id, out var s)) return Results.NotFound();
    var nodeset = refl.ToNodes(s);
    foreach (var n in nodeset) reg.Upsert(n);
    return Results.Ok(nodeset);
});

app.MapPost("/ingest/spec", (IEnumerable<Node> nodeset, [FromServices] SpecReflector refl, [FromServices] SpecRegistry specs) =>
{
    var spec = refl.FromNodes(nodeset);
    specs.UpsertSpec(spec);
    return Results.Ok(spec);
});

// --- Minimal diff/patch over Node (git‑like tiny deltas) ---
app.MapGet("/diff/{id}", (string id, string? against, [FromServices] NodeRegistry reg) =>
{
    if (!reg.TryGet(id, out var a)) return Results.NotFound();
    if (against is null || !reg.TryGet(against, out var b)) return Results.BadRequest("missing or invalid 'against'");
    var patch = PatchEngine.Diff(b, a); // produce ops to get from b → a
    return Results.Ok(patch);
});

app.MapPost("/patch/{targetId}", (string targetId, PatchDoc patch, [FromServices] NodeRegistry reg) =>
{
    if (!reg.TryGet(targetId, out var n)) return Results.NotFound();
    var updated = PatchEngine.Apply(n, patch);
    reg.Upsert(updated);
    return Results.Ok(updated);
});

// Spec exchange (fractal sharing)
app.MapGet("/spec/export/{id}", (string id) => specs.TryGetAtoms(id, out var a) ? Results.Ok(a) : Results.NotFound());
app.MapPost("/spec/import", (ModuleAtoms atoms) => { specs.UpsertAtoms(atoms); return Results.Ok(new { imported = atoms.Id }); });

// Adapter registration (constrained demo)
app.MapPost("/adapters/register", (AdapterRegistration r) =>
{
    if (r.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)) adapters.Register(new HttpAdapterWithScheme("https"));
    else if (r.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)) adapters.Register(new HttpAdapter());
    else if (r.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase)) adapters.Register(new FileAdapter());
    return Results.Ok(new { registered = r.Scheme });
});

// ----- Phase transitions & resonance -----
app.MapPost("/phase/melt/{id}", (string id, [FromServices] PhaseEngine phase, [FromServices] NodeRegistry reg) =>
{
    if (!reg.TryGet(id, out var n)) return Results.NotFound();
    var melted = phase.Melt(n);
    reg.Upsert(melted);
    return Results.Ok(melted);
});

app.MapPost("/phase/refreeze/{id}", (string id, [FromServices] PhaseEngine phase, [FromServices] NodeRegistry reg) =>
{
    if (!reg.TryGet(id, out var n)) return Results.NotFound();
    var frozen = phase.Refreeze(n);
    reg.Upsert(frozen);
    return Results.Ok(frozen);
});

app.MapPost("/resonance/check", async (ResonanceProposal p, [FromServices] IResonanceChecker checker, [FromServices] NodeRegistry reg) =>
{
    var report = await checker.CheckAsync(p, reg);
    return Results.Ok(report);
});

app.Run();
