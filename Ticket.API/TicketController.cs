using System.Security.Cryptography;
using System.Text;
using Abstracciones.Excepciones;
using Abstracciones.Ticket;
using Abstracciones.Ticket.Request;
using DA.Interfaces.Ticket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Ticket.API;

[ApiController]
[Route("api/tickets")]
[Produces("application/json")]
[Tags("Tickets")]
public class TicketController : ControllerBase
{
    private readonly ITicketFlujo _ticketFlujo;

    public TicketController(ITicketFlujo ticketFlujo)
    {
        _ticketFlujo = ticketFlujo;
    }

    // GET /api/tickets
    [HttpGet]
    [ProducesResponseType(typeof(List<Abstracciones.Ticket.Ticket>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult ObtenerTickets([FromQuery] ReqFiltro filtros)
    {
        try
        {
            var (tickets, total, pagina, tamano) = _ticketFlujo.ListarTickets(filtros);

            Response.Headers.Append("X-Total-Count", total.ToString());
            Response.Headers.Append("X-Page", pagina.ToString());
            Response.Headers.Append("X-Page-Size", tamano.ToString());

            return Ok(tickets);
        }
        catch (TicketValidationException ex)
        {
            return CrearValidationProblem(ex, "Parámetros de consulta inválidos");
        }
    }

    // GET /api/tickets/{id}
    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType(typeof(Abstracciones.Ticket.Ticket), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult ObtenerTicketPorId([FromRoute] int id)
    {
        try
        {
            var ticket = _ticketFlujo.ObtenerTicket(id);
            var etag = CalcularETag(ticket);

            if (Request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch))
            {
                if (ifNoneMatch.ToString().Trim('"') == etag.Trim('"') || ifNoneMatch.ToString() == etag)
                {
                    Response.Headers.ETag = etag;
                    return StatusCode(StatusCodes.Status304NotModified);
                }
            }

            Response.Headers.ETag = etag;
            return Ok(ticket);
        }
        catch (TicketNotFoundException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Ticket no encontrado",
                detail: ex.Message
            );
        }
    }

    // GET /api/tickets/codigo/{codigo}
    [HttpGet("codigo/{codigo:codigoTicket}")]
    [ProducesResponseType(typeof(Abstracciones.Ticket.Ticket), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult ObtenerTicketPorCodigo([FromRoute] string codigo)
    {
        try
        {
            var ticket = _ticketFlujo.ObtenerTicketCodigo(codigo);
            return Ok(ticket);
        }
        catch (TicketNotFoundException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Ticket no encontrado",
                detail: ex.Message
            );
        }
    }

    // POST /api/tickets
    [HttpPost]
    [ProducesResponseType(typeof(Abstracciones.Ticket.Ticket), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult CrearTicket([FromBody] Abstracciones.Ticket.Ticket request)
    {
        try
        {
            var resultado = _ticketFlujo.CrearTicket(request);
            return CreatedAtAction(nameof(ObtenerTicketPorId), new { id = resultado.Id }, resultado);
        }
        catch (TicketValidationException ex)
        {
            return CrearValidationProblem(ex, "Solicitud de creación de ticket inválida");
        }
    }

    // PATCH /api/tickets/{id}/estado
    [HttpPatch("{id:int:min(1)}/estado")]
    [ProducesResponseType(typeof(Abstracciones.Ticket.Ticket), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public IActionResult ActualizarEstado(
        [FromRoute] int id,
        [FromBody] ReqActualizarEstado request)
    {
        try
        {
            var resultado = _ticketFlujo.ActualizarEstadoTicket(id, request);
            return Ok(resultado);
        }
        catch (TicketNotFoundException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Ticket no encontrado",
                detail: ex.Message
            );
        }
        catch (TicketConflictException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflicto en la operación",
                detail: ex.Message
            );
        }
        catch (TicketValidationException ex)
        {
            return CrearValidationProblem(ex, "Datos de actualización de estado inválidos");
        }
    }

    private ActionResult CrearValidationProblem(TicketValidationException ex, string title)
    {
        var modelState = new ModelStateDictionary();
        foreach (var (campo, mensajes) in ex.Errores)
        {
            foreach (var msg in mensajes)
            {
                modelState.AddModelError(campo, msg);
            }
        }

        return ValidationProblem(
            title: title,
            detail: ex.Message,
            statusCode: StatusCodes.Status400BadRequest,
            modelStateDictionary: modelState
        );
    }

    private static string CalcularETag(Abstracciones.Ticket.Ticket ticket)
    {
        var rawData = $"{ticket.Id}|{ticket.Codigo}|{ticket.Estado}|{ticket.Prioridad}|{ticket.AgenteId}|{ticket.Reaperturas}|{ticket.FechaActualizacion:O}";
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        var hash = Convert.ToHexString(bytes).ToLowerInvariant();
        return $"\"{hash}\"";
    }
}
