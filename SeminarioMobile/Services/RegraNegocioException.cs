namespace SeminarioMobile.Services;

// Erro de validação de negócio (equivalente às HTTPException do backend
// atual) — a UI mostra a Message diretamente para o usuário.
public class RegraNegocioException(string message) : Exception(message);
