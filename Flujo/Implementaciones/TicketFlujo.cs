using Abstracciones.Excepciones;
using Abstracciones.Ticket;
using Abstracciones.Ticket.Request;
using Abstracciones.Ticket.Response;
using DA.Interfaces.Cliente;
using DA.Interfaces.Ticket;

namespace Flujo.Implementaciones;

public class TicketFlujo : ITicketFlujo
{
    private const int MaxTamanoPagina = 40; // Variante 3: máximo 40 por página
    private readonly ITicketDA _ticketDa;
    private readonly IClienteDA _clienteDa;

    public TicketFlujo(ITicketDA ticketDa, IClienteDA clienteDa)
    {
        _ticketDa = ticketDa;
        _clienteDa = clienteDa;
    }

    public (List<Ticket> Tickets, int Total, int Pagina, int Tamano) ListarTickets(ReqFiltro filtros)
    {
        var errores = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (filtros.Pagina.HasValue && filtros.Pagina.Value < 1)
        {
            AgregarError(errores, "pagina", "El parámetro 'pagina' debe ser un entero mayor o igual a 1.");
        }

        if (filtros.Tamano.HasValue && filtros.Tamano.Value < 1)
        {
            AgregarError(errores, "tamano", "El parámetro 'tamano' debe ser un entero mayor o igual a 1.");
        }
        else if (filtros.Tamano.HasValue && filtros.Tamano.Value > MaxTamanoPagina)
        {
            AgregarError(errores, "tamano", $"El tamaño de página no puede exceder el máximo permitido ({MaxTamanoPagina} para la Variante 3).");
        }

        if (filtros.ClienteId.HasValue && filtros.ClienteId.Value < 1)
        {
            AgregarError(errores, "clienteId", "El parámetro 'clienteId' debe ser un entero mayor o igual a 1.");
        }

        if (filtros.Estado.HasValue && !Enum.IsDefined(filtros.Estado.Value))
        {
            AgregarError(errores, "estado", "El estado indicado no es válido. Valores permitidos: Abierto, EnProceso, Resuelto, Cerrado.");
        }

        if (filtros.Prioridad.HasValue && !Enum.IsDefined(filtros.Prioridad.Value))
        {
            AgregarError(errores, "prioridad", "La prioridad indicada no es válida. Valores permitidos: Baja, Media, Alta, Critica.");
        }

        if (!string.IsNullOrWhiteSpace(filtros.Orden))
        {
            var ordenNormalizado = filtros.Orden.Trim().ToLowerInvariant();
            if (ordenNormalizado is not ("fecha" or "-fecha" or "prioridad" or "-prioridad"))
            {
                AgregarError(errores, "orden", "El parámetro 'orden' es inválido. Valores permitidos: fecha, -fecha, prioridad, -prioridad.");
            }
        }

        if (filtros.Desde.HasValue && filtros.Hasta.HasValue && filtros.Desde.Value > filtros.Hasta.Value)
        {
            AgregarError(errores, "desde", "La fecha 'desde' no puede ser posterior a la fecha 'hasta'.");
        }

        LanzarSiHayErrores(errores, "Uno o más parámetros de filtrado o paginación son inválidos.");

        int pagina = filtros.Pagina ?? 1;
        int tamano = filtros.Tamano ?? 10;

        var reqAjustado = filtros with { Pagina = pagina, Tamano = tamano };
        var (tickets, total) = _ticketDa.ListarTickets(reqAjustado);

        return (tickets, total, pagina, tamano);
    }

    public Ticket ObtenerTicket(int id)
    {
        var ticket = _ticketDa.ObtenerTicket(id);
        if (ticket == null)
        {
            throw new TicketNotFoundException($"No se encontró ningún ticket con Id {id}.");
        }
        return ticket;
    }

    public Ticket ObtenerTicketCodigo(string codigo)
    {
        var ticket = _ticketDa.ObtenerTicketCodigo(codigo);
        if (ticket == null)
        {
            throw new TicketNotFoundException($"No se encontró ningún ticket con código '{codigo}'.");
        }
        return ticket;
    }

