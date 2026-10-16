using TicketModel = Abstracciones.Ticket.Ticket;
using Abstracciones.Ticket.Request;
using Abstracciones.Ticket.Response;

namespace DA.Interfaces.Ticket;

public interface ITicketFlujo
{
    (List<TicketModel> Tickets, int Total, int Pagina, int Tamano) ListarTickets(ReqFiltro filtros);
    TicketModel ObtenerTicket(int id);
    TicketModel ObtenerTicketCodigo(string codigo);
    TicketModel CrearTicket(TicketModel ticket);
    TicketModel ActualizarEstadoTicket(int id, ReqActualizarEstado request);
    ResumenTickets ObtenerResumenTickets();
    TicketModel ReabrirTicket(int id);
}
