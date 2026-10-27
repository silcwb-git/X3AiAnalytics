namespace X3AiAnalytics.Domain.Entities;

public class Cliente
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Regiao { get; set; } = string.Empty;
    public string Segmento { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}