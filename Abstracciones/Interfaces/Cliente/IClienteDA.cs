namespace DA.Interfaces.Cliente;

public interface IClienteDA
{
    List<Abstracciones.Modelos.Cliente> ListarClientes();
    Abstracciones.Modelos.Cliente? ObtenerCliente(int id);
    bool ExisteCliente(int id);
}
