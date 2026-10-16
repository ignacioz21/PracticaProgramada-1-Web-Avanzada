using System.ComponentModel.DataAnnotations;

namespace Abstracciones.Ticket;

public class Ticket
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El título del ticket es obligatorio.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "El título del ticket debe tener entre 3 y 200 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción del ticket es obligatoria.")]
    [StringLength(2000, MinimumLength = 5, ErrorMessage = "La descripción del ticket debe tener entre 5 y 2000 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [EnumDataType(typeof(PrioridadTicket), ErrorMessage = "La prioridad especificada no es válida. Valores permitidos: Baja, Media, Alta, Critica.")]
    public PrioridadTicket Prioridad { get; set; }

    [EnumDataType(typeof(EstadoTicket), ErrorMessage = "El estado especificado no es válido. Valores permitidos: Abierto, EnProceso, Resuelto, Cerrado.")]
    public EstadoTicket Estado { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El ClienteId es obligatorio y debe ser un número entero mayor o igual a 1.")]
    public int ClienteId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El AgenteId debe ser un número entero mayor o igual a 1.")]
    public int? AgenteId { get; set; }

    public int Reaperturas { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}
