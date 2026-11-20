using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GestionalePersone.Data;
using GestionalePersone.Models;
using GestionalePersone.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Controllers;

// Modulo Fantapolitica: leghe, politici e squadre.
// Leghe: tutti gli utenti del modulo vedono tutte le leghe e ne
// possono creare; chi crea una lega ne fa sempre parte. I membri si scelgono tra gli
// utenti attivi abilitati al modulo (Super admin compresi). Modifica ed eliminazione:
// solo chi ha creato la lega e il Super admin (non l'Amministratore del modulo).
// Politici: tutti li vedono, solo il Super admin li gestisce.
// Squadre: ogni membro ha una squadra per lega con Squadra.PoliticiPerSquadra politici
// (lo stesso politico può stare in più squadre), comprati spendendo al massimo i crediti
// per squadra (li decide il Super admin, come i costi). Il prezzo pagato resta quello anche
// se poi il costo cambia. Tutti i membri vedono le squadre e la classifica della lega.
// La squadra si crea una volta sola: salvarla vale come conferma e poi il giocatore non la
// può più cambiare né eliminare. Il Super admin e l'Amministratore del modulo ("gestori")
// modificano ed eliminano le squadre di tutti, compresa la propria.
// Punti: dagli eventi (FantapoliticaEventiController), vedi Services/PunteggiFantapolitica.
[ApiController]
[Authorize(Policy = Politiche.Fantapolitica)]
[Route("api/fantapolitica")]
public class FantapoliticaController : ControllerBase
{
    private readonly AppDbContext _context;

    public FantapoliticaController(AppDbContext context)
    {
        _context = context;
    }

    public class LegaRequest
    {
        [Required(ErrorMessage = "Il nome della lega è obbligatorio.")]
        [StringLength(100, ErrorMessage = "Il nome deve avere al massimo 100 caratteri.")]
        public string Nome { get; set; } = string.Empty;

        // Utenti scelti come membri (chi ha creato la lega viene aggiunto comunque).
        public List<int>? Membri { get; set; }
    }

    public class PoliticoRequest
    {
        [Required(ErrorMessage = "Il nome è obbligatorio.")]
        [StringLength(100, ErrorMessage = "Il nome deve avere al massimo 100 caratteri.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Il cognome è obbligatorio.")]
        [StringLength(100, ErrorMessage = "Il cognome deve avere al massimo 100 caratteri.")]
        public string Cognome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Il ruolo è obbligatorio.")]
        public string Ruolo { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Il partito deve avere al massimo 100 caratteri.")]
        public string? Partito { get; set; }

        [Range(0, 10000, ErrorMessage = "Il costo deve essere tra 0 e 10000 crediti.")]
        public int Costo { get; set; }
    }

    public class ImpostazioniRequest
    {
        [Range(1, 10000, ErrorMessage = "I crediti per squadra devono essere tra 1 e 10000.")]
        public int CreditiPerSquadra { get; set; }
    }

    public class SquadraRequest
    {
        [Required(ErrorMessage = "Il nome della squadra è obbligatorio.")]
        [StringLength(50, ErrorMessage = "Il nome della squadra deve avere al massimo 50 caratteri.")]
        public string Nome { get; set; } = string.Empty;

        public List<int>? Politici { get; set; }
    }

    public record PersonaDto(int Id, string Nome);

    // Punti: tutti quelli presi dal politico.
    public record PoliticoDto(int Id, string Nome, string Cognome, string Ruolo, string? Partito, int Costo,
        int Squadre, int Punti);

    // Politico in una squadra: Prezzo è quanto è stato pagato, Costo quanto costa oggi,
    // Punti quelli portati alla squadra da quando c'è.
    public record PoliticoAcquistatoDto(int Id, string Nome, string Cognome, string Ruolo, string? Partito,
        int Costo, int Prezzo, int Punti, DateOnly AcquistatoIl);

    // Punti: totale della squadra (solo i politici che ha adesso).
    public record SquadraDto(int Id, string Nome, List<PoliticoAcquistatoDto> Politici, int Spesi,
        int Punti, DateTime AggiornatoIl);

    public record ImpostazioniDto(int CreditiPerSquadra, int PoliticiPerSquadra);

