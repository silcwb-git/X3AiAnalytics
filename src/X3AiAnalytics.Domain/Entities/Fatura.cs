namespace X3AiAnalytics.Domain.Entities;

public class Fatura
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public DateTime DataEmissao { get; set; }
    public string Produto { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}