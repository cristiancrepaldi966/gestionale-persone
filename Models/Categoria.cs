using System.ComponentModel.DataAnnotations;

namespace GestionalePersone.Models;

public class Categoria
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Il nome della categoria è obbligatorio.")]
    [StringLength(50, ErrorMessage = "Il nome della categoria deve avere al massimo 50 caratteri.")]
    public string Nome { get; set; } = string.Empty;
}
