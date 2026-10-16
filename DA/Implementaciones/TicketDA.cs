using Abstracciones.Ticket;
using Abstracciones.Ticket.Request;
using Abstracciones.Ticket.Response;
using DA.Interfaces.Ticket;

namespace DA.Implementaciones;

public class TicketDa : ITicketDA
{
    // Lista estática en memoria con al menos 25 tickets de ejemplo (formato HD-NNNN)
    private static readonly List<Ticket> Tickets = new()
    {
        new Ticket
        {
            Id = 1,
            Codigo = "HD-0001",
            Titulo = "Fallo en autenticación de usuario",
            Descripcion = "El usuario no puede iniciar sesión con sus credenciales habituales.",
            Prioridad = PrioridadTicket.Alta,
            Estado = EstadoTicket.Abierto,
            ClienteId = 101,
            AgenteId = null,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-28),
            FechaActualizacion = DateTime.UtcNow.AddDays(-28)
        },
        new Ticket
        {
            Id = 2,
            Codigo = "HD-0002",
            Titulo = "Lentitud al cargar reportes mensuales",
            Descripcion = "El módulo de reportes mensuales demora más de 45 segundos en responder.",
            Prioridad = PrioridadTicket.Media,
            Estado = EstadoTicket.EnProceso,
            ClienteId = 102,
            AgenteId = 201,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-26),
            FechaActualizacion = DateTime.UtcNow.AddDays(-25)
        },
        new Ticket
        {
            Id = 3,
            Codigo = "HD-0003",
            Titulo = "Error 500 al procesar pago con tarjeta",
            Descripcion = "La pasarela devuelve un error interno al confirmar la orden de compra.",
            Prioridad = PrioridadTicket.Critica,
            Estado = EstadoTicket.EnProceso,
            ClienteId = 103,
            AgenteId = 201,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-25),
            FechaActualizacion = DateTime.UtcNow.AddDays(-24)
        },
        new Ticket
        {
            Id = 4,
            Codigo = "HD-0004",
            Titulo = "Solicitud de nuevo acceso de lectura",
            Descripcion = "Se requiere acceso de solo lectura para el área de auditoría interna.",
            Prioridad = PrioridadTicket.Baja,
            Estado = EstadoTicket.Resuelto,
            ClienteId = 104,
            AgenteId = 202,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-24),
            FechaActualizacion = DateTime.UtcNow.AddDays(-22)
        },
        new Ticket
        {
            Id = 5,
            Codigo = "HD-0005",
            Titulo = "Error al exportar a Excel",
            Descripcion = "El archivo descargado se encuentra dañado o ilegible.",
            Prioridad = PrioridadTicket.Media,
            Estado = EstadoTicket.Abierto,
            ClienteId = 105,
            AgenteId = null,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-23),
            FechaActualizacion = DateTime.UtcNow.AddDays(-23)
        },
        new Ticket
        {
            Id = 6,
            Codigo = "HD-0006",
            Titulo = "No llegan correos de confirmación",
            Descripcion = "Los clientes reportan no recibir notificación tras registrarse.",
            Prioridad = PrioridadTicket.Alta,
            Estado = EstadoTicket.EnProceso,
            ClienteId = 101,
            AgenteId = 203,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-22),
            FechaActualizacion = DateTime.UtcNow.AddDays(-21)
        },
        new Ticket
        {
            Id = 7,
            Codigo = "HD-0007",
            Titulo = "Caída de servidor secundario",
            Descripcion = "El nodo 2 del clúster no responde a los pings de salud.",
            Prioridad = PrioridadTicket.Critica,
            Estado = EstadoTicket.Resuelto,
            ClienteId = 102,
            AgenteId = 201,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-21),
            FechaActualizacion = DateTime.UtcNow.AddDays(-20)
        },
        new Ticket
        {
            Id = 8,
            Codigo = "HD-0008",
            Titulo = "Actualizar logotipo en cabecera",
            Descripcion = "Cambiar imagen corporativa según nuevo manual de marca.",
            Prioridad = PrioridadTicket.Baja,
            Estado = EstadoTicket.Cerrado,
            ClienteId = 103,
            AgenteId = 202,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-20),
            FechaActualizacion = DateTime.UtcNow.AddDays(-18)
        },
        new Ticket
        {
            Id = 9,
            Codigo = "HD-0009",
            Titulo = "Error de sincronización con CRM",
            Descripcion = "Las cuentas creadas hoy no se reflejan en la base comercial.",
            Prioridad = PrioridadTicket.Alta,
            Estado = EstadoTicket.EnProceso,
            ClienteId = 104,
            AgenteId = 203,
            Reaperturas = 1,
            FechaCreacion = DateTime.UtcNow.AddDays(-19),
            FechaActualizacion = DateTime.UtcNow.AddDays(-17)
        },
        new Ticket
        {
            Id = 10,
            Codigo = "HD-0010",
            Titulo = "Problema con caracteres especiales en nombres",
            Descripcion = "Los nombres con tildes o eñes generan excepción al guardar.",
            Prioridad = PrioridadTicket.Media,
            Estado = EstadoTicket.Abierto,
            ClienteId = 105,
            AgenteId = null,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-18),
            FechaActualizacion = DateTime.UtcNow.AddDays(-18)
        },
        new Ticket
        {
            Id = 11,
            Codigo = "HD-0011",
            Titulo = "Fuga de memoria en servicio de notificaciones",
            Descripcion = "El servicio incrementa su consumo de RAM hasta reiniciar el pod.",
            Prioridad = PrioridadTicket.Critica,
            Estado = EstadoTicket.Abierto,
            ClienteId = 101,
            AgenteId = null,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-17),
            FechaActualizacion = DateTime.UtcNow.AddDays(-17)
        },
        new Ticket
        {
            Id = 12,
            Codigo = "HD-0012",
            Titulo = "Modificar texto en pantalla de bienvenida",
            Descripcion = "Ajustar texto legal de términos y condiciones.",
            Prioridad = PrioridadTicket.Baja,
            Estado = EstadoTicket.Resuelto,
            ClienteId = 102,
            AgenteId = 202,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-16),
            FechaActualizacion = DateTime.UtcNow.AddDays(-15)
        },
        new Ticket
        {
            Id = 13,
            Codigo = "HD-0013",
            Titulo = "Inconsistencia en cálculo de IVA",
            Descripcion = "El total de la factura difiere por 1 céntimo en ciertas combinaciones.",
            Prioridad = PrioridadTicket.Alta,
            Estado = EstadoTicket.EnProceso,
            ClienteId = 103,
            AgenteId = 204,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-15),
            FechaActualizacion = DateTime.UtcNow.AddDays(-14)
        },
        new Ticket
        {
            Id = 14,
            Codigo = "HD-0014",
            Titulo = "Sesión expira de forma prematura",
            Descripcion = "El token se invalida tras 2 minutos de inactividad en vez de 30.",
            Prioridad = PrioridadTicket.Media,
            Estado = EstadoTicket.Abierto,
            ClienteId = 104,
            AgenteId = null,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-14),
            FechaActualizacion = DateTime.UtcNow.AddDays(-14)
        },
        new Ticket
        {
            Id = 15,
            Codigo = "HD-0015",
            Titulo = "Vulnerabilidad en librería externa de parsing",
            Descripcion = "Actualizar paquete NuGet a versión con parche de seguridad.",
            Prioridad = PrioridadTicket.Critica,
            Estado = EstadoTicket.Resuelto,
            ClienteId = 105,
            AgenteId = 201,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-13),
            FechaActualizacion = DateTime.UtcNow.AddDays(-12)
        },
        new Ticket
        {
            Id = 16,
            Codigo = "HD-0016",
            Titulo = "Habilitar modo oscuro en portal de soporte",
            Descripcion = "Petición recurrente de los usuarios del área de operaciones.",
            Prioridad = PrioridadTicket.Baja,
            Estado = EstadoTicket.Abierto,
            ClienteId = 101,
            AgenteId = null,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-12),
            FechaActualizacion = DateTime.UtcNow.AddDays(-12)
        },
        new Ticket
        {
            Id = 17,
            Codigo = "HD-0017",
            Titulo = "Webhook de facturación no responde",
            Descripcion = "Timeouts constantes al intentar notificar al servicio de contabilidad.",
            Prioridad = PrioridadTicket.Alta,
            Estado = EstadoTicket.Cerrado,
            ClienteId = 102,
            AgenteId = 204,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-11),
            FechaActualizacion = DateTime.UtcNow.AddDays(-9)
        },
        new Ticket
        {
            Id = 18,
            Codigo = "HD-0018",
            Titulo = "Paginación rota en catálogo de productos",
            Descripcion = "Al avanzar a la página 3 vuelve a mostrar elementos de la 1.",
            Prioridad = PrioridadTicket.Media,
            Estado = EstadoTicket.EnProceso,
            ClienteId = 103,
            AgenteId = 204,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-10),
            FechaActualizacion = DateTime.UtcNow.AddDays(-8)
        },
        new Ticket
        {
            Id = 19,
            Codigo = "HD-0019",
            Titulo = "Fallo de conexión a base de datos réplica",
            Descripcion = "El pool de conexiones agota los sockets disponibles en horas pico.",
            Prioridad = PrioridadTicket.Critica,
            Estado = EstadoTicket.Abierto,
            ClienteId = 104,
            AgenteId = null,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-9),
            FechaActualizacion = DateTime.UtcNow.AddDays(-9)
        },
        new Ticket
        {
            Id = 20,
            Codigo = "HD-0020",
            Titulo = "Agregar campo de teléfono móvil en perfil",
            Descripcion = "Permitir guardar código de país y número celular.",
            Prioridad = PrioridadTicket.Baja,
            Estado = EstadoTicket.Resuelto,
            ClienteId = 105,
            AgenteId = 202,
            Reaperturas = 1,
            FechaCreacion = DateTime.UtcNow.AddDays(-8),
            FechaActualizacion = DateTime.UtcNow.AddDays(-6)
        },
        new Ticket
        {
            Id = 21,
            Codigo = "HD-0021",
            Titulo = "Certificado SSL por vencer en 7 días",
            Descripcion = "Renovar certificado wildcard en balanceador de carga.",
            Prioridad = PrioridadTicket.Alta,
            Estado = EstadoTicket.Abierto,
            ClienteId = 101,
            AgenteId = null,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-7),
            FechaActualizacion = DateTime.UtcNow.AddDays(-7)
        },
        new Ticket
        {
            Id = 22,
            Codigo = "HD-0022",
            Titulo = "Error tipográfico en mensaje de recuperación",
            Descripcion = "Corregir ortografía en el correo de restablecimiento de contraseña.",
            Prioridad = PrioridadTicket.Baja,
            Estado = EstadoTicket.Cerrado,
            ClienteId = 102,
            AgenteId = 202,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-6),
            FechaActualizacion = DateTime.UtcNow.AddDays(-5)
        },
        new Ticket
        {
            Id = 23,
            Codigo = "HD-0023",
            Titulo = "Búsqueda por palabra clave no retorna coincidencias parciales",
            Descripcion = "Solo encuentra resultados con coincidencia exacta completa.",
            Prioridad = PrioridadTicket.Media,
            Estado = EstadoTicket.Abierto,
            ClienteId = 103,
            AgenteId = null,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-5),
            FechaActualizacion = DateTime.UtcNow.AddDays(-5)
        },
        new Ticket
        {
            Id = 24,
            Codigo = "HD-0024",
            Titulo = "Fallo masivo en procesamiento batch nocturno",
            Descripcion = "El job de las 02:00 AM abortó dejando transacciones a medias.",
            Prioridad = PrioridadTicket.Critica,
            Estado = EstadoTicket.EnProceso,
            ClienteId = 104,
            AgenteId = 201,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-3),
            FechaActualizacion = DateTime.UtcNow.AddDays(-2)
        },
        new Ticket
        {
            Id = 25,
            Codigo = "HD-0025",
            Titulo = "Revisión de logs de auditoría por solicitud legal",
            Descripcion = "Extraer bitácora de accesos del mes previo para inspección.",
            Prioridad = PrioridadTicket.Media,
            Estado = EstadoTicket.Abierto,
            ClienteId = 105,
            AgenteId = null,
            Reaperturas = 0,
            FechaCreacion = DateTime.UtcNow.AddDays(-1),
            FechaActualizacion = DateTime.UtcNow.AddDays(-1)
        }
    };

    public (List<Ticket> Tickets, int Total) ListarTickets(ReqFiltro filtros)
    {
        int paginaActual = (filtros.Pagina.HasValue && filtros.Pagina.Value >= 1) ? filtros.Pagina.Value : 1;
        int tamanoPagina = (filtros.Tamano.HasValue && filtros.Tamano.Value >= 1) ? filtros.Tamano.Value : 10;

        IEnumerable<Ticket> lista = Tickets;

        // 1. Filtrado
        lista = lista.Where(t =>
            (!filtros.Estado.HasValue || t.Estado == filtros.Estado.Value) &&
            (!filtros.Prioridad.HasValue || t.Prioridad == filtros.Prioridad.Value) &&
            (!filtros.ClienteId.HasValue || t.ClienteId == filtros.ClienteId.Value) &&
            (!filtros.Desde.HasValue || t.FechaCreacion >= filtros.Desde.Value) &&
            (!filtros.Hasta.HasValue || t.FechaCreacion <= filtros.Hasta.Value) &&
            (string.IsNullOrWhiteSpace(filtros.Q) ||
             t.Titulo.Contains(filtros.Q, StringComparison.OrdinalIgnoreCase) ||
             t.Descripcion.Contains(filtros.Q, StringComparison.OrdinalIgnoreCase) ||
             t.Codigo.Contains(filtros.Q, StringComparison.OrdinalIgnoreCase))
        );

        // 2. Ordenamiento
        var orden = filtros.Orden?.Trim();
        bool desc = !string.IsNullOrEmpty(orden) && orden.StartsWith('-');
        var campo = desc ? orden![1..] : orden;

        lista = (campo?.ToLowerInvariant(), desc) switch
        {
            ("fecha", false) => lista.OrderBy(t => t.FechaCreacion),
            ("fecha", true) => lista.OrderByDescending(t => t.FechaCreacion),
            ("prioridad", false) => lista.OrderBy(t => t.Prioridad),
            ("prioridad", true) => lista.OrderByDescending(t => t.Prioridad),
            ("estado", false) => lista.OrderBy(t => t.Estado),
            ("estado", true) => lista.OrderByDescending(t => t.Estado),
            _ => lista.OrderByDescending(t => t.FechaCreacion)
        };

        var listaTotal = lista.ToList();
        int total = listaTotal.Count;

        // 3. Paginación
        var items = listaTotal
            .Skip((paginaActual - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToList();

        return (items, total);
    }

    public Ticket? ObtenerTicket(int id) =>
        Tickets.FirstOrDefault(t => t.Id == id);

    public Ticket? ObtenerTicketCodigo(string codigo) => 
        Tickets.FirstOrDefault(t => string.Equals(t.Codigo, codigo, StringComparison.OrdinalIgnoreCase));

    public Ticket CrearTicket(Ticket ticket)
    {
        if (ticket.Id <= 0)
        {
            ticket.Id = Tickets.Count > 0 ? Tickets.Max(t => t.Id) + 1 : 1;
        }
        Tickets.Add(ticket);
        return ticket;
    }

    public Ticket ActualizarTicket(Ticket ticket)
    {
        var index = Tickets.FindIndex(t => t.Id == ticket.Id);
        if (index >= 0)
        {
            Tickets[index] = ticket;
        }
        return ticket;
    }

    public ResumenTickets ObtenerResumenTickets()
    {
        int total = Tickets.Count;

        var porEstado = Enum.GetValues<EstadoTicket>()
            .ToDictionary(e => e.ToString(), e => Tickets.Count(t => t.Estado == e));

        var porPrioridad = Enum.GetValues<PrioridadTicket>()
            .ToDictionary(p => p.ToString(), p => Tickets.Count(t => t.Prioridad == p));

        return new ResumenTickets(total, porEstado, porPrioridad);
    }

    public int ContarTicketsEnProcesoPorAgente(int agenteId)
    {
        return Tickets.Count(t => t.AgenteId == agenteId && t.Estado == EstadoTicket.EnProceso);
    }
}
