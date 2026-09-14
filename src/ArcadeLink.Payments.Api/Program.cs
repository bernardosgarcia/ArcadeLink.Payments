using ArcadeLink.Payments.Api.Consumers;
using ArcadeLink.Payments.Api.Options;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<PaymentsOptions>(builder.Configuration.GetSection(PaymentsOptions.SectionName));

builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration["Redis:ConnectionString"]);
builder.Services.AddSingleton<IApprovalRateProvider, CachedApprovalRateProvider>();

builder.Services.AddMassTransit(x =>
{
    // Prefixed for consistency with the other services and to avoid any future collision if another
    // service ever adds a consumer class with the same name on the shared RabbitMQ vhost (see the
    // ArcadeLink.Catalog/Notifications PaymentProcessedEventConsumer gotcha discovered in Etapa 5).
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("payments", false));

    x.AddConsumer<OrderPlacedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"]!);
            h.Password(builder.Configuration["RabbitMq:Password"]!);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Payments is purely event-driven today: it exposes no authenticated HTTP endpoint, so it carries no
// authentication/authorization wiring at all. Any authenticated endpoint added here in the future
// must go through the API Gateway (Kong) and read the X-User-Id/X-User-Role headers it injects,
// following the KongHeader scheme used by Users and Catalog.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health")
    .WithOpenApi();

app.Run();
