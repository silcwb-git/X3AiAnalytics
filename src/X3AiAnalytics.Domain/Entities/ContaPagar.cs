using X3AiAnalytics.Domain.Enums;

namespace X3AiAnalytics.Domain.Entities;

public class ContaPagar
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int CentroCustoId { get; set; }
    public CentroCusto? CentroCusto { get; set; }
    public DateTime DataVencimento { get; set; }
    public decimal Valor { get; set; }
    public StatusTitulo Status { get; set; }
}