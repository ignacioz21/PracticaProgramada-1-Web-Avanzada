namespace Abstracciones.Excepciones;

public class TicketNotFoundException : Exception
{
    public TicketNotFoundException(string message) : base(message) { }
}

public class TicketConflictException : Exception
{
    public TicketConflictException(string message) : base(message) { }
}

public class TicketValidationException : Exception
{
    public IDictionary<string, string[]> Errores { get; }

    public TicketValidationException(string message) : base(message)
    {
        Errores = new Dictionary<string, string[]>
        {
            ["general"] = [message]
        };
    }

    public TicketValidationException(string campo, string message) : base(message)
    {
        Errores = new Dictionary<string, string[]>
        {
            [campo] = [message]
        };
    }

    public TicketValidationException(IDictionary<string, string[]> errores, string message) : base(message)
    {
        Errores = errores;
    }
}
