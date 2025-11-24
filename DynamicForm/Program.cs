using DynamicForm.Components;
using DynamicForm.Helper;
using DynamicForm.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options => options.DetailedErrors = true);

builder.Services.AddScoped<IFormService, FormService>();
builder.Services.AddScoped<FormBuilderState>();
builder.Services.AddScoped<FormStateService>();
builder.Services.AddScoped<ICalculationEngineService, CalculationEngineService>();
builder.Services.AddScoped<INumberFormatService, NumberFormatService>();
builder.Services.AddScoped<IEnhancedCalculationEngineService, EnhancedCalculationEngineService>();
builder.Services.AddScoped<ITableLayoutService, TableLayoutService>();
builder.Services.AddScoped<IAdvancedTableLayoutService, AdvancedTableLayoutService>();
builder.Services.AddScoped<IRuleEngineService, RuleEngineService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
