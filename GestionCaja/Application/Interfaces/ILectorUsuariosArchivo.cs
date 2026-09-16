using Application.Dtos;

namespace Application.Interfaces;

public interface ILectorUsuariosArchivo
{
    List<(int fila, CrearUsuarioDto dto)> Leer(Stream contenido, string extension);
}