    // Posizione in classifica (a pari punti, stessa posizione); chi non ha squadra ha 0 punti.
    public record MembroDto(int Id, string Nome, SquadraDto? Squadra, int Punti, int Posizione);

    // GestioneSquadre: chi guarda può modificare le squadre dei membri (Super admin e
    // Amministratore del modulo).
    public record LegaDto(int Id, string Nome, PersonaDto? CreatoDa, bool Gestibile, DateTime CreatoIl,
        List<MembroDto> Membri, bool GestioneSquadre);

    // Le mie leghe con la mia squadra (pagina "La mia squadra").
    public record MiaLegaDto(int Id, string Nome, SquadraDto? Squadra);

    // Classifica generale: tutte le squadre di tutte le leghe e tutti i politici.
    public record LegaBreveDto(int Id, string Nome);

    public record SquadraInClassificaDto(int Posizione, PersonaDto Utente, LegaBreveDto Lega, SquadraDto Squadra);

    public record EventoDelPoliticoDto(DateOnly Data, string Voce, int Punti, string? Descrizione);

    public record PoliticoInClassificaDto(int Posizione, PoliticoDto Politico, List<EventoDelPoliticoDto> Eventi);

    public record ClassificaDto(List<SquadraInClassificaDto> Squadre, List<PoliticoInClassificaDto> Politici);

    // GestioneSquadre: posso cambiare la mia squadra anche dopo averla confermata.
    public record MieSquadreDto(int PoliticiPerSquadra, int CreditiPerSquadra, bool GestioneSquadre, List<MiaLegaDto> Leghe);

    private int IdCorrente => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Utenti che possono far parte di una lega: attivi e abilitati al modulo.
    private IQueryable<Utente> UtentiAbilitati => _context.Utenti.AsNoTracking()
        .Where(u => u.Attivo && (u.SuperAdmin || u.Autorizzazioni.Any(a => a.Modulo == Moduli.Fantapolitica)));

    // Nome da mostrare: "Nome Cognome", oppure l'email se mancano.
    private static string NomeVisibile(Utente u)
    {
        var nome = $"{u.Nome} {u.Cognome}".Trim();
        return string.IsNullOrEmpty(nome) ? u.Email : nome;
    }

    private bool PuoGestire(Lega l) => l.CreatoDaId == IdCorrente || User.SuperAdmin();

    // Super admin e Amministratore del modulo: modificano le squadre di tutti.
    private bool GestoreSquadre => User.E(Moduli.Fantapolitica, Livelli.Amministratore);

    private static readonly StringComparer Alfabetico =
        StringComparer.Create(new System.Globalization.CultureInfo("it-IT"), true);

    // Ordine dei politici: per ruolo (Sindaco, Vicesindaco, ...), poi cognome e nome.
    private static IEnumerable<Politico> Ordina(IEnumerable<Politico> politici) => politici
        .OrderBy(p => Array.IndexOf(RuoliPolitico.Tutti, p.Ruolo))
        .ThenBy(p => p.Cognome, Alfabetico)
        .ThenBy(p => p.Nome, Alfabetico);

    private static PoliticoDto Dto(Politico p, int squadre = 0, int punti = 0) =>
        new(p.Id, p.Nome, p.Cognome, p.Ruolo, p.Partito, p.Costo, squadre, punti);

    // La squadra con i politici che ha adesso e i loro punti.
    private static SquadraDto Dto(Squadra s, PunteggiFantapolitica punteggi)
    {
        var attuali = s.Politici.Where(ps => ps.InSquadra).ToDictionary(ps => ps.PoliticoId);
        return new(s.Id, s.Nome,
            Ordina(attuali.Values.Select(ps => ps.Politico))
                .Select(p => new PoliticoAcquistatoDto(p.Id, p.Nome, p.Cognome, p.Ruolo, p.Partito, p.Costo,
                    attuali[p.Id].Prezzo, punteggi.DellAcquisto(attuali[p.Id]), attuali[p.Id].AcquistatoIl))
                .ToList(),
            attuali.Values.Sum(ps => ps.Prezzo), punteggi.DellaSquadra(s), s.AggiornatoIl);
    }

    // Impostazioni del modulo (la riga c'è sempre: la crea la migration).
    private async Task<ImpostazioniFantapolitica> Impostazioni() =>
        await _context.ImpostazioniFantapolitica.FindAsync(ImpostazioniFantapolitica.IdUnico)
        ?? new ImpostazioniFantapolitica();

