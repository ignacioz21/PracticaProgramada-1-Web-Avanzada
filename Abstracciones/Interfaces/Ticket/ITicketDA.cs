using TicketModel = Abstracciones.Ticket.Ticket;
using Abstracciones.Ticket.Request;
using Abstracciones.Ticket.Response;

namespace DA.Interfaces.Ticket;

public interface ITicketDA
{
    (List<TicketModel> Tickets, int Total) ListarTickets(ReqFiltro filtros);
    TicketModel? ObtenerTicket(int id);
    TicketModel? ObtenerTicketCodigo(string codigo);
    TicketModel CrearTicket(TicketModel ticket);
    TicketModel ActualizarTicket(TicketModel ticket);
    ResumenTickets ObtenerResumenTickets();
    int ContarTicketsEnProcesoPorAgente(int agenteId);
}
