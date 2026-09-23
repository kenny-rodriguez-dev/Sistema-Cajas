using Domain.Entities;

namespace Application.Interfaces;

public interface IRepositorioClientes
{
    Task<List<Cliente>> ListarAsync(bool incluirInactivos);
    Task<Cliente?> ObtenerPorIdAsync(int id);
    Task<Cliente?> ObtenerParaEditarAsync(int id);
    Task<Cliente?> BuscarPorIdentificacionAsync(string identificacion);
    Task<bool> ExisteIdentificacionAsync(string identificacion);
    Task<bool> TieneContratoActivoAsync(int clienteId);
    Task AgregarAsync(Cliente cliente);
    Task<List<string>> ObtenerIdentificacionesAsync(List<string> identificaciones);
    Task AgregarVariosAsync(List<Cliente> clientes);
    Task GuardarAsync();
}