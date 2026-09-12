using Application.Dtos;

namespace Application.Interfaces;

public interface ILectorClientesArchivo
{
    List<(int fila, CrearClienteDto dto)> Leer(Stream contenido, string extension);
    // Devuelve pares: número de fila y los datos de esa fila.
    //
    // Fíjate que devuelvo CrearClienteDto, el MISMO que usa el formulario.
    // Eso no es casualidad: así las reglas de validación son idénticas
    // para el formulario y para el Excel. Una sola fuente de verdad.
    //
    // Y fíjate en lo que NO aparece aquí: ni ClosedXML, ni nada de Excel.
    // La capa Application solo dice "necesito algo que sepa leer clientes
    // de un archivo". Con qué librería, es problema de Infrastructure.
}