namespace Application.Interfaces;

public interface IServicioRecuperacion
{
    (string tokenPlano, string tokenHash) GenerarToken();
    bool VerificarToken(string tokenPlano, string tokenHash);
}