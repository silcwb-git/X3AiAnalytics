using X3AiAnalytics.Domain.Enums;

namespace X3AiAnalytics.Domain.Entities;

public class OrdemProducao
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string Produto { get; set; } = string.Empty;
    public string Linha { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; }
    public DateTime DataPrevisao { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorPrevisto { get; set; }
    public StatusProducao Status { get; set; }
}