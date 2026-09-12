using ClosedXML.Excel;
using Application.Dtos;
using Application.Interfaces;

namespace Infrastructure.Services;

public class LectorClientesExcel : ILectorClientesArchivo
{
    public List<(int fila, CrearClienteDto dto)> Leer(Stream contenido, string extension)
    {
        return extension == ".csv"
            ? LeerCsv(contenido)
            : LeerExcel(contenido);
    }

    private static List<(int, CrearClienteDto)> LeerExcel(Stream contenido)
    {
        var resultado = new List<(int, CrearClienteDto)>();

        using var libro = new XLWorkbook(contenido);
        // "using" = cierra y libera el archivo al terminar, pase lo que pase.
        // Sin él, el archivo quedaría abierto ocupando memoria.

        var hoja = libro.Worksheet(1);   // la primera hoja

        var filas = hoja.RangeUsed()?.RowsUsed().Skip(1);
        // RangeUsed = solo el rectángulo con datos, no el millón de
        // filas vacías que tiene toda hoja de Excel.
        //
        // Skip(1) = saltar la primera fila, que son los encabezados.

        if (filas is null) return resultado;

        foreach (var fila in filas)
        {
            resultado.Add((
                fila.RowNumber(),
                // El número real de Excel, el que el usuario ve a la izquierda.

                new CrearClienteDto
                {
                    Nombres = Celda(fila, 1),
                    Apellidos = Celda(fila, 2),
                    Identificacion = Celda(fila, 3),
                    Correo = Celda(fila, 4),
                    Telefono = Celda(fila, 5),
                    Direccion = Celda(fila, 6),
                    ReferenciaDireccion = Celda(fila, 7)
                }
            ));
        }

        return resultado;
    }

    private static string Celda(IXLRangeRow fila, int columna) =>
        fila.Cell(columna).GetString().Trim();
    // GetString() y no GetValue<string>(): si alguien escribió la cédula
    // como número, Excel la guarda como número y GetValue reventaría.
    // GetString convierte lo que sea a texto.
    //
    // Trim() quita espacios sobrantes. En archivos hechos a mano
    // hay espacios invisibles POR TODAS PARTES.

    private static List<(int, CrearClienteDto)> LeerCsv(Stream contenido)
    {
        var resultado = new List<(int, CrearClienteDto)>();
        using var lector = new StreamReader(contenido);

        int numeroFila = 0;
        string? linea;

        while ((linea = lector.ReadLine()) is not null)
        {
            numeroFila++;
            if (numeroFila == 1) continue;              // encabezados
            if (string.IsNullOrWhiteSpace(linea)) continue;

            var partes = linea.Split(';');
            // Punto y coma, no coma: en configuración regional de
            // Latinoamérica, Excel exporta CSV con punto y coma,
            // porque la coma se usa como separador decimal.
            //
            // AVISO HONESTO: este parser es básico. Si un campo trae
            // un punto y coma dentro de comillas, se rompe. Para
            // producción se usa una librería como CsvHelper. Lo dejo
            // simple a propósito para que se entienda qué hace.

            if (partes.Length < 7) continue;

            resultado.Add((numeroFila, new CrearClienteDto
            {
                Nombres = partes[0].Trim(),
                Apellidos = partes[1].Trim(),
                Identificacion = partes[2].Trim(),
                Correo = partes[3].Trim(),
                Telefono = partes[4].Trim(),
                Direccion = partes[5].Trim(),
                ReferenciaDireccion = partes[6].Trim()
            }));
        }

        return resultado;
    }
}