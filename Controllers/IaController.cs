using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using GestionalePersone.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionalePersone.Controllers;

// Modulo IA. Pagina IA 3D: dalla descrizione di un pezzo a forma e misure
// (Services/RiconoscitoreIa3d.cs), poi il file STL della forma base (Services/FormeBaseIa3d.cs).
[ApiController]
[Authorize(Policy = Politiche.Ia)]
[Route("api/ia")]
public class IaController : ControllerBase
{
    private readonly GeneratoreStl _generatore;

    public IaController(GeneratoreStl generatore)
    {
        _generatore = generatore;
    }

    public class RiconosciRequest
    {
        [Required(ErrorMessage = "Descrivi il pezzo da creare.")]
        [StringLength(RiconoscitoreIa3d.LunghezzaMassima, ErrorMessage = "La descrizione deve avere al massimo 300 caratteri.")]
        public string Testo { get; set; } = string.Empty;
    }

    public class StlRequest
    {
        public Dictionary<string, JsonElement>? Parametri { get; set; }
    }

    // Pezzo riconosciuto: misure per il modello (Parametri, da rimandare a /3d/stl), centri dei
    // fori e ingombro per il disegno, misure scritte per l'utente e cosa è stato capito.
    public record RiconoscimentoDto(
        string Forma,
        string NomeForma,
        Dictionary<string, object> Parametri,
        FormeBaseIa3d.Foro[] Fori,
        FormeBaseIa3d.Ingombro Ingombro,
        List<RiconoscitoreIa3d.Misura> Misure,
        List<RiconoscitoreIa3d.Parte> Capito,
        List<string> NonCapito,
        List<string> Avvisi);

    [HttpPost("3d/riconosci")]
    public ActionResult<RiconoscimentoDto> Riconosci(RiconosciRequest richiesta)
    {
        var testo = richiesta.Testo.Trim();
        if (testo.Length == 0)
            return BadRequest(new { messaggio = "Descrivi il pezzo da creare." });

        RiconoscitoreIa3d.Risultato r;
        try
        {
            r = RiconoscitoreIa3d.Riconosci(testo);
        }
        catch (RegexMatchTimeoutException)
        {
            return UnprocessableEntity(new { messaggio = "Descrizione troppo complicata: prova con una frase più semplice." });
        }

        if (r.Forma == null)
            return UnprocessableEntity(new
            {
                messaggio = "Non ho capito che pezzo vuoi. Scrivi la forma (cubo, blocco, piastra, cilindro, " +
                            "tubo, sfera o cono) e le misure, es. \"cilindro diametro 30 alto 50\"."
            });

        // Le misure riconosciute passano dagli stessi controlli dei valori inviati per l'STL.
        var parametri = JsonSerializer.SerializeToElement(r.Parametri)
            .EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
        var (_, pezzo, errori) = FormeBaseIa3d.Valida(parametri);
        if (pezzo == null)
            return UnprocessableEntity(new { messaggio = string.Join(" ", errori) });

        return new RiconoscimentoDto(pezzo.Forma, pezzo.NomeForma, pezzo.Parametri, pezzo.Fori, pezzo.Ingombro,
            r.Misure, r.Capito, r.NonCapito, r.Avvisi);
    }

    // Restituisce il file STL della forma base con le misure ricevute.
    [HttpPost("3d/stl")]
    public async Task<IActionResult> GeneraStl(StlRequest richiesta, CancellationToken annulla)
    {
        var (valori, pezzo, errori) = FormeBaseIa3d.Valida(richiesta.Parametri);
        if (pezzo == null)
            return BadRequest(new { messaggio = string.Join(" ", errori) });

        try
        {
            var stl = await _generatore.Genera(FormeBaseIa3d.Definizione, valori, annulla);
            return File(stl, "model/stl", pezzo.Forma + ".stl");
        }
        catch (GeneratoreStl.OpenScadNonDisponibile e)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { messaggio = e.Message });
        }
        catch (GeneratoreStl.GenerazioneFallita e)
        {
            return UnprocessableEntity(new { messaggio = e.Message });
        }
    }
}
