using System.ComponentModel.DataAnnotations;

namespace Abstracciones.Ticket.Request;

public class ReqActualizarEstado
{
    [Required(ErrorMessage = "El estado destino es obligatorio.")]
    [EnumDataType(typeof(EstadoTicket), ErrorMessage = "El estado especificado no es válido. Valores permitidos: Abierto, EnProceso, Resuelto, Cerrado.")]
    public EstadoTicket? Estado { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El AgenteId debe ser un número entero mayor o igual a 1.")]
    public int? AgenteId { get; set; }

    [EnumDataType(typeof(PrioridadTicket), ErrorMessage = "La prioridad especificada no es válida. Valores permitidos: Baja, Media, Alta, Critica.")]
    public PrioridadTicket? Prioridad { get; set; }
}
