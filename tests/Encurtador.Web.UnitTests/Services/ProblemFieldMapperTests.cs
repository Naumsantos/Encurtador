using Encurtador.Web.Services;

namespace Encurtador.Web.UnitTests.Services;

public sealed class ProblemFieldMapperTests
{
    [Theory]
    [InlineData("Informe uma URL absoluta HTTP ou HTTPS válida com até 2048 caracteres.", FormField.DestinationUrl)]
    [InlineData("A data de expiração deve ser futura.", FormField.Expiration)]
    [InlineData("O código deve ter de 4 a 20 caracteres e conter apenas letras ASCII, números, hífen ou sublinhado.", FormField.Code)]
    [InlineData("O código informado já está em uso.", FormField.Code)]
    [InlineData("O corpo da solicitação contém JSON inválido ou dados obrigatórios ausentes.", FormField.General)]
    [InlineData("Qualquer outro texto.", FormField.General)]
    [InlineData("", FormField.General)]
    [InlineData(null, FormField.General)]
    public void Maps_known_messages_and_defaults_to_general(string? detail, FormField expected)
    {
        Assert.Equal(expected, ProblemFieldMapper.Map(detail));
    }
}
