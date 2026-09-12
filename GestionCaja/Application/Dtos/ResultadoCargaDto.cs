namespace Application.Dtos;

public class ErrorFilaDto
{
    public int Fila { get; set; }
    // El número de fila TAL COMO LO VE EL USUARIO EN EXCEL.
    // Si le digo "error en el índice 3" tiene que ponerse a contar.
    // Si le digo "error en la fila 5", abre el archivo y la ve.

    public List<string> Errores { get; set; } = new();
}

public class ResultadoCargaDto
{
    public bool Exitoso { get; set; }
    public int FilasLeidas { get; set; }
    public int FilasImportadas { get; set; }
    public List<ErrorFilaDto> Errores { get; set; } = new();
    public string Mensaje { get; set; } = string.Empty;
}