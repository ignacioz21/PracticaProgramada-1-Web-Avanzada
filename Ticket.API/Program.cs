using System.Text.Json.Serialization;
using Abstracciones.Excepciones;
using DA.Implementaciones;
using DA.Interfaces.Cliente;
using DA.Interfaces.Ticket;
using Flujo.Implementaciones;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
using Ticket.API.Restricciones;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Personaliza el 400 automático de [ApiController] para incluir siempre title, detail útil y el detalle de campos en errors
        options.InvalidModelStateResponseFactory = context =>
        {
            var mensajes = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m));

            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = "Error de validación en la solicitud",
                Status = StatusCodes.Status400BadRequest,
                Detail = string.Join(" ", mensajes) switch
                {
                    { Length: > 0 } d => d,
                    _ => "Uno o más campos enviados contienen valores o formatos inválidos."
                },
                Instance = context.HttpContext.Request.Path
            };

            return new BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });

// Configuración de ProblemDetails estándar RFC 9457
builder.Services.AddProblemDetails();

// Registro de restricciones de ruta personalizadas
builder.Services.Configure<RouteOptions>(options =>
{
    options.ConstraintMap.Add("codigoTicket", typeof(CodigoTicketRouteConstraint));
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Inyección de dependencias
builder.Services.AddSingleton<IClienteDA, ClienteDA>();
builder.Services.AddSingleton<ITicketDA, TicketDa>();
builder.Services.AddScoped<ITicketFlujo, TicketFlujo>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

var defaultFilesOptions = new DefaultFilesOptions();
defaultFilesOptions.DefaultFileNames.Clear();
defaultFilesOptions.DefaultFileNames.Add("tickets.html");
app.UseDefaultFiles(defaultFilesOptions);
app.UseStaticFiles();

app.UseAuthorization();

// Minimal APIs requeridas en Program.cs
app.MapGet("/min/tickets/resumen", (ITicketFlujo flujo) =>
{
    var resumen = flujo.ObtenerResumenTickets();
    return Results.Ok(resumen);
})
.WithName("ObtenerResumenTicketsMin")
.WithSummary("Resumen de tickets agrupados por estado y prioridad")
.WithTags("Tickets Minimal")
.Produces<Abstracciones.Ticket.Response.ResumenTickets>(StatusCodes.Status200OK);

app.MapPost("/min/tickets/{id:int:min(1)}/reabrir", (int id, ITicketFlujo flujo) =>
{
    try
    {
        var ticket = flujo.ReabrirTicket(id);
        return Results.Ok(ticket);
    }
    catch (TicketNotFoundException ex)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Ticket no encontrado",
            detail: ex.Message
        );
    }
    catch (TicketConflictException ex)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Conflicto al reabrir ticket",
            detail: ex.Message
        );
    }
})
.WithName("ReabrirTicketMin")
.WithSummary("Reabre un ticket en estado Resuelto pasándolo a EnProceso")
.WithTags("Tickets Minimal")
.Produces<Abstracciones.Ticket.Ticket>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status409Conflict);

app.MapControllers();

app.Run();
