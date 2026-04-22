using FluentValidation;
using MediatR;
using SubscriptionBilling.Api.Endpoints;
using SubscriptionBilling.Api.Middleware;
using SubscriptionBilling.Application;
using SubscriptionBilling.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapCustomerEndpoints();
app.MapSubscriptionEndpoints();
app.MapInvoiceEndpoints();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

// Expose Program for WebApplicationFactory in integration tests.
public partial class Program { }