    // ---------------------------------------------------------------------------
    // Utenti da scegliere come membri
    // ---------------------------------------------------------------------------

    [HttpGet("utenti")]
    public async Task<IEnumerable<PersonaDto>> GetUtenti()
    {
        var utenti = await UtentiAbilitati.ToListAsync();
        return utenti.Select(u => new PersonaDto(u.Id, NomeVisibile(u)))
            .OrderBy(p => p.Nome, Alfabetico);
    }

    // ---------------------------------------------------------------------------
    // Leghe
    // ---------------------------------------------------------------------------

    [HttpGet("leghe")]
    public async Task<IEnumerable<LegaDto>> GetLeghe()
    {
        var leghe = await _context.Leghe.AsNoTracking()
            .Include(l => l.CreatoDa)
            .Include(l => l.Membri).ThenInclude(m => m.Utente)
            .OrderBy(l => l.Nome)
            .ToListAsync();
        var squadre = await SquadreDi(leghe.Select(l => l.Id).ToList());
        var punteggi = await PunteggiFantapolitica.Carica(_context);
        return leghe.Select(l => Dto(l, squadre, punteggi));
    }

    [HttpGet("leghe/{id}")]
    public async Task<ActionResult<LegaDto>> GetLega(int id)
    {
        var lega = await _context.Leghe.AsNoTracking()
            .Include(l => l.CreatoDa)
            .Include(l => l.Membri).ThenInclude(m => m.Utente)
            .SingleOrDefaultAsync(l => l.Id == id);
        if (lega == null)
            return NotFound(new { messaggio = "Lega non trovata." });
        return Dto(lega, await SquadreDi([lega.Id]), await PunteggiFantapolitica.Carica(_context));
    }

    // Squadre delle leghe indicate, per (lega, utente), con tutti gli acquisti (anche venduti).
    private async Task<Dictionary<(int, int), Squadra>> SquadreDi(List<int> leghe) =>
        (await _context.Squadre.AsNoTracking()
            .Include(s => s.Politici).ThenInclude(ps => ps.Politico)
            .Where(s => leghe.Contains(s.LegaId))
            .ToListAsync())
        .ToDictionary(s => (s.LegaId, s.UtenteId));

