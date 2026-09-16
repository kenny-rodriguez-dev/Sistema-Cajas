using ClosedXML.Excel;
using Application.Dtos;
using Application.Interfaces;

namespace Infrastructure.Services;

public class LectorUsuariosExcel : ILectorUsuariosArchivo
{
    // Columnas esperadas: Username | Password | Correo | RolId

    public List<(int fila, CrearUsuarioDto dto)> Leer(Stream contenido, string extension) =>
        extension == ".csv" ? LeerCsv(contenido) : LeerExcel(contenido);

    private static List<(int, CrearUsuarioDto)> LeerExcel(Stream contenido)
    {
        var resultado = new List<(int, CrearUsuarioDto)>();
        using var libro = new XLWorkbook(contenido);
        var hoja = libro.Worksheet(1);
        var filas = hoja.RangeUsed()?.RowsUsed().Skip(1);
        if (filas is null) return resultado;

        foreach (var fila in filas)
        {
            int.TryParse(fila.Cell(4).GetString().Trim(), out int rolId);
            // TryParse en vez de Parse: si la celda trae texto basura,
            // deja rolId en 0 en vez de lanzar excepcion. Y un 0 falla
            // la validacion [Range(2,3)] con un mensaje claro.

            resultado.Add((fila.RowNumber(), new CrearUsuarioDto
            {
                Username = fila.Cell(1).GetString().Trim(),
                Password = fila.Cell(2).GetString().Trim(),
                Correo = fila.Cell(3).GetString().Trim(),
                RolId = rolId
            }));
        }
        return resultado;
    }

    private static List<(int, CrearUsuarioDto)> LeerCsv(Stream contenido)
    {
        var resultado = new List<(int, CrearUsuarioDto)>();
        using var lector = new StreamReader(contenido);
        int numeroFila = 0;
        string? linea;

        while ((linea = lector.ReadLine()) is not null)
        {
            numeroFila++;
            if (numeroFila == 1 || string.IsNullOrWhiteSpace(linea)) continue;

            var partes = linea.Split(';');
            if (partes.Length < 4) continue;

            int.TryParse(partes[3].Trim(), out int rolId);

            resultado.Add((numeroFila, new CrearUsuarioDto
            {
                Username = partes[0].Trim(),
                Password = partes[1].Trim(),
                Correo = partes[2].Trim(),
                RolId = rolId
            }));
        }
        return resultado;
    }
}