using Domain.Entities;

namespace Application.Interfaces;

// EL CONTRATO. Dice QUÉ se puede hacer, jamás CÓMO.
//
// Fíjate en lo que NO aparece por ningún lado en este archivo:
// ni Entity Framework, ni SQL, ni SQL Server, ni una cadena de conexión.
// Este archivo podría cumplirse con una base de datos, con un archivo
// de texto, o con una lista en memoria para pruebas.
//
// La "I" del inicio es convención de C# para interfaces.
public interface IRepositorioClientes
{
    Task<List<Cliente>> ListarActivosAsync();

    Task<Cliente?> ObtenerPorIdAsync(int id);

    Task<bool> ExisteIdentificacionAsync(string identificacion);

    Task AgregarAsync(Cliente cliente);
    // Solo firmas de métodos, sin cuerpo. Una interfaz es una promesa:
    // "quien implemente esto, tiene que saber hacer estas cuatro cosas".
}