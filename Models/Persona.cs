using System.ComponentModel.DataAnnotations;

namespace GestionalePersone.Models;

public class Persona
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Il nome è obbligatorio.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Il nome deve avere al massimo 100 caratteri.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Il cognome è obbligatorio.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Il cognome deve avere al massimo 100 caratteri.")]
    public string Cognome { get; set; } = string.Empty;
}
