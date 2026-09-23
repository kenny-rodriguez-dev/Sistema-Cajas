namespace Domain.Entities;

public class Pago
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    // Viene del diagrama original. Aquí además sostiene la regla de
    // "un pago por cliente por mes", que no se podría escribir si
    // solo tuviera el contrato.

    public int ContratoId { get; set; }
    public Contrato Contrato { get; set; } = null!;

    public string Periodo { get; set; } = string.Empty;
    // "2026-09". Siempre 7 caracteres: año, guion, mes.
    // Texto y no fecha porque lo que se paga es UN MES, no un día.
    // Y coincide exacto con lo que devuelve un <input type="month">.

    public decimal Monto { get; set; }
    // decimal, nunca double. Dinero.
    // Y es una COPIA del precio al momento de cobrar, no un enlace.

    public int FormaPagoId { get; set; }
    public FormaPago FormaPago { get; set; } = null!;
    // También una copia: la forma de pago que tenía el contrato ese día.
    // Si después la cambian, este pago sigue diciendo cómo se cobró.

    public DateTime FechaPago { get; set; } = DateTime.UtcNow;

    public int RegistradoPorUsuarioId { get; set; }
    public Usuario RegistradoPorUsuario { get; set; } = null!;
}