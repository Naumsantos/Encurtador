namespace Encurtador.Web.Services;

public static class ProblemFieldMapper
{
    public const string InvalidUrl = "Informe uma URL absoluta HTTP ou HTTPS válida com até 2048 caracteres.";
    public const string PastExpiration = "A data de expiração deve ser futura.";
    public const string InvalidCode = "O código deve ter de 4 a 20 caracteres e conter apenas letras ASCII, números, hífen ou sublinhado.";
    public const string CodeInUse = "O código informado já está em uso.";

    public static FormField Map(string? detail) => detail switch
    {
        InvalidUrl => FormField.DestinationUrl,
        PastExpiration => FormField.Expiration,
        InvalidCode or CodeInUse => FormField.Code,
        _ => FormField.General
    };
}