    [HttpPost("leghe")]
    public async Task<ActionResult<LegaDto>> CreaLega(LegaRequest richiesta)
    {
        var (membri, errore) = await Controlla(richiesta, IdCorrente);
        if (errore != null) return errore;

        var lega = new Lega
        {
            Nome = richiesta.Nome.Trim(),
            CreatoDaId = IdCorrente,
            Membri = membri!.Select(id => new MembroLega { UtenteId = id }).ToList(),
        };
        _context.Leghe.Add(lega);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLega), new { id = lega.Id }, (await GetLega(lega.Id)).Value);
    }

    [HttpPut("leghe/{id}")]
    public async Task<IActionResult> ModificaLega(int id, LegaRequest richiesta)
    {
        var lega = await _context.Leghe.Include(l => l.Membri).SingleOrDefaultAsync(l => l.Id == id);
        if (lega == null)
            return NotFound(new { messaggio = "Lega non trovata." });
        if (!PuoGestire(lega))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { messaggio = "Solo chi ha creato la lega può modificarla." });

        // Chi l'ha creata resta sempre nella lega (anche quando la modifica il Super admin).
        // Chi è già nella lega ci resta anche se nel frattempo non è più abilitato al modulo.
        var (membri, errore) = await Controlla(richiesta, lega.CreatoDaId,
            lega.Membri.Select(m => m.UtenteId).ToHashSet());
        if (errore != null) return errore;

        lega.Nome = richiesta.Nome.Trim();
        lega.Membri.RemoveAll(m => !membri!.Contains(m.UtenteId));
        foreach (var utenteId in membri!.Where(u => lega.Membri.All(m => m.UtenteId != u)))
            lega.Membri.Add(new MembroLega { UtenteId = utenteId });

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("leghe/{id}")]
    public async Task<IActionResult> EliminaLega(int id)
    {
        var lega = await _context.Leghe.FindAsync(id);
        if (lega == null)
            return NotFound(new { messaggio = "Lega non trovata." });
        if (!PuoGestire(lega))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { messaggio = "Solo chi ha creato la lega può eliminarla." });

        _context.Leghe.Remove(lega);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Controlla il nome e i membri scelti; restituisce gli id dei membri con il creatore incluso.
    // giaMembri: chi è già nella lega può restarci anche se non è più tra gli abilitati.
    private async Task<(HashSet<int>?, ActionResult?)> Controlla(LegaRequest richiesta, int? creatoreId,
        HashSet<int>? giaMembri = null)
    {
        if (string.IsNullOrWhiteSpace(richiesta.Nome))
            return (null, BadRequest(new { messaggio = "Il nome della lega è obbligatorio." }));

        var scelti = (richiesta.Membri ?? []).ToHashSet();
        var nuovi = scelti.Where(id => giaMembri == null || !giaMembri.Contains(id)).ToList();
        if (nuovi.Count > 0)
        {
            var abilitati = await UtentiAbilitati.Where(u => nuovi.Contains(u.Id)).CountAsync();
            if (abilitati != nuovi.Count)
                return (null, BadRequest(new { messaggio = "Si possono scegliere solo utenti abilitati alla Fantapolitica." }));
        }

        if (creatoreId != null) scelti.Add(creatoreId.Value);
        return (scelti, null);
    }

    // Posizioni in classifica di un elenco già ordinato: chi ha la stessa chiave (es. stessi
    // punti e stessi crediti spesi) ha la stessa posizione (1, 2, 2, 4...).
    private static List<int> Posizioni<T, TChiave>(IList<T> ordinati, Func<T, TChiave> chiave)
    {
        var posizioni = new List<int>(ordinati.Count);
        for (var i = 0; i < ordinati.Count; i++)
        {
            posizioni.Add(i > 0 && EqualityComparer<TChiave>.Default.Equals(chiave(ordinati[i]), chiave(ordinati[i - 1]))
                ? posizioni[i - 1]
                : i + 1);
        }
        return posizioni;
    }

    // La lega con i membri in ordine di classifica: punti, poi (spareggio) meno crediti spesi.
    // Chi non ha la squadra va in fondo.
    private LegaDto Dto(Lega l, Dictionary<(int, int), Squadra> squadre, PunteggiFantapolitica punteggi)
    {
        var membri = l.Membri
            .Select(m =>
            {
                var squadra = squadre.TryGetValue((l.Id, m.UtenteId), out var s) ? Dto(s, punteggi) : null;
                return new MembroDto(m.UtenteId, NomeVisibile(m.Utente), squadra, squadra?.Punti ?? 0, 0);
            })
            .OrderBy(m => m.Squadra == null)
            .ThenByDescending(m => m.Punti)
            .ThenBy(m => m.Squadra?.Spesi ?? 0)
            .ThenBy(m => m.Nome, Alfabetico)
            .ToList();

        var posizioni = Posizioni(membri, m => (m.Squadra == null, m.Punti, m.Squadra?.Spesi ?? 0));
        for (var i = 0; i < membri.Count; i++)
            membri[i] = membri[i] with { Posizione = posizioni[i] };

        return new(l.Id, l.Nome,
            l.CreatoDa == null ? null : new PersonaDto(l.CreatoDa.Id, NomeVisibile(l.CreatoDa)),
            PuoGestire(l), l.CreatoIl, membri, GestoreSquadre);
    }

    // ---------------------------------------------------------------------------
    // Classifica generale (tutti gli utenti del modulo)
    // ---------------------------------------------------------------------------

    // Squadre di tutte le leghe (punti, poi meno crediti spesi) e politici (punti), con il
    // dettaglio degli eventi di ogni politico.
    [HttpGet("classifica")]
    public async Task<ClassificaDto> GetClassifica()
    {
        var punteggi = await PunteggiFantapolitica.Carica(_context);

        var squadre = (await _context.Squadre.AsNoTracking()
                .Include(s => s.Utente)
                .Include(s => s.Politici).ThenInclude(ps => ps.Politico)
                .ToListAsync())
            .Select(s => new { s.LegaId, s.Utente, Dto = Dto(s, punteggi) })
            .OrderByDescending(x => x.Dto.Punti)
            .ThenBy(x => x.Dto.Spesi)
            .ThenBy(x => x.Dto.Nome, Alfabetico)
            .ToList();
        var leghe = await _context.Leghe.AsNoTracking().ToDictionaryAsync(l => l.Id, l => l.Nome);
        var posizioniSquadre = Posizioni(squadre, x => (x.Dto.Punti, x.Dto.Spesi));

        var politici = await _context.Politici.AsNoTracking().ToListAsync();
        var inSquadra = await _context.PoliticiSquadre.AsNoTracking()
            .Where(ps => ps.CedutoIl == null)
            .GroupBy(ps => ps.PoliticoId)
            .Select(g => new { g.Key, Numero = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Numero);
        var eventi = (await _context.EventiPolitici.AsNoTracking()
                .Select(ep => new { ep.PoliticoId, ep.Evento.Data, ep.Evento.Id, Voce = ep.Evento.Voce.Nome, ep.Evento.Punti, ep.Evento.Descrizione })
                .ToListAsync())
            .GroupBy(e => e.PoliticoId)
            .ToDictionary(g => g.Key, g => g
                .OrderByDescending(e => e.Data).ThenByDescending(e => e.Id)
                .Select(e => new EventoDelPoliticoDto(e.Data, e.Voce, e.Punti, e.Descrizione))
                .ToList());
        var ordinati = Ordina(politici)
            .Select(p => Dto(p, inSquadra.GetValueOrDefault(p.Id), punteggi.DelPolitico(p.Id)))
            .OrderByDescending(p => p.Punti)   // stabile: a pari punti resta l'ordine per ruolo e nome
            .ToList();
        var posizioniPolitici = Posizioni(ordinati, p => p.Punti);

        return new ClassificaDto(
            squadre.Select((x, i) => new SquadraInClassificaDto(posizioniSquadre[i],
                new PersonaDto(x.Utente.Id, NomeVisibile(x.Utente)),
                new LegaBreveDto(x.LegaId, leghe.GetValueOrDefault(x.LegaId, "")), x.Dto)).ToList(),
            ordinati.Select((p, i) => new PoliticoInClassificaDto(posizioniPolitici[i], p,
                eventi.GetValueOrDefault(p.Id) ?? [])).ToList());
    }

    // ---------------------------------------------------------------------------
    // Politici: tutti li vedono, solo il Super admin li gestisce
    // ---------------------------------------------------------------------------

    [HttpGet("politici")]
    public async Task<IEnumerable<PoliticoDto>> GetPolitici()
    {
        var politici = await _context.Politici.AsNoTracking().ToListAsync();
        var squadre = await _context.PoliticiSquadre.AsNoTracking()
            .Where(ps => ps.CedutoIl == null)
            .GroupBy(ps => ps.PoliticoId)
            .Select(g => new { g.Key, Numero = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Numero);
        var punteggi = await PunteggiFantapolitica.Carica(_context);
        return Ordina(politici).Select(p => Dto(p, squadre.GetValueOrDefault(p.Id), punteggi.DelPolitico(p.Id)));
    }

    [HttpPost("politici")]
    [Authorize(Policy = Politiche.Admin)]
    public async Task<ActionResult<PoliticoDto>> CreaPolitico(PoliticoRequest richiesta)
    {
        var politico = new Politico();
        var errore = Applica(politico, richiesta);
        if (errore != null) return errore;

        _context.Politici.Add(politico);
        await _context.SaveChangesAsync();
        return Created($"/api/fantapolitica/politici/{politico.Id}", Dto(politico));
    }

    [HttpPut("politici/{id}")]
    [Authorize(Policy = Politiche.Admin)]
    public async Task<IActionResult> ModificaPolitico(int id, PoliticoRequest richiesta)
    {
        var politico = await _context.Politici.FindAsync(id);
        if (politico == null)
            return NotFound(new { messaggio = "Politico non trovato." });

        var errore = Applica(politico, richiesta);
        if (errore != null) return errore;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Il politico esce anche dalle squadre che lo avevano scelto e dagli eventi:
    // i punti che aveva portato non contano più.
    [HttpDelete("politici/{id}")]
    [Authorize(Policy = Politiche.Admin)]
    public async Task<IActionResult> EliminaPolitico(int id)
    {
        var politico = await _context.Politici.FindAsync(id);
        if (politico == null)
            return NotFound(new { messaggio = "Politico non trovato." });

        _context.Politici.Remove(politico);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private ActionResult? Applica(Politico politico, PoliticoRequest richiesta)
    {
        if (string.IsNullOrWhiteSpace(richiesta.Nome) || string.IsNullOrWhiteSpace(richiesta.Cognome))
            return BadRequest(new { messaggio = "Nome e cognome sono obbligatori." });
        if (!RuoliPolitico.Tutti.Contains(richiesta.Ruolo))
            return BadRequest(new { messaggio = "Ruolo non valido." });

        politico.Nome = richiesta.Nome.Trim();
        politico.Cognome = richiesta.Cognome.Trim();
        politico.Ruolo = richiesta.Ruolo;
        politico.Partito = string.IsNullOrWhiteSpace(richiesta.Partito) ? null : richiesta.Partito.Trim();
        politico.Costo = richiesta.Costo;
        return null;
    }

    // ---------------------------------------------------------------------------
    // Impostazioni: tutti le leggono, solo il Super admin le cambia
    // ---------------------------------------------------------------------------

    [HttpGet("impostazioni")]
    public async Task<ImpostazioniDto> GetImpostazioni() =>
        new((await Impostazioni()).CreditiPerSquadra, Squadra.PoliticiPerSquadra);

    // Le squadre già fatte restano valide anche se i crediti scendono; chi le modifica
    // dovrà rientrare nel nuovo limite.
    [HttpPut("impostazioni")]
    [Authorize(Policy = Politiche.Admin)]
    public async Task<IActionResult> ModificaImpostazioni(ImpostazioniRequest richiesta)
    {
        var impostazioni = await _context.ImpostazioniFantapolitica.FindAsync(ImpostazioniFantapolitica.IdUnico);
        if (impostazioni == null)
        {
            impostazioni = new ImpostazioniFantapolitica();
            _context.ImpostazioniFantapolitica.Add(impostazioni);
        }
        impostazioni.CreditiPerSquadra = richiesta.CreditiPerSquadra;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ---------------------------------------------------------------------------
    // Squadre: ognuno gestisce la propria, una per ogni lega di cui fa parte
    // ---------------------------------------------------------------------------

    // Le leghe di cui faccio parte, ciascuna con la mia squadra (se l'ho già creata).
    [HttpGet("squadre/mie")]
    public async Task<MieSquadreDto> GetMieSquadre()
    {
        var id = IdCorrente;
        var leghe = await _context.MembriLeghe.AsNoTracking()
            .Where(m => m.UtenteId == id)
            .Select(m => new { m.Lega.Id, m.Lega.Nome })
            .ToListAsync();
        var squadre = await _context.Squadre.AsNoTracking()
            .Include(s => s.Politici).ThenInclude(ps => ps.Politico)
            .Where(s => s.UtenteId == id)
            .ToDictionaryAsync(s => s.LegaId);
        var punteggi = await PunteggiFantapolitica.Carica(_context);

        return new MieSquadreDto(Squadra.PoliticiPerSquadra, (await Impostazioni()).CreditiPerSquadra, GestoreSquadre, leghe
            .OrderBy(l => l.Nome, Alfabetico)
            .Select(l => new MiaLegaDto(l.Id, l.Nome, squadre.TryGetValue(l.Id, out var s) ? Dto(s, punteggi) : null))
            .ToList());
    }

    // Crea la mia squadra nella lega (vale come conferma). Dopo, solo i gestori possono
    // cambiarla.
    [HttpPut("leghe/{legaId}/squadra")]
    public async Task<IActionResult> SalvaSquadra(int legaId, SquadraRequest richiesta)
    {
        var id = IdCorrente;
        if (!await _context.MembriLeghe.AnyAsync(m => m.LegaId == legaId && m.UtenteId == id))
            return NotFound(new { messaggio = "Non fai parte di questa lega." });
        if (!GestoreSquadre && await _context.Squadre.AnyAsync(s => s.LegaId == legaId && s.UtenteId == id))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { messaggio = "La tua squadra è già confermata: non si può più cambiare." });

        return await Salva(legaId, id, richiesta);
    }

    // Crea o modifica la squadra di un membro della lega (solo gestori).
    [HttpPut("leghe/{legaId}/squadre/{utenteId}")]
    [Authorize(Policy = Politiche.FantapoliticaAmministratore)]
    public async Task<IActionResult> SalvaSquadraDi(int legaId, int utenteId, SquadraRequest richiesta)
    {
        if (!await _context.MembriLeghe.AnyAsync(m => m.LegaId == legaId && m.UtenteId == utenteId))
            return NotFound(new { messaggio = "Questa persona non fa parte della lega." });

        return await Salva(legaId, utenteId, richiesta);
    }

    // Regole comuni: nome, numero di politici, crediti; acquisti e vendite nello storico.
    private async Task<IActionResult> Salva(int legaId, int id, SquadraRequest richiesta)
    {
        if (string.IsNullOrWhiteSpace(richiesta.Nome))
            return BadRequest(new { messaggio = "Il nome della squadra è obbligatorio." });

        var scelti = (richiesta.Politici ?? []).ToHashSet();
        if (scelti.Count != Squadra.PoliticiPerSquadra)
            return BadRequest(new { messaggio = $"La squadra deve avere {Squadra.PoliticiPerSquadra} politici." });
        var costi = await _context.Politici.Where(p => scelti.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Costo);
        if (costi.Count != scelti.Count)
            return BadRequest(new { messaggio = "Uno dei politici scelti non esiste più. Ricarica la pagina." });

        var squadra = await _context.Squadre.Include(s => s.Politici)
            .SingleOrDefaultAsync(s => s.LegaId == legaId && s.UtenteId == id);
        var attuali = squadra?.Politici.Where(ps => ps.InSquadra).ToList() ?? [];

        // Chi resta in squadra mantiene il prezzo pagato; i nuovi si pagano al costo di oggi.
        var prezzi = scelti.ToDictionary(p => p,
            p => attuali.FirstOrDefault(ps => ps.PoliticoId == p)?.Prezzo ?? costi[p]);
        var crediti = (await Impostazioni()).CreditiPerSquadra;
        var spesi = prezzi.Values.Sum();
        if (spesi > crediti)
            return BadRequest(new { messaggio = $"Hai superato i crediti: la squadra costa {spesi} crediti su {crediti}." });

        if (squadra == null)
        {
            squadra = new Squadra { LegaId = legaId, UtenteId = id };
            _context.Squadre.Add(squadra);
        }

        squadra.Nome = richiesta.Nome.Trim();
        squadra.AggiornatoIl = DateTime.UtcNow;

        // Venduti: restano nello storico con la data di vendita, ma i loro punti non contano
        // più. Se erano stati comprati oggi si tolgono e basta.
        var oggi = PunteggiFantapolitica.Oggi();
        foreach (var venduto in attuali.Where(ps => !scelti.Contains(ps.PoliticoId)))
        {
            if (venduto.AcquistatoIl >= oggi) squadra.Politici.Remove(venduto);
            else venduto.CedutoIl = oggi;
        }
        foreach (var politicoId in scelti.Where(p => attuali.All(ps => ps.PoliticoId != p)))
            squadra.Politici.Add(new PoliticoSquadra { PoliticoId = politicoId, Prezzo = prezzi[politicoId], AcquistatoIl = oggi });

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Eliminare la propria squadra: solo i gestori (il giocatore non può rifarla da capo).
    [HttpDelete("leghe/{legaId}/squadra")]
    public Task<IActionResult> EliminaSquadra(int legaId) =>
        GestoreSquadre
            ? Elimina(legaId, IdCorrente)
            : Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status403Forbidden,
                new { messaggio = "La tua squadra è confermata: non si può eliminare." }));

    [HttpDelete("leghe/{legaId}/squadre/{utenteId}")]
    [Authorize(Policy = Politiche.FantapoliticaAmministratore)]
    public Task<IActionResult> EliminaSquadraDi(int legaId, int utenteId) => Elimina(legaId, utenteId);

    private async Task<IActionResult> Elimina(int legaId, int id)
    {
        var squadra = await _context.Squadre.SingleOrDefaultAsync(s => s.LegaId == legaId && s.UtenteId == id);
        if (squadra == null)
            return NotFound(new { messaggio = "Squadra non trovata." });

        _context.Squadre.Remove(squadra);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
