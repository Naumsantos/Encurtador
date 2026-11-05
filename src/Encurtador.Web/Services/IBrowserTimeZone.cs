namespace Encurtador.Web.Services;

public interface IBrowserTimeZone
{
    // Deslocamento do fuso do navegador para a data e hora locais informadas.
    ValueTask<TimeSpan> GetOffsetAsync(DateTime localDateTime);

    // Deslocamento do fuso do navegador no instante informado.
    ValueTask<TimeSpan> GetOffsetAtAsync(DateTimeOffset instant);
}
