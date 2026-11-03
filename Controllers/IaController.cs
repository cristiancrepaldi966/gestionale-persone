using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using GestionalePersone.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionalePersone.Controllers;

// Modulo IA. Pagina IA 3D: dalla descrizione di un pezzo a forma e misure, con domande
// quando manca qualcosa (Services/RiconoscitoreIa3d.cs); correzione a mano delle misure;
// file STL della forma base (Services/FormeBaseIa3d.cs).
[ApiController]
[Authorize(Policy = Politiche.Ia)]
[Route("api/ia")]
public class IaController : ControllerBase
{
    private const int MaxRisposte = 40;
    private const int LunghezzaMassimaRisposta = 40;

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

        // Risposte alle domande già fatte: codice della domanda → valore (testo o numero).
        public Dictionary<string, JsonElement>? Risposte { get; set; }
    }

    public class ParametriRequest
    {
        public Dictionary<string, JsonElement>? Parametri { get; set; }
    }

    // Esito, in tre casi:
    //  - Domande non vuote: serve ancora una risposta (si pone la prima);
    //  - Errori non vuoti: le misure non vanno bene, si correggono a mano partendo da Parametri;
    //  - altrimenti il pezzo è pronto: Parametri (da rimandare a /3d/stl), Fori e Ingombro per il disegno.
    public record RiconoscimentoDto(
        string? Forma,
        string? NomeForma,
        List<RiconoscitoreIa3d.Domanda> Domande,
        Dictionary<string, object>? Parametri,
        FormeBaseIa3d.Foro[]? Fori,
        FormeBaseIa3d.Ingombro? Ingombro,
        List<RiconoscitoreIa3d.Misura> Misure,
        List<RiconoscitoreIa3d.Parte> Capito,
        List<string> NonCapito,
        List<string> Avvisi,
        List<string> Errori);

    // Campi della correzione a mano, per ogni forma.
    public record CampoDto(string Nome, string Etichetta, decimal Min, decimal Max);

    public record FormaDto(string Codice, string Nome, CampoDto[] Misure, bool Fori, RiconoscitoreIa3d.Opzione[] PosizioniFori);

    [HttpGet("3d/forme")]
    public IEnumerable<FormaDto> GetForme()
    {
        CampoDto Campo(string nome, string etichetta)
        {
            var p = FormeBaseIa3d.Definizione.Parametri.First(x => x.Nome == nome);
            return new CampoDto(nome, etichetta, p.Min!.Value, p.Max!.Value);
        }

        return FormeBaseIa3d.NomiForme.Select(f => new FormaDto(
            f.Key, f.Value,
            FormeBaseIa3d.MisureDi(f.Key).Select(m => Campo(m.Nome, m.Etichetta)).ToArray(),
            FormeBaseIa3d.ConFori(f.Key),
            FormeBaseIa3d.PosizioniPer(f.Key)
                .Select(p => new RiconoscitoreIa3d.Opzione(p, char.ToUpperInvariant(FormeBaseIa3d.NomePosizione(p)[0]) +
                                                            FormeBaseIa3d.NomePosizione(p)[1..]))
                .ToArray()));
    }

    [HttpPost("3d/riconosci")]
    public ActionResult<RiconoscimentoDto> Riconosci(RiconosciRequest richiesta)
    {
        var testo = richiesta.Testo.Trim();
        if (testo.Length == 0)
            return BadRequest(new { messaggio = "Descrivi il pezzo da creare." });

        RiconoscitoreIa3d.Risultato r;
        try
        {
            r = RiconoscitoreIa3d.Riconosci(testo, LeggiRisposte(richiesta.Risposte));
        }
        catch (RegexMatchTimeoutException)
        {
            return UnprocessableEntity(new { messaggio = "Descrizione troppo complicata: prova con una frase più semplice." });
        }

        var nomeForma = r.Forma == null ? null : FormeBaseIa3d.NomeForma(r.Forma);
        if (r.Forma == null || r.Domande.Count > 0)
            return new RiconoscimentoDto(r.Forma, nomeForma, r.Domande, null, null, null,
                r.Misure, r.Capito, r.NonCapito, r.Avvisi, []);

        // Le misure riconosciute passano dagli stessi controlli di quelle corrette a mano.
        var (_, pezzo, errori) = FormeBaseIa3d.Valida(InJson(r.Parametri));
        if (pezzo == null)
            return new RiconoscimentoDto(r.Forma, nomeForma, [], r.Parametri, null, null,
                r.Misure, r.Capito, r.NonCapito, r.Avvisi, errori);

        return new RiconoscimentoDto(pezzo.Forma, pezzo.NomeForma, [], pezzo.Parametri, pezzo.Fori, pezzo.Ingombro,
            r.Misure, r.Capito, r.NonCapito, r.Avvisi, []);
    }

    // Misure corrette a mano: controllate e restituite con fori e ingombro per il disegno.
    [HttpPost("3d/pezzo")]
    public ActionResult<RiconoscimentoDto> ControllaPezzo(ParametriRequest richiesta)
    {
        var (_, pezzo, errori) = FormeBaseIa3d.Valida(richiesta.Parametri);
        if (pezzo == null)
            return UnprocessableEntity(new { messaggio = string.Join(" ", errori) });

        return new RiconoscimentoDto(pezzo.Forma, pezzo.NomeForma, [], pezzo.Parametri, pezzo.Fori, pezzo.Ingombro,
            RiconoscitoreIa3d.MisureDi(pezzo.Parametri), [], [], [], []);
    }

    // Restituisce il file STL della forma base con le misure ricevute.
    [HttpPost("3d/stl")]
    public async Task<IActionResult> GeneraStl(ParametriRequest richiesta, CancellationToken annulla)
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

    // Risposte come testo (i numeri nel formato JSON, es. "4.5"); valori strani si scartano
    // e la domanda verrà fatta di nuovo.
    private static Dictionary<string, string> LeggiRisposte(Dictionary<string, JsonElement>? ricevute)
    {
        var risposte = new Dictionary<string, string>();
        foreach (var (codice, valore) in (ricevute ?? new Dictionary<string, JsonElement>()).Take(MaxRisposte))
        {
            var testo = valore.ValueKind switch
            {
                JsonValueKind.Number => valore.GetRawText(),
                JsonValueKind.String => valore.GetString()?.Trim(),
                _ => null,
            };
            if (codice.Length <= LunghezzaMassimaRisposta && testo is { Length: > 0 and <= LunghezzaMassimaRisposta })
                risposte[codice] = testo;
        }
        return risposte;
    }

    private static Dictionary<string, JsonElement> InJson(Dictionary<string, object> parametri) =>
        JsonSerializer.SerializeToElement(parametri).EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
}
