using X3AiAnalytics.Domain.Enums;

namespace X3AiAnalytics.Domain.Entities;

public class TituloReceber
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public DateTime DataVencimento { get; set; }
    public decimal Valor { get; set; }
    public StatusTitulo Status { get; set; }
}