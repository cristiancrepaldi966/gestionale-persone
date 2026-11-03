using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Forme = GestionalePersone.Services.FormeBaseIa3d.Forme;
using Posizioni = GestionalePersone.Services.FormeBaseIa3d.Posizioni;

namespace GestionalePersone.Services;

// Riconoscimento del modulo IA (pagina IA 3D): legge una descrizione in italiano, es.
// "piastra 100x60 spessore 4 con 4 fori da 5 agli angoli", e ne ricava forma base e misure.
// Prima versione a regole (niente servizi esterni): forme e sinonimi, misure con unità
// (mm, cm), dimensioni "AxBxC", fori (numero, diametro, viti M3/M4..., posizione) e angoli
// arrotondati. Le misure non scritte prendono un valore predefinito, segnalato.
// I valori ottenuti vanno poi controllati con FormeBaseIa3d.Valida.
//
// Dialogo: quando manca qualcosa di essenziale (la forma, una misura principale, numero o
// diametro dei fori) o restano parole non capite, invece di inventare un valore il risultato
// contiene delle domande. La pagina le pone una alla volta e rimanda la stessa frase con
// tutte le risposte date (codice della domanda → valore): il server non ricorda nulla.
public static class RiconoscitoreIa3d
{
    public const int LunghezzaMassima = 300;

    // Un pezzo della frase e cosa ne è stato capito.
    public record Parte(string Testo, string Significato);

    // Una misura del pezzo, già scritta per l'utente (es. "100 mm").
    public record Misura(string Nome, string Etichetta, string Valore, bool Predefinita);

    public static class TipiDomanda
    {
        public const string Scelta = "scelta";   // si risponde con uno dei pulsanti
        public const string Numero = "numero";   // si scrive un numero; le opzioni sono scorciatoie
    }

    public record Opzione(string Valore, string Etichetta);

    // Codice: "forma", "parole", "misura:<nome>" (anche "misura:lato"), "fori:numero",
    // "fori:diametro", "fori:posizione". Suggerito: valore proposto (segnaposto del campo).
    public record Domanda(string Codice, string Testo, string Tipo, Opzione[] Opzioni,
        string? Unita = null, decimal? Min = null, decimal? Max = null, decimal? Suggerito = null, string? Aiuto = null);

    // Forma null: serve ancora sapere che forma ha il pezzo (c'è la domanda "forma").
    // Con domande da fare, le misure non ancora note hanno un valore provvisorio.
    public record Risultato(
        string? Forma,
        Dictionary<string, object> Parametri,
        List<Parte> Capito,
        List<string> NonCapito,
        List<string> Avvisi,
        List<Misura> Misure,
        List<Domanda> Domande);

    public static Risultato Riconosci(string testo, IReadOnlyDictionary<string, string>? risposte = null) =>
        new Lettura(testo, risposte ?? new Dictionary<string, string>()).Esegui();

    // Misure da mostrare per un pezzo (solo quelle che contano per la sua forma).
    public static List<Misura> MisureDi(IReadOnlyDictionary<string, object> parametri, IReadOnlySet<string>? predefinite = null)
    {
        var forma = (string)parametri["forma"];
        decimal N(string nome) => Convert.ToDecimal(parametri[nome], CultureInfo.InvariantCulture);
        bool P(string nome) => predefinite?.Contains(nome) == true;

        var misure = new List<Misura>();
        foreach (var (nome, etichetta) in FormeBaseIa3d.MisureDi(forma))
        {
            if (nome == "raggio_angoli" && N(nome) == 0) continue;
            misure.Add(new Misura(nome, etichetta, FormeBaseIa3d.Mm(N(nome)), P(nome)));
        }

        if (forma == Forme.Tubo && N("diametro_interno") > 0)
            misure.Add(new Misura("parete", "Parete", FormeBaseIa3d.Mm((N("diametro") - N("diametro_interno")) / 2), false));

        if (FormeBaseIa3d.ConFori(forma) && N("fori") > 0)
        {
            var posizione = FormeBaseIa3d.NomePosizione((string)parametri["posizione_fori"]);
            misure.Add(new Misura("fori", "Fori", ((int)N("fori")).ToString(CultureInfo.InvariantCulture), P("fori")));
            misure.Add(new Misura("diametro_fori", "Diametro dei fori", "Ø " + FormeBaseIa3d.Mm(N("diametro_fori")), P("diametro_fori")));
            misure.Add(new Misura("posizione_fori", "Posizione dei fori", char.ToUpperInvariant(posizione[0]) + posizione[1..],
                P("posizione_fori")));
        }
        return misure;
    }

    // ---------------------------------------------------------------------------
    // Parole riconosciute (minuscole, senza accenti)
    // ---------------------------------------------------------------------------

    private enum Voce { Lunghezza, Larghezza, Profondita, Altezza, Spessore, Diametro, Raggio, Interno, Esterno, Parete, Base, Cima, Lato }

    private static readonly Dictionary<string, string> ParoleForma = new()
    {
        ["cubo"] = Forme.Blocco, ["cubetto"] = Forme.Blocco, ["blocco"] = Forme.Blocco,
        ["blocchetto"] = Forme.Blocco, ["parallelepipedo"] = Forme.Blocco, ["mattoncino"] = Forme.Blocco,
        ["mattone"] = Forme.Blocco, ["barra"] = Forme.Blocco, ["barretta"] = Forme.Blocco, ["listello"] = Forme.Blocco,

        ["piastra"] = Forme.Piastra, ["piastrina"] = Forme.Piastra, ["lastra"] = Forme.Piastra,
        ["placca"] = Forme.Piastra, ["placchetta"] = Forme.Piastra, ["tavoletta"] = Forme.Piastra,
        ["pannello"] = Forme.Piastra, ["rettangolo"] = Forme.Piastra, ["quadrato"] = Forme.Piastra,

        ["cilindro"] = Forme.Cilindro, ["cilindretto"] = Forme.Cilindro, ["disco"] = Forme.Cilindro,
        ["dischetto"] = Forme.Cilindro, ["tondo"] = Forme.Cilindro, ["tondino"] = Forme.Cilindro,
        ["perno"] = Forme.Cilindro, ["asta"] = Forme.Cilindro, ["bastoncino"] = Forme.Cilindro, ["piolo"] = Forme.Cilindro,

        ["tubo"] = Forme.Tubo, ["tubetto"] = Forme.Tubo, ["anello"] = Forme.Tubo, ["distanziale"] = Forme.Tubo,
        ["boccola"] = Forme.Tubo, ["rondella"] = Forme.Tubo, ["manicotto"] = Forme.Tubo,

        ["sfera"] = Forme.Sfera, ["palla"] = Forme.Sfera, ["pallina"] = Forme.Sfera, ["biglia"] = Forme.Sfera,

        ["cono"] = Forme.Cono, ["conetto"] = Forme.Cono, ["tronco di cono"] = Forme.Cono,
        ["troncocono"] = Forme.Cono, ["tronco-cono"] = Forme.Cono,
    };

    // Forme che hanno tutti i lati uguali se non si dice altro.
    private static readonly HashSet<string> ParoleCubo = new() { "cubo", "cubetto" };

    // Parole che introducono una misura ("alto 50", "diametro interno 16"...).
    private static readonly Dictionary<string, Voce> ParoleMisura = new()
    {
        ["diametro interno"] = Voce.Interno, ["diametro esterno"] = Voce.Esterno,
        ["diametro di base"] = Voce.Base, ["diametro alla base"] = Voce.Base, ["diametro base"] = Voce.Base,
        ["diametro inferiore"] = Voce.Base, ["diametro superiore"] = Voce.Cima,
        ["diametro in cima"] = Voce.Cima, ["diametro sopra"] = Voce.Cima,
        ["foro interno"] = Voce.Interno, ["buco interno"] = Voce.Interno,
        ["spessore della parete"] = Voce.Parete, ["spessore delle pareti"] = Voce.Parete,
        ["spessore parete"] = Voce.Parete, ["spessore pareti"] = Voce.Parete,
        ["parete"] = Voce.Parete, ["pareti"] = Voce.Parete,
        ["lunghezza"] = Voce.Lunghezza, ["lungo"] = Voce.Lunghezza, ["lunga"] = Voce.Lunghezza,
        ["larghezza"] = Voce.Larghezza, ["largo"] = Voce.Larghezza, ["larga"] = Voce.Larghezza,
        ["profondita"] = Voce.Profondita, ["profondo"] = Voce.Profondita, ["profonda"] = Voce.Profondita,
        ["altezza"] = Voce.Altezza, ["alto"] = Voce.Altezza, ["alta"] = Voce.Altezza, ["h"] = Voce.Altezza,
        ["spessore"] = Voce.Spessore, ["spesso"] = Voce.Spessore, ["spessa"] = Voce.Spessore,
        ["diametro"] = Voce.Diametro, ["diam"] = Voce.Diametro, ["ø"] = Voce.Diametro,
        ["raggio"] = Voce.Raggio, ["lato"] = Voce.Lato,
        ["interno"] = Voce.Interno, ["interna"] = Voce.Interno,
        ["esterno"] = Voce.Esterno, ["esterna"] = Voce.Esterno,
        ["base"] = Voce.Base, ["cima"] = Voce.Cima, ["in cima"] = Voce.Cima,
        ["sopra"] = Voce.Cima, ["punta"] = Voce.Cima,
    };

    // Nomi che possono anche seguire la misura ("50 di altezza").
    private static readonly string[] NomiMisura =
    [
        "diametro interno", "diametro esterno", "diametro di base", "diametro alla base", "diametro in cima",
        "spessore della parete", "spessore delle pareti", "spessore parete", "parete",
        "lunghezza", "larghezza", "profondita", "altezza", "spessore", "diametro", "raggio", "lato",
    ];

    // Parole che non portano informazioni: non finiscono tra quelle "non capite".
    private static readonly HashSet<string> ParoleVuote = new()
    {
        "un", "uno", "una", "il", "lo", "la", "i", "gli", "le", "l", "di", "da", "del", "dello", "della",
        "dei", "degli", "delle", "a", "al", "allo", "alla", "ai", "agli", "alle", "in", "nel", "nella",
        "nei", "con", "su", "sul", "sulla", "per", "tra", "fra", "e", "ed", "o", "che", "mi", "ci",
        "vorrei", "voglio", "fammi", "fai", "fare", "crea", "creami", "creare", "disegna", "disegnami",
        "stampa", "stampami", "serve", "servirebbe", "pezzo", "oggetto", "forma", "misura", "misure",
        "dimensioni", "circa", "tipo", "come", "mm", "millimetri", "cm", "centimetri", "x", "pieno",
        "piena", "solido", "solida", "grazie", "favore", "ogni", "tutti",
    };

    // Diametro del foro passante per una vite metrica (M3 → 3,4 mm).
    private static readonly Dictionary<decimal, decimal> ForiPerViti = new()
    {
        [2] = 2.4m, [2.5m] = 2.9m, [3] = 3.4m, [4] = 4.5m, [5] = 5.5m, [6] = 6.6m, [8] = 9, [10] = 11, [12] = 13.5m,
    };

    private static readonly Dictionary<string, int> NumeriInLettere = new()
    {
        ["un"] = 1, ["uno"] = 1, ["una"] = 1, ["due"] = 2, ["tre"] = 3, ["quattro"] = 4, ["cinque"] = 5,
        ["sei"] = 6, ["sette"] = 7, ["otto"] = 8, ["nove"] = 9, ["dieci"] = 10, ["dodici"] = 12,
    };

    // ---------------------------------------------------------------------------
    // Modelli di testo (sul testo normalizzato)
    // ---------------------------------------------------------------------------

    private const string Inizio = @"(?<![\p{L}\p{N}.])";
    private const string Fine = @"(?![\p{L}\p{N}])";
    private const string Unita = "(?:millimetri|millimetro|centimetri|centimetro|mm|cm)";

    // Dove vanno i fori.
    private const string Posizione =
        @"(?:(?<centro>al\s+centro|in\s+mezzo|nel\s+mezzo|centrale|centrali|centrat[oi])" +
        @"|(?<angoli>(?:(?:a|ne|su)(?:gli|i)\s+|in\s+)?(?:(?:4|quattro)\s+)?(?:angoli|spigoli|vertici)" +
        @"(?!\s+(?:arrotondat|smussat|stondat|raggiat|raccordat)))" +
        @"|(?<fila>in\s+(?:fila|riga|linea)|allineat[io])" +
        @"|(?<cerchio>in\s+(?:cerchio|tondo|corona|circolo)|a\s+(?:cerchio|corona|raggiera)|attorno|intorno|lungo\s+il\s+bordo|sul\s+bordo))";

    // Numero con unità facoltativa: gruppi "g" (valore) e "ug" (unità).
    private static string Num(string g) =>
        "(?<" + g + @">\d+(?:\.\d+)?)(?:\s*(?<u" + g + ">" + Unita + @")(?![\p{L}]))?";

    private static Regex Crea(string modello) =>
        new(modello, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));

    // Alternative di parole, le più lunghe per prime ("diametro interno" prima di "diametro").
    private static string Alternative(IEnumerable<string> parole) =>
        string.Join("|", parole
            .OrderByDescending(p => p.Length)
            .Select(p => string.Join(@"\s+", p.Split(' ').Select(Regex.Escape))));

    private static readonly Regex ModelloForma = Crea(Inizio + "(?<w>" + Alternative(ParoleForma.Keys) + ")" + Fine);

    private static readonly Regex ModelloQuadrato = Crea(Inizio + "quadrat[aoie]" + Fine);

    private static readonly Regex ModelloPunta = Crea(Inizio + @"(?:a\s+punta|appuntit[oa]|a\s+spillo)" + Fine);

    // "4 fori da 5 agli angoli", "un foro centrale da 8", "2 fori per viti m4 in fila"
    private static readonly Regex ModelloFori = Crea(Inizio +
        @"(?:(?<quanti>\d+|un|uno|una|due|tre|quattro|cinque|sei|sette|otto|nove|dieci|dodici)\s+)?" +
        @"(?<parola>fori|foro|buchi|buco)(?:\s+passant[ei])?(?!\s+intern)" +
        @"(?:\s+" + Posizione + ")?" +
        @"(?:\s*(?:(?:da|di|del|per)\s+)?(?:(?:diametro|diam|ø)\s*(?:(?:di|da)\s+)?)?" +
        @"(?:(?:viti?\s+)?m(?<vite>\d+(?:\.\d+)?)|" + Num("d") + @")(?:\s+(?:di\s+)?diametro)?)?" +
        @"(?:\s+" + Posizione + ")?" + Fine);

    // "angoli arrotondati", "con gli spigoli smussati di 3", "raggio angoli 5", "arrotondata"
    private static readonly Regex ModelloAngoliArrotondati = Crea(Inizio +
        @"(?:(?:con\s+)?(?:gli\s+|i\s+)?(?:angoli|spigoli|bordi)\s+(?:arrotondat|smussat|stondat|raggiat|raccordat)[aeio]" +
        @"|(?:raggio|r)\s+(?:(?:de|a)gli\s+)?(?:angoli|spigoli)" +
        @"|(?:arrotondat|stondat|smussat|raccordat)[aeio])" +
        @"(?:\s*(?:(?:di|da|a|con|raggio|r)\s+)*" + Num("r") + ")?" + Fine);

    private static readonly Regex ModelloPosizione = Crea(Inizio + Posizione + Fine);

    // "100x60x3", "10 x 20 cm", "80 per 40 per 3"
    private const string Per = @"(?:\s*x\s*|\s+per\s+)";
    private static readonly Regex ModelloDimensioni = Crea(Inizio + Num("a") + Per + Num("b") +
        "(?:" + Per + Num("c") + ")?" + Fine);

    // "alto 50", "diametro: 30", "spessore della parete 2 mm"
    private static readonly Regex ModelloMisura = Crea(Inizio + "(?<k>" + Alternative(ParoleMisura.Keys) + ")" +
        @"\s*(?:(?:di|da|pari\s+a|uguale\s+a)\s+|[:=]\s*)?" + Num("n") + Fine);

    // "50 di altezza", "4 mm di spessore"
    private static readonly Regex ModelloMisuraDopo = Crea(Inizio + Num("n") + @"\s*(?:di\s+|d['’]\s*)?(?<k>" +
        Alternative(NomiMisura) + ")" + Fine);

    // "base 100x60": la piastra, se non è scritta un'altra forma.
    private static readonly Regex ModelloBase = Crea(Inizio + "base" + Fine);

    private static readonly Regex ModelloNumero = Crea(Inizio + Num("n") + Fine);

    private static readonly Regex ModelloParola = Crea(@"[\p{L}\p{N}]+(?:\.\d+)?");

    private static readonly Regex InFondo = Crea(@"(?:^|[^\p{L}])in\s+$");

    // ---------------------------------------------------------------------------
    // Testo normalizzato
    // ---------------------------------------------------------------------------

    // Minuscole, senza accenti, "×" → "x", "4,5" → "4.5". Stessa lunghezza dell'originale:
    // le posizioni trovate valgono anche per il testo scritto dall'utente.
    private static string Normalizza(string testo)
    {
        var sb = new StringBuilder(testo.Length);
        for (var i = 0; i < testo.Length; i++)
        {
            var c = char.ToLowerInvariant(testo[i]);
            if (char.IsSurrogate(c)) c = ' ';
            else if (c is '×' or '*') c = 'x';
            else if (c is '⌀' or 'ø') c = 'ø';
            else if (c == ',' && i > 0 && i < testo.Length - 1 && char.IsDigit(testo[i - 1]) && char.IsDigit(testo[i + 1])) c = '.';
            else if (c > '\u007f')
            {
                var scomposto = c.ToString().Normalize(NormalizationForm.FormD);
                if (scomposto.Length > 0 && scomposto[0] <= '\u007f') c = scomposto[0];
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static string Spazi(string testo) => Regex.Replace(testo, @"\s+", " ");

    // Valore di un numero trovato, in mm; l'unità del gruppo vale se il numero non ha la sua.
    private static decimal Valore(Match m, string gruppo, string? unitaGruppo = null)
    {
        if (!decimal.TryParse(m.Groups[gruppo].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var n) ||
            n > 100000)
            n = 100000;
        var u = m.Groups["u" + gruppo];
        var unita = u.Success ? u.Value : unitaGruppo;
        if (unita != null && unita.StartsWith('c')) n *= 10;
        return Math.Round(n, 2);
    }

    private static string? Dove(Match m) =>
        m.Groups["centro"].Success ? Posizioni.Centro
        : m.Groups["angoli"].Success ? Posizioni.Angoli
        : m.Groups["fila"].Success ? Posizioni.Fila
        : m.Groups["cerchio"].Success ? Posizioni.Cerchio
        : null;

    private static int Quanti(string testo) =>
        int.TryParse(testo, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n
        : NumeriInLettere.TryGetValue(testo, out var lettere) ? lettere
        : 1000;

    private static decimal DiametroPerVite(decimal vite) =>
        ForiPerViti.TryGetValue(vite, out var d) ? d : Math.Round(vite * 1.1m, 1);

    private static (string, decimal)[] P(params (string, decimal)[] valori) => valori;

    // "Agli angoli" → "agli angoli"; "Ø 4 mm" resta com'è.
    private static string Minuscola(string testo) =>
        testo.Length > 0 && char.IsUpper(testo[0]) && testo[0] != 'Ø' ? char.ToLowerInvariant(testo[0]) + testo[1..] : testo;

    // ---------------------------------------------------------------------------
    // Pezzi di frase trovati
    // ---------------------------------------------------------------------------

    private record Trovato(int Posizione, string Testo);

    private record Indicazione(int Posizione, string Testo, Voce Voce, decimal Valore) : Trovato(Posizione, Testo);

    private record Dimensioni(int Posizione, string Testo, decimal[] Valori) : Trovato(Posizione, Testo);

    private record NumeroLibero(int Posizione, string Testo, decimal Valore, int Lunghezza) : Trovato(Posizione, Testo);

    private record Fori(int Posizione, string Testo, int? Quanti, decimal? Diametro, string? Dove) : Trovato(Posizione, Testo);

    // Lettura di una frase: i modelli si applicano in ordine; ogni parte riconosciuta viene
    // "consumata" (sostituita da un carattere neutro), così non la legge anche il modello dopo.
    private sealed class Lettura
    {
        private const char Usato = '\u0001';

        private readonly string _originale;
        private readonly char[] _testo;

        private readonly List<(int Posizione, Parte Parte)> _capito = new();
        private readonly List<string> _nonCapito = new();
        private readonly List<string> _avvisi = new();

        private readonly List<Trovato> _elementi = new();      // dimensioni e misure, in ordine di posizione
        private readonly List<NumeroLibero> _numeri = new();   // numeri senza parola accanto

        private string? _forma;
        private string? _parolaForma;
        private bool _quadrato;
        private Fori? _fori;
        private Trovato? _arrotondati;
        private decimal? _raggioAngoli;
        private decimal? _parete;
        private string? _posizioneFori;

        private readonly Dictionary<string, decimal> _valori = new();
        private readonly HashSet<string> _predefinite = new();

        private readonly IReadOnlyDictionary<string, string> _risposte;
        private readonly List<Domanda> _domande = new();

        public Lettura(string testo, IReadOnlyDictionary<string, string> risposte)
        {
            _originale = testo;
            _testo = Normalizza(testo).ToCharArray();
            _risposte = risposte;
        }

        private string? RispostaTesto(string codice) => _risposte.GetValueOrDefault(codice);

        // Risposta numerica (in mm per le misure); null se manca o non è un numero valido.
        private decimal? RispostaNumero(string codice) =>
            decimal.TryParse(RispostaTesto(codice), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var n) &&
            n <= 100000 ? Math.Round(n, 2) : null;

        // Rondella, distanziale e anello: lo "spessore" è l'altezza, non la parete.
        private bool SpessoreEAltezza => _parolaForma is "rondella" or "distanziale" or "anello";

        public Risultato Esegui()
        {
            CercaForma();
            Cerca(ModelloPunta, m => { _elementi.Add(new Indicazione(m.Index, Pezzo(m), Voce.Cima, 0)); return true; });
            CercaFori();
            CercaAngoliArrotondati();
            CercaPosizioneFori();
            CercaDimensioni();
            CercaMisure();
            CercaBase();
            Cerca(ModelloNumero, m => { _numeri.Add(new NumeroLibero(m.Index, Pezzo(m), Valore(m, "n"), m.Length)); return false; });

            if (_forma == null && !ScegliForma())
                return new Risultato(null, new(), Capito(), NonCapito(), _avvisi, new(), _domande);

            Assegna();
            Completa();

            // Parole non capite: si chiede prima di tutto se ignorarle (o riscrivere la frase).
            var nonCapito = NonCapito();
            if (nonCapito.Count > 0 && RispostaTesto("parole") != "ignora")
                _domande.Insert(0, new Domanda("parole",
                    (nonCapito.Count == 1 ? "Non ho capito " : "Non ho capito queste parti: ") +
                    string.Join(", ", nonCapito.Select(p => $"«{p}»")) + ". Cosa faccio?",
                    TipiDomanda.Scelta,
                    [new("ignora", nonCapito.Count == 1 ? "Ignorala" : "Ignorale"), new("riscrivi", "Riscrivo la descrizione")]));

            var parametri = Parametri();
            var misure = MisureDi(parametri, _predefinite);
            var predefinite = misure.Where(m => m.Predefinita)
                .Select(m => $"{m.Etichetta.ToLowerInvariant()} {Minuscola(m.Valore)}").ToList();
            if (predefinite.Count > 0)
                _avvisi.Add("Non indicato nella descrizione, uso un valore predefinito: " + string.Join(", ", predefinite) + ".");

            return new Risultato(_forma, parametri, Capito(), nonCapito, _avvisi, misure, _domande);
        }

        // Nessuna forma scritta: vale la risposta alla domanda "forma", altrimenti la si chiede
        // (proponendo per prima quella che si deduce dalle misure). False se manca la risposta.
        private bool ScegliForma()
        {
            var sconosciute = ModelloParola.Matches(new string(_testo))
                .Where(m => m.Value.Any(char.IsLetter) && !ParoleVuote.Contains(m.Value)).ToList();
            var scelta = RispostaTesto("forma");

            if (scelta != null && FormeBaseIa3d.NomiForme.ContainsKey(scelta))
            {
                _forma = scelta;
                // Una sola parola sconosciuta (es. «scatolina»): era il nome della forma.
                if (sconosciute.Count == 1)
                {
                    var m = sconosciute[0];
                    AggiungiCapito(m, FormeBaseIa3d.NomeForma(scelta).ToLowerInvariant() + " (tua risposta)");
                    Segna(m.Index, m.Length);
                }
                return true;
            }

            var dedotta = DeduciForma();
            var opzioni = FormeBaseIa3d.NomiForme
                .OrderBy(f => f.Key == dedotta ? 0 : 1)
                .Select(f => new Opzione(f.Key, f.Key == dedotta ? f.Value + " (dalle misure)" : f.Value))
                .ToArray();
            _domande.Add(new Domanda("forma",
                sconosciute.Count == 1 ? $"Non conosco «{Pezzo(sconosciute[0])}»: che forma è?" : "Che forma deve avere il pezzo?",
                TipiDomanda.Scelta, opzioni));
            return false;
        }

        // -----------------------------------------------------------------------
        // Ricerca nel testo
        // -----------------------------------------------------------------------

        // Ogni occorrenza del modello nel testo ancora libero; se "usa" la accetta, diventa usata.
        private void Cerca(Regex modello, Func<Match, bool> usa)
        {
            foreach (Match m in modello.Matches(new string(_testo)))
                if (m.Length > 0 && usa(m))
                    Segna(m.Index, m.Length);
        }

        private void Segna(int inizio, int lunghezza)
        {
            for (var i = inizio; i < inizio + lunghezza; i++)
                _testo[i] = Usato;
        }

        private string Pezzo(Match m) => _originale.Substring(m.Index, m.Length).Trim();

        private void AggiungiCapito(Match m, string significato) => _capito.Add((m.Index, new Parte(Pezzo(m), significato)));

        private void CercaForma()
        {
            string? testoForma = null;
            var altre = new List<string>();
            Cerca(ModelloForma, m =>
            {
                var parola = Spazi(m.Groups["w"].Value);
                if (parola == "tondo" && InFondo.IsMatch(new string(_testo, 0, m.Index)))
                    return false;   // "fori in tondo": è la posizione dei fori
                if (_forma == null)
                {
                    _forma = ParoleForma[parola];
                    _parolaForma = parola;
                    testoForma = Pezzo(m);
                    AggiungiCapito(m, FormeBaseIa3d.NomeForma(_forma).ToLowerInvariant() +
                        (ParoleCubo.Contains(parola) ? " con i lati uguali" : ""));
                }
                else altre.Add(Pezzo(m));
                return true;
            });
            if (altre.Count > 0)
                _avvisi.Add($"Per ora creo una forma alla volta: ho tenuto «{testoForma}» e tralasciato «{string.Join("», «", altre)}».");

            _quadrato = _parolaForma == "quadrato";
            Cerca(ModelloQuadrato, m => { _quadrato = true; AggiungiCapito(m, "lati uguali"); return true; });
        }

        private void CercaFori()
        {
            var altri = new List<string>();
            Cerca(ModelloFori, m =>
            {
                if (_fori != null)
                {
                    altri.Add(Pezzo(m));
                    return true;
                }
                int? quanti = null;
                if (m.Groups["quanti"].Success) quanti = Quanti(m.Groups["quanti"].Value);
                else if (m.Groups["parola"].Value is "foro" or "buco") quanti = 1;

                decimal? diametro = null;
                if (m.Groups["vite"].Success) diametro = DiametroPerVite(Valore(m, "vite"));
                else if (m.Groups["d"].Success) diametro = Valore(m, "d");

                _fori = new Fori(m.Index, Pezzo(m), quanti, diametro, Dove(m));
                return true;
            });
            if (altri.Count > 0)
                _avvisi.Add($"Per ora un solo gruppo di fori: ho tenuto «{_fori!.Testo}» e tralasciato «{string.Join("», «", altri)}».");
        }

        private void CercaAngoliArrotondati()
        {
            Cerca(ModelloAngoliArrotondati, m =>
            {
                if (_arrotondati == null)
                {
                    _arrotondati = new Trovato(m.Index, Pezzo(m));
                    if (m.Groups["r"].Success) _raggioAngoli = Valore(m, "r");
                }
                return true;
            });
        }

        // La posizione dei fori scritta lontano dai fori ("2 fori da 4 ... in fila").
        private void CercaPosizioneFori()
        {
            Cerca(ModelloPosizione, m =>
            {
                if (_fori == null || _fori.Dove != null) return false;
                _fori = _fori with { Dove = Dove(m), Testo = _fori.Testo + " … " + Pezzo(m) };
                return true;
            });
        }

        private void CercaDimensioni()
        {
            Cerca(ModelloDimensioni, m =>
            {
                var gruppi = m.Groups["c"].Success ? new[] { "a", "b", "c" } : new[] { "a", "b" };
                string? unita = null;   // "10x20x5 cm": vale per tutte l'ultima unità scritta
                foreach (var g in gruppi)
                    if (m.Groups["u" + g].Success) unita = m.Groups["u" + g].Value;
                _elementi.Add(new Dimensioni(m.Index, Pezzo(m), gruppi.Select(g => Valore(m, g, unita)).ToArray()));
                return true;
            });
        }

        private void CercaMisure()
        {
            bool Aggiungi(Match m)
            {
                var voce = ParoleMisura[Spazi(m.Groups["k"].Value)];
                _elementi.Add(new Indicazione(m.Index, Pezzo(m), voce, Valore(m, "n")));
                return true;
            }
            Cerca(ModelloMisura, Aggiungi);
            Cerca(ModelloMisuraDopo, Aggiungi);
        }

        private void CercaBase()
        {
            if (_forma != null) return;
            Cerca(ModelloBase, m =>
            {
                if (_forma != null) return false;
                _forma = Forme.Piastra;
                _parolaForma = "base";
                AggiungiCapito(m, "piastra");
                return true;
            });
        }

        // Forma che si deduce dalle misure (proposta per prima nella domanda "forma").
        private string? DeduciForma()
        {
            var voci = _elementi.OfType<Indicazione>().Select(i => i.Voce).ToHashSet();
            var dimensioni = _elementi.OfType<Dimensioni>().ToList();
            bool Una(params Voce[] scelte) => scelte.Any(voci.Contains);

            return dimensioni.Any(d => d.Valori.Length == 3) ? Forme.Blocco
                : Una(Voce.Interno, Voce.Esterno, Voce.Parete) ? Forme.Tubo
                : Una(Voce.Cima, Voce.Base) ? Forme.Cono
                : Una(Voce.Diametro, Voce.Raggio) ? Forme.Cilindro
                : dimensioni.Count > 0 && Una(Voce.Spessore) ? Forme.Piastra
                : dimensioni.Count > 0 || Una(Voce.Lunghezza, Voce.Larghezza, Voce.Profondita, Voce.Altezza, Voce.Spessore, Voce.Lato)
                    ? Forme.Blocco
                : null;
        }

        // -----------------------------------------------------------------------
        // Dalle parti trovate alle misure del pezzo
        // -----------------------------------------------------------------------

        private string Etichetta(string nome) => FormeBaseIa3d.Etichetta(_forma!, nome).ToLowerInvariant();

        private static string Scrivi(string nome, decimal valore) =>
            nome == "fori" ? ((int)valore).ToString(CultureInfo.InvariantCulture) : FormeBaseIa3d.Mm(valore);

        // Assegna le misure di un pezzo di frase; una misura già indicata non viene cambiata.
        private void Imposta(Trovato t, params (string Nome, decimal Valore)[] valori)
        {
            var messi = new List<string>();
            foreach (var (nome, valore) in valori)
            {
                if (_valori.TryGetValue(nome, out var gia))
                    _avvisi.Add($"«{t.Testo}» non usato per {Etichetta(nome)}: avevo già {Scrivi(nome, gia)}.");
                else
                {
                    _valori[nome] = valore;
                    messi.Add($"{Etichetta(nome)} {Scrivi(nome, valore)}");
                }
            }
            if (messi.Count > 0)
                _capito.Add((t.Posizione, new Parte(t.Testo, string.Join(", ", messi))));
        }

        private void Predefinito(string nome, decimal valore)
        {
            if (_valori.TryAdd(nome, valore)) _predefinite.Add(nome);
        }

        private void Assegna()
        {
            var lunghezzaIndicata = _elementi.OfType<Indicazione>().Any(i => i.Voce == Voce.Lunghezza);
            foreach (var e in _elementi.OrderBy(e => e.Posizione))
            {
                if (e is Dimensioni d) AssegnaDimensioni(d);
                else if (e is Indicazione i) AssegnaIndicazione(i, lunghezzaIndicata);
            }
            AssegnaNumeri();
        }

        private void AssegnaDimensioni(Dimensioni d)
        {
            var v = d.Valori;
            switch (_forma)
            {
                case Forme.Blocco or Forme.Piastra:
                    if (v.Length == 3) Imposta(d, ("larghezza", v[0]), ("profondita", v[1]), ("altezza", v[2]));
                    else Imposta(d, ("larghezza", v[0]), ("profondita", v[1]));
                    break;
                case Forme.Cilindro:
                    Imposta(d, ("diametro", v[0]), ("altezza", v[1]));
                    if (v.Length == 3) _avvisi.Add($"«{d.Testo}»: per il cilindro bastano diametro e altezza, ho usato le prime due misure.");
                    break;
                case Forme.Sfera:
                    Imposta(d, ("diametro", v[0]));
                    _avvisi.Add($"«{d.Testo}»: per la sfera basta il diametro, ho usato la prima misura.");
                    break;
                case Forme.Tubo:
                    if (v.Length == 3) Imposta(d, ("diametro", v[0]), ("diametro_interno", v[1]), ("altezza", v[2]));
                    else if (v[1] < v[0]) Imposta(d, ("diametro", v[0]), ("diametro_interno", v[1]));   // "20x16": esterno × interno
                    else Imposta(d, ("diametro", v[0]), ("altezza", v[1]));
                    break;
                case Forme.Cono:
                    if (v.Length == 3) Imposta(d, ("diametro", v[0]), ("diametro_cima", v[1]), ("altezza", v[2]));
                    else Imposta(d, ("diametro", v[0]), ("altezza", v[1]));
                    break;
            }
        }

        private void AssegnaIndicazione(Indicazione i, bool lunghezzaIndicata)
        {
            var v = i.Valore;

            // Tubo: la parete diventa il diametro interno quando si conosce l'esterno (Completa).
            if (_forma == Forme.Tubo && (i.Voce == Voce.Parete || (i.Voce == Voce.Spessore && !SpessoreEAltezza)))
            {
                if (_parete is decimal gia)
                    _avvisi.Add($"«{i.Testo}» non usato: avevo già la parete di {FormeBaseIa3d.Mm(gia)}.");
                else
                {
                    _parete = v;
                    _capito.Add((i.Posizione, new Parte(i.Testo, "parete " + FormeBaseIa3d.Mm(v))));
                }
                return;
            }

            (string, decimal)[]? valori;
            if (_forma is Forme.Blocco or Forme.Piastra)
            {
                valori = i.Voce switch
                {
                    Voce.Lunghezza => P(("larghezza", v)),
                    // "lungo 100 largo 40": la lunghezza va sulla larghezza, la larghezza sulla profondità.
                    Voce.Larghezza => P((lunghezzaIndicata ? "profondita" : "larghezza", v)),
                    Voce.Profondita => P(("profondita", v)),
                    Voce.Altezza or Voce.Spessore => P(("altezza", v)),
                    Voce.Lato when _forma == Forme.Blocco => P(("larghezza", v), ("profondita", v), ("altezza", v)),
                    Voce.Lato => P(("larghezza", v), ("profondita", v)),
                    Voce.Raggio => P(("raggio_angoli", v)),
                    _ => null,
                };
            }
            else
            {
                valori = (_forma, i.Voce) switch
                {
                    (_, Voce.Raggio) => P(("diametro", v * 2)),
                    (Forme.Sfera, Voce.Diametro) => P(("diametro", v)),
                    (Forme.Sfera, _) => null,
                    (_, Voce.Altezza or Voce.Lunghezza or Voce.Spessore) => P(("altezza", v)),
                    (Forme.Cilindro, Voce.Diametro or Voce.Esterno or Voce.Base) => P(("diametro", v)),
                    (Forme.Tubo, Voce.Diametro or Voce.Esterno) => P(("diametro", v)),
                    (Forme.Tubo, Voce.Interno) => P(("diametro_interno", v)),
                    (Forme.Cono, Voce.Diametro or Voce.Base) => P(("diametro", v)),
                    (Forme.Cono, Voce.Cima) => P(("diametro_cima", v)),
                    _ => null,
                };
            }

            if (valori == null) _nonCapito.Add(i.Testo);   // misura che non vale per questa forma
            else Imposta(i, valori);
        }

        // Numeri senza parola accanto: riempiono, in ordine, le misure che mancano.
        private void AssegnaNumeri()
        {
            var cubo = _parolaForma != null && ParoleCubo.Contains(_parolaForma);
            var ordine = _forma switch
            {
                Forme.Blocco or Forme.Piastra => new[] { "larghezza", "profondita", "altezza" },
                Forme.Cilindro => new[] { "diametro", "altezza" },
                Forme.Tubo => new[] { "diametro", "diametro_interno", "altezza" },
                Forme.Sfera => new[] { "diametro" },
                _ => new[] { "diametro", "altezza" },
            };

            foreach (var n in _numeri)
            {
                (string, decimal)[]? valori = null;
                if (cubo && !_valori.ContainsKey("larghezza") && !_valori.ContainsKey("profondita") && !_valori.ContainsKey("altezza"))
                    valori = P(("larghezza", n.Valore), ("profondita", n.Valore), ("altezza", n.Valore));
                else
                {
                    var libero = ordine.FirstOrDefault(p => !_valori.ContainsKey(p) && !(p == "diametro_interno" && _parete != null));
                    // Tubo: un secondo numero più grande del diametro è la lunghezza, non il foro.
                    if (libero == "diametro_interno" && _valori.TryGetValue("diametro", out var d) && n.Valore >= d)
                        libero = _valori.ContainsKey("altezza") ? null : "altezza";
                    if (libero != null) valori = P((libero, n.Valore));
                }
                if (valori == null) continue;   // resta tra le parole non capite
                Imposta(n, valori);
                Segna(n.Posizione, n.Lunghezza);
            }
        }

        // Misura essenziale non scritta: vale la risposta "misura:<nome>"; altrimenti la si
        // chiede, tenendo intanto un valore provvisorio (serve solo a completare i parametri).
        private void Essenziale(string nome, decimal provvisorio, params Opzione[] scorciatoie)
        {
            if (_valori.ContainsKey(nome)) return;
            if (RispostaNumero("misura:" + nome) is decimal v)
            {
                _valori[nome] = v;
                return;
            }
            _valori[nome] = provvisorio;
            var p = FormeBaseIa3d.Definizione.Parametri.First(x => x.Nome == nome);
            _domande.Add(new Domanda("misura:" + nome, $"Che {Etichetta(nome)} deve avere?", TipiDomanda.Numero,
                scorciatoie, "mm", p.Min, p.Max, provvisorio, $"In millimetri, da {Scrivi(p.Min!.Value)} a {Scrivi(p.Max!.Value)}."));
        }

        // Lati uguali (cubo, quadrato): il primo lato scritto vale per tutti; se nessuno è
        // scritto vale la risposta "misura:lato", altrimenti si chiede il lato.
        private void Lato(decimal provvisorio, params string[] nomi)
        {
            var lato = nomi.Select(n => _valori.TryGetValue(n, out var v) ? v : (decimal?)null).FirstOrDefault(v => v != null)
                ?? RispostaNumero("misura:lato");
            if (lato == null)
            {
                lato = provvisorio;
                _domande.Add(new Domanda("misura:lato", "Quanto è lungo il lato?", TipiDomanda.Numero, [], "mm",
                    1, FormeBaseIa3d.MisuraMassima, provvisorio, $"In millimetri, da 1 a {Scrivi(FormeBaseIa3d.MisuraMassima)}."));
            }
            foreach (var nome in nomi)
                _valori.TryAdd(nome, lato.Value);
        }

        private static string Scrivi(decimal valore) => valore.ToString("0.##", CultureInfo.GetCultureInfo("it-IT"));

        // Misure essenziali mancanti (domande), parete del tubo, angoli e fori.
        private void Completa()
        {
            switch (_forma)
            {
                case Forme.Blocco or Forme.Piastra:
                {
                    var piastra = _forma == Forme.Piastra;
                    if (_parolaForma != null && ParoleCubo.Contains(_parolaForma))
                        Lato(20, "larghezza", "profondita", "altezza");
                    else if (_quadrato)
                        Lato(piastra ? 60 : 20, "larghezza", "profondita");
                    Essenziale("larghezza", piastra ? 100 : 40);
                    Essenziale("profondita", piastra ? 60 : 20);
                    Essenziale("altezza", piastra ? 3 : 10);
                    CompletaAngoli();
                    break;
                }
                case Forme.Cilindro:
                    Essenziale("diametro", 20);
                    Essenziale("altezza", 20);
                    break;
                case Forme.Tubo:
                {
                    Essenziale("diametro", 20);
                    var d = _valori["diametro"];
                    if (_parete is decimal parete)
                    {
                        if (_valori.ContainsKey("diametro_interno"))
                            _avvisi.Add($"Parete di {FormeBaseIa3d.Mm(parete)} non usata: c'è già il diametro interno.");
                        else
                            _valori["diametro_interno"] = Math.Max(0, d - 2 * parete);
                    }
                    Essenziale("diametro_interno", d > 6 ? d - 4 : Math.Round(d / 2, 1));
                    Essenziale("altezza", SpessoreEAltezza ? 3 : 20);
                    break;
                }
                case Forme.Sfera:
                    Essenziale("diametro", 30);
                    break;
                case Forme.Cono:
                    Essenziale("diametro", 30);
                    Essenziale("diametro_cima", 0, new Opzione("0", "A punta"));
                    Essenziale("altezza", 30);
                    break;
            }

            if (_arrotondati != null && _forma is not (Forme.Blocco or Forme.Piastra))
                _avvisi.Add($"«{_arrotondati.Testo}»: gli angoli arrotondati valgono solo per blocco e piastra.");

            CompletaFori();
        }

        private void CompletaAngoli()
        {
            if (_arrotondati == null) return;
            if (_raggioAngoli is decimal r)
            {
                Imposta(_arrotondati, ("raggio_angoli", r));
                return;
            }
            // Dettaglio, non essenziale: raggio predefinito, modificabile nella correzione.
            _capito.Add((_arrotondati.Posizione, new Parte(_arrotondati.Testo, "angoli arrotondati")));
            var lato = Math.Min(_valori["larghezza"], _valori["profondita"]);
            Predefinito("raggio_angoli", Math.Round(Math.Min(3, lato / 4), 1));
        }

        // Diametri dei fori passanti per le viti più comuni (scorciatoie della domanda).
        private static readonly Opzione[] ForiViti =
        [
            new("3.4", "M3 · 3,4 mm"), new("4.5", "M4 · 4,5 mm"), new("5.5", "M5 · 5,5 mm"), new("6.6", "M6 · 6,6 mm"),
        ];

        private void CompletaFori()
        {
            if (_fori == null) return;
            if (_fori.Quanti == 0)
            {
                _capito.Add((_fori.Posizione, new Parte(_fori.Testo, "nessun foro")));
                return;
            }
            if (!FormeBaseIa3d.ConFori(_forma!))
            {
                _avvisi.Add($"«{_fori.Testo}»: per ora i fori si fanno solo su blocco, piastra e cilindro.");
                return;
            }

            var rettangolo = _forma != Forme.Cilindro;
            var dove = _fori.Dove;
            var inAttesa = false;

            // Quanti: scritto, implicito ("agli angoli" = 4) oppure chiesto.
            var quanti = _fori.Quanti;
            if (quanti == null && rettangolo && dove == Posizioni.Angoli) quanti = 4;
            if (quanti == null && RispostaNumero("fori:numero") is decimal r && r >= 1 && r <= 50 && r == decimal.Truncate(r))
                quanti = (int)r;
            if (quanti == null)
            {
                _domande.Add(new Domanda("fori:numero", "Quanti fori devo fare?", TipiDomanda.Numero,
                    [new("1", "1"), new("2", "2"), new("4", "4")], null, 1, 50, 2));
                inAttesa = true;
            }
            var n = quanti ?? 2;

            // Correzioni automatiche delle posizioni che non si possono fare (segnalate).
            if (rettangolo && dove == Posizioni.Cerchio)
            {
                var nuova = n == 1 ? Posizioni.Centro : n == 4 ? Posizioni.Angoli : Posizioni.Fila;
                _avvisi.Add($"Sul blocco e sulla piastra i fori in cerchio non ci sono ancora: li metto {FormeBaseIa3d.NomePosizione(nuova)}.");
                dove = nuova;
            }
            if (!rettangolo && dove is Posizioni.Angoli or Posizioni.Fila)
            {
                var nuova = n == 1 ? Posizioni.Centro : Posizioni.Cerchio;
                _avvisi.Add($"Sul cilindro i fori vanno al centro o in cerchio: li metto {FormeBaseIa3d.NomePosizione(nuova)}.");
                dove = nuova;
            }
            if (dove == Posizioni.Angoli && n != 4)
            {
                _avvisi.Add("Agli angoli vanno 4 fori: ne metto 4.");
                n = 4;
            }
            if (dove == Posizioni.Centro && n > 1)
            {
                var nuova = rettangolo ? Posizioni.Fila : Posizioni.Cerchio;
                _avvisi.Add($"Al centro va un solo foro: con {n} fori li metto {FormeBaseIa3d.NomePosizione(nuova)}.");
                dove = nuova;
            }

            // Posizione non scritta: si chiede solo quando c'è davvero da scegliere
            // (4 fori su blocco o piastra: agli angoli o in fila).
            if (dove == null && !inAttesa)
            {
                if (n == 1) dove = Posizioni.Centro;
                else if (!rettangolo) dove = Posizioni.Cerchio;
                else if (n != 4) dove = Posizioni.Fila;
                else if (RispostaTesto("fori:posizione") is Posizioni.Angoli or Posizioni.Fila) dove = RispostaTesto("fori:posizione");
                else
                {
                    _domande.Add(new Domanda("fori:posizione", "Dove metto i 4 fori?", TipiDomanda.Scelta,
                        [new(Posizioni.Angoli, "Agli angoli"), new(Posizioni.Fila, "In fila")]));
                    inAttesa = true;
                }
            }
            dove ??= Posizioni.Centro;

            // Diametro: scritto (anche come vite M) oppure chiesto.
            var diametro = _fori.Diametro ?? RispostaNumero("fori:diametro");
            if (diametro == null)
            {
                _domande.Add(new Domanda("fori:diametro", n == 1 ? "Che diametro deve avere il foro?" : "Che diametro devono avere i fori?",
                    TipiDomanda.Numero, ForiViti, "mm", 0.5m, 100, 4, "In millimetri. Per una vite scegli la sua misura M."));
                inAttesa = true;
            }

            _valori["fori"] = n;
            _valori["diametro_fori"] = diametro ?? 4;
            _posizioneFori = dove;
            _capito.Add((_fori.Posizione, new Parte(_fori.Testo, inAttesa
                ? "fori (mi servono ancora dei dettagli)"
                : $"{n} {(n == 1 ? "foro" : "fori")} da {FormeBaseIa3d.Mm(diametro!.Value)} {FormeBaseIa3d.NomePosizione(dove)}")));
        }

        // -----------------------------------------------------------------------
        // Risultato
        // -----------------------------------------------------------------------

        // Tutti i parametri del modello: quelli trovati e, per gli altri, i valori di partenza.
        private Dictionary<string, object> Parametri()
        {
            var parametri = FormeBaseIa3d.Definizione.Parametri.ToDictionary(p => p.Nome, p => p.Predefinito);
            foreach (var (nome, valore) in _valori)
                parametri[nome] = valore;
            parametri["forma"] = _forma!;
            parametri["posizione_fori"] = _posizioneFori ?? Posizioni.Centro;
            return parametri;
        }

        private List<Parte> Capito() => _capito.OrderBy(c => c.Posizione).Select(c => c.Parte).ToList();

        // Misure che non valgono per la forma e parole rimaste senza significato.
        private List<string> NonCapito()
        {
            var parole = new List<string>(_nonCapito);
            foreach (Match m in ModelloParola.Matches(new string(_testo)))
                if (!ParoleVuote.Contains(m.Value))
                    parole.Add(Pezzo(m));
            return parole.Distinct().ToList();
        }
    }
}
