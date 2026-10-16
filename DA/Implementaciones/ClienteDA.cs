using Abstracciones.Modelos;
using DA.Interfaces.Cliente;

namespace DA.Implementaciones;

public class ClienteDA : IClienteDA
{
    private static readonly List<Cliente> Clientes = new()
    {
        new Cliente { Id = 101, Nombre = "Ana Sofía Gómez", Email = "ana.gomez@empresa.com" },
        new Cliente { Id = 102, Nombre = "Carlos Morales", Email = "carlos.morales@empresa.com" },
        new Cliente { Id = 103, Nombre = "Beatriz Fernández", Email = "beatriz.f@empresa.com" },
        new Cliente { Id = 104, Nombre = "David Navarro", Email = "david.navarro@empresa.com" },
        new Cliente { Id = 105, Nombre = "Elena Vargas", Email = "elena.vargas@empresa.com" }
    };

    public List<Cliente> ListarClientes() => Clientes;

    public Cliente? ObtenerCliente(int id) => Clientes.FirstOrDefault(c => c.Id == id);

    public bool ExisteCliente(int id) => Clientes.Any(c => c.Id == id);
}