    public Ticket CrearTicket(Ticket ticket)
    {
        var errores = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(ticket.Titulo))
        {
            AgregarError(errores, "titulo", "El título del ticket es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(ticket.Descripcion))
        {
            AgregarError(errores, "descripcion", "La descripción del ticket es obligatoria.");
        }

        if (ticket.ClienteId < 1)
        {
            AgregarError(errores, "clienteId", "El ClienteId debe ser mayor o igual a 1.");
        }
        else if (!_clienteDa.ExisteCliente(ticket.ClienteId))
        {
            AgregarError(errores, "clienteId", $"El cliente con Id {ticket.ClienteId} no existe en el sistema.");
        }

        if (!Enum.IsDefined(ticket.Prioridad))
        {
            AgregarError(errores, "prioridad", "La prioridad especificada no es válida.");
        }

        LanzarSiHayErrores(errores, "No se pudo crear el ticket debido a errores de validación en los campos enviados.");

        // Generación automática del código (Variante 3: HD-NNNN)
        ticket.Codigo = GenerarSiguienteCodigo();

        // Estado inicial obligatorio al crear
        ticket.Estado = EstadoTicket.Abierto;
        ticket.Reaperturas = 0;

        // Fechas automáticas del sistema
        var ahora = DateTime.Now;
        ticket.FechaCreacion = ahora;
        ticket.FechaActualizacion = ahora;

        return _ticketDa.CrearTicket(ticket);
    }

    public Ticket ActualizarEstadoTicket(int id, ReqActualizarEstado request)
    {
        var errores = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (!request.Estado.HasValue)
        {
            AgregarError(errores, "estado", "El campo 'estado' es obligatorio.");
        }
        else if (!Enum.IsDefined(request.Estado.Value))
        {
            AgregarError(errores, "estado", "El estado especificado no es válido.");
        }

        if (request.AgenteId.HasValue && request.AgenteId.Value < 1)
        {
            AgregarError(errores, "agenteId", "El campo 'agenteId' debe ser mayor o igual a 1.");
        }

        if (request.Prioridad.HasValue && !Enum.IsDefined(request.Prioridad.Value))
        {
            AgregarError(errores, "prioridad", "La prioridad especificada no es válida.");
        }

        LanzarSiHayErrores(errores, "Los datos enviados para actualizar el estado del ticket son inválidos.");

        var ticket = _ticketDa.ObtenerTicket(id);
        if (ticket == null)
        {
            throw new TicketNotFoundException($"No se encontró ningún ticket con Id {id}.");
        }

        var estadoActual = ticket.Estado;
        var nuevoEstado = request.Estado!.Value;

        if (estadoActual == EstadoTicket.Cerrado)
        {
            throw new TicketConflictException("El ticket se encuentra en estado Cerrado y no admite más transiciones.");
        }

        if (estadoActual != nuevoEstado)
        {
            bool transicionValida = (estadoActual, nuevoEstado) switch
            {
                (EstadoTicket.Abierto, EstadoTicket.EnProceso) => true,
                (EstadoTicket.EnProceso, EstadoTicket.Resuelto) => true,
                (EstadoTicket.Resuelto, EstadoTicket.Cerrado) => true,
                (EstadoTicket.Resuelto, EstadoTicket.EnProceso) => true,
                _ => false
            };

            if (!transicionValida)
            {
                throw new TicketConflictException($"Transición de estado inválida: no es posible pasar de {estadoActual} a {nuevoEstado}.");
            }
        }

        int? agenteFinal = request.AgenteId ?? ticket.AgenteId;

        // Regla especial Variante 3 (HTTP 409):
        // "Un agente no puede tener más de 5 tickets EnProceso al mismo tiempo."
        if (nuevoEstado == EstadoTicket.EnProceso && agenteFinal.HasValue)
        {
            int enProcesoCount = _ticketDa.ContarTicketsEnProcesoPorAgente(agenteFinal.Value);
            bool yaEstabaEnProcesoConEsteAgente = (estadoActual == EstadoTicket.EnProceso && ticket.AgenteId == agenteFinal.Value);
            if (!yaEstabaEnProcesoConEsteAgente && enProcesoCount >= 5)
            {
                throw new TicketConflictException($"El agente {agenteFinal.Value} ya cuenta con 5 tickets en proceso (límite máximo permitido).");
            }
        }

        if (request.AgenteId.HasValue)
        {
            ticket.AgenteId = request.AgenteId.Value;
        }

        if (request.Prioridad.HasValue)
        {
            ticket.Prioridad = request.Prioridad.Value;
        }

        if (estadoActual == EstadoTicket.Resuelto && nuevoEstado == EstadoTicket.EnProceso)
        {
            ticket.Reaperturas++;
        }

        ticket.Estado = nuevoEstado;
        ticket.FechaActualizacion = DateTime.Now;

        return _ticketDa.ActualizarTicket(ticket);
    }

    public ResumenTickets ObtenerResumenTickets() => _ticketDa.ObtenerResumenTickets();

    public Ticket ReabrirTicket(int id)
    {
        var ticket = _ticketDa.ObtenerTicket(id);
        if (ticket == null)
        {
            throw new TicketNotFoundException($"No se encontró ningún ticket con Id {id}.");
        }

        if (ticket.Estado != EstadoTicket.Resuelto)
        {
            throw new TicketConflictException($"Solo se pueden reabrir tickets que estén en estado Resuelto. El ticket actual está en {ticket.Estado}.");
        }

        // Regla especial Variante 3: si tiene agente asignado, validar límite de 5 tickets en proceso
        if (ticket.AgenteId.HasValue)
        {
            int enProcesoCount = _ticketDa.ContarTicketsEnProcesoPorAgente(ticket.AgenteId.Value);
            if (enProcesoCount >= 5)
            {
                throw new TicketConflictException($"El agente asignado ({ticket.AgenteId.Value}) ya cuenta con 5 tickets en proceso.");
            }
        }

        ticket.Estado = EstadoTicket.EnProceso;
        ticket.Reaperturas++;
        ticket.FechaActualizacion = DateTime.Now;

        return _ticketDa.ActualizarTicket(ticket);
    }

    private static void AgregarError(Dictionary<string, List<string>> errores, string campo, string mensaje)
    {
        if (!errores.TryGetValue(campo, out var lista))
        {
            lista = [];
            errores[campo] = lista;
        }
        lista.Add(mensaje);
    }

    private static void LanzarSiHayErrores(Dictionary<string, List<string>> errores, string mensajeGeneral)
    {
        if (errores.Count == 0) return;

        var dict = errores.ToDictionary(k => k.Key, v => v.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
        var detalle = string.Join(" ", errores.SelectMany(e => e.Value));
        throw new TicketValidationException(dict, string.IsNullOrWhiteSpace(detalle) ? mensajeGeneral : detalle);
    }

    private string GenerarSiguienteCodigo()
    {
        var (todos, _) = _ticketDa.ListarTickets(new ReqFiltro(null, null, null, null, null, null, null, 1, 1000));

        int maxNumero = 0;
        foreach (var t in todos)
        {
            if (!string.IsNullOrEmpty(t.Codigo) && t.Codigo.StartsWith("HD-", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(t.Codigo[3..], out int num) && num > maxNumero)
                {
                    maxNumero = num;
                }
            }
        }

        return $"HD-{(maxNumero + 1):D4}";
    }
}
