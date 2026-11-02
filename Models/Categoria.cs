using System.ComponentModel.DataAnnotations;

namespace GestionalePersone.Models;

public class Categoria
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Il nome della categoria è obbligatorio.")]
    [StringLength(50, ErrorMessage = "Il nome della categoria deve avere al massimo 50 caratteri.")]
    public string Nome { get; set; } = string.Empty;

    // Icona (Bootstrap Icons) e colore: vedi AspettoCategoria per i valori ammessi.
    public string Icona { get; set; } = AspettoCategoria.IconaPredefinita;

    public string Colore { get; set; } = AspettoCategoria.ColorePredefinito;
}
