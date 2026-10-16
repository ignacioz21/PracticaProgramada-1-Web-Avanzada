namespace Abstracciones.Ticket.Response;

public record ResumenTickets(
    int Total,
    Dictionary<string, int> PorEstado,
    Dictionary<string, int> PorPrioridad
);
