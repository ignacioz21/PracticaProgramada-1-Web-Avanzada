namespace Abstracciones.Ticket.Request;

public record ReqFiltro(
    EstadoTicket? Estado,
    PrioridadTicket? Prioridad,
    int? ClienteId,
    DateTime? Desde,
    DateTime? Hasta,
    string? Q,
    string? Orden,
    int? Pagina = 1,
    int? Tamano = 10
);
