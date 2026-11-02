namespace GestionalePersone.Models;

public class Uscita
{
    public int Id { get; set; }

    public DateOnly Data { get; set; }

    public decimal Importo { get; set; }

    public string Descrizione { get; set; } = string.Empty;

    public string? Note { get; set; }

    public int CategoriaId { get; set; }

    public Categoria Categoria { get; set; } = null!;

    // Chi ha inserito l'uscita (null se l'utente è stato eliminato).
    public int? CreatoDaId { get; set; }

    public Utente? CreatoDa { get; set; }

    public DateTime CreatoIl { get; set; } = DateTime.UtcNow;
}
