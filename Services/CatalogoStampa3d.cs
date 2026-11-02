using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GestionalePersone.Services;

// Catalogo del modulo Stampa 3D: gli oggetti che si possono configurare, con le loro
// misure regolabili. Ogni oggetto ha un file OpenSCAD in Modelli3d/ con gli stessi
// nomi di parametro. Per aggiungere un oggetto: il file .scad e una voce qui.
public static partial class CatalogoStampa3d
{
    public static class Tipi
    {
        public const string Numero = "numero";
        public const string Testo = "testo";
        public const string Scelta = "scelta";
        public const string SiNo = "si-no";
    }

    public record Scelta(string Valore, string Etichetta);

    public record Parametro(
        string Nome, string Etichetta, string Tipo, object Predefinito,
        string? Unita = null, decimal? Min = null, decimal? Max = null, decimal? Passo = null,
        int? MaxLunghezza = null, Scelta[]? Scelte = null, string? Aiuto = null);

    public record Oggetto(string Codice, string Nome, string Descrizione, string Icona, Parametro[] Parametri)
    {
        public string FileScad => Codice + ".scad";
    }

    private static Parametro Numero(string nome, string etichetta, decimal predefinito, decimal min, decimal max,
        decimal passo = 1, string unita = "mm", string? aiuto = null) =>
        new(nome, etichetta, Tipi.Numero, predefinito, unita, min, max, passo, Aiuto: aiuto);

    private static Parametro Testo(string nome, string etichetta, string predefinito, int maxLunghezza, string? aiuto = null) =>
        new(nome, etichetta, Tipi.Testo, predefinito, MaxLunghezza: maxLunghezza, Aiuto: aiuto);

    private static Parametro SceltaTra(string nome, string etichetta, string predefinito, params Scelta[] scelte) =>
        new(nome, etichetta, Tipi.Scelta, predefinito, Scelte: scelte);

    public static readonly Oggetto[] Tutti =
    [
        new("targhetta", "Targhetta con nome", "Per porte, cassette postali, regali: testo in rilievo e fori per fissarla.", "card-text",
        [
            Testo("testo", "Testo", "CASA", 20),
            Numero("larghezza", "Larghezza", 100, 30, 200),
            Numero("altezza", "Altezza", 30, 15, 100),
            Numero("spessore", "Spessore", 3, 1.5m, 10, 0.5m),
            Numero("dimensione_testo", "Altezza delle lettere", 12, 4, 60),
            Numero("rilievo", "Rilievo del testo", 1.2m, 0.4m, 5, 0.2m),
            new("fori", "Fori per le viti", Tipi.SiNo, true),
            Numero("raggio", "Raggio degli angoli", 4, 0, 20),
        ]),
        new("portachiavi", "Portachiavi", "Piccolo e veloce da stampare: iniziali o nome con l'anello per le chiavi.", "key",
        [
            Testo("testo", "Testo", "M", 10),
            SceltaTra("forma", "Forma", "rettangolo",
                new("rettangolo", "Rettangolo"), new("cerchio", "Cerchio"), new("cuore", "Cuore")),
            Numero("larghezza", "Larghezza", 50, 25, 90),
            Numero("spessore", "Spessore", 3, 2, 6, 0.5m),
            Numero("dimensione_testo", "Altezza delle lettere", 10, 4, 40),
            Numero("rilievo", "Rilievo del testo", 1, 0.4m, 3, 0.2m),
        ]),
        new("scatola", "Scatola con coperchio", "Per organizzare oggetti: misure interne, scomparti e coperchio a incastro.", "box-seam",
        [
            Numero("larghezza", "Larghezza interna", 80, 20, 200),
            Numero("profondita", "Profondità interna", 60, 20, 200),
            Numero("altezza", "Altezza interna", 40, 10, 150),
            Numero("pareti", "Spessore delle pareti", 2, 1.2m, 5, 0.2m),
            Numero("scomparti", "Scomparti", 1, 1, 6, 1, "", "1 = nessun divisorio."),
            Numero("tolleranza", "Gioco del coperchio", 0.3m, 0.1m, 1, 0.05m, "mm", "Aumentalo se il coperchio entra troppo stretto."),
        ]),
        new("portapenne", "Portapenne", "Da scrivania: cilindro o esagono, con divisori interni.", "pencil",
        [
            SceltaTra("forma", "Forma", "cilindro", new("cilindro", "Cilindro"), new("esagono", "Esagono")),
            Numero("diametro", "Diametro", 70, 40, 150),
            Numero("altezza", "Altezza", 100, 40, 200),
            Numero("pareti", "Spessore delle pareti", 2, 1.2m, 5, 0.2m),
            Numero("scomparti", "Scomparti", 1, 1, 6, 1, "", "1 = nessun divisorio."),
        ]),
        new("supporto_telefono", "Supporto per telefono o tablet", "Base con bordo e schienale inclinato. Si stampa coricato su un fianco.", "phone",
        [
            Numero("larghezza", "Larghezza del supporto", 70, 40, 200),
            Numero("spessore_dispositivo", "Spessore del dispositivo", 11, 6, 30, 0.5m, "mm", "Con la cover."),
            Numero("inclinazione", "Inclinazione", 65, 45, 80, 1, "°"),
            Numero("altezza_schienale", "Altezza dello schienale", 80, 50, 160),
        ]),
        new("gancio", "Gancio a muro", "Piastra con due fori per le viti e braccio con punta. Si stampa coricato su un fianco.", "paperclip",
        [
            Numero("larghezza", "Larghezza", 20, 10, 60),
            Numero("altezza", "Altezza della piastra", 60, 40, 150),
            Numero("sporgenza", "Sporgenza del braccio", 40, 20, 120),
            Numero("spessore", "Spessore", 5, 3, 12, 0.5m, "mm", "Più spesso = regge più peso."),
            Numero("diametro_foro", "Diametro dei fori", 4, 2.5m, 8, 0.5m),
        ]),
        new("sottobicchiere", "Sottobicchiere", "Rotondo, esagonale o quadrato, con bordo e testo inciso.", "cup-hot",
        [
            SceltaTra("forma", "Forma", "cerchio",
                new("cerchio", "Cerchio"), new("esagono", "Esagono"), new("quadrato", "Quadrato")),
            Numero("diametro", "Diametro", 95, 60, 150),
            Numero("spessore", "Spessore", 4, 2, 8, 0.5m),
            Numero("bordo", "Bordo rialzato", 1.5m, 0, 4, 0.5m, "mm", "0 = piatto."),
            Testo("testo", "Testo inciso", "", 12, "Lascia vuoto per nessun testo."),
        ]),
        new("distanziale", "Distanziale / rondella", "Pezzo tecnico per riparazioni e montaggi.", "nut",
        [
            Numero("diametro_interno", "Diametro interno", 8.4m, 1, 100, 0.1m),
            Numero("diametro_esterno", "Diametro esterno", 16, 3, 120, 0.1m),
            Numero("spessore", "Spessore", 2, 0.4m, 50, 0.2m),
        ]),
    ];

    public static Oggetto? Trova(string? codice) => Tutti.FirstOrDefault(o => o.Codice == codice);

    // Testi ammessi: lettere (anche accentate), numeri, spazio e pochi simboli. Niente
    // virgolette o barre: il valore finisce dentro un file OpenSCAD.
    [GeneratedRegex(@"^[\p{L}\p{N} .,\-'!?&+#()]*$")]
    private static partial Regex TestoAmmesso();

    // Controlla i parametri ricevuti e li converte in valori OpenSCAD (-D nome=valore).
    // Parametri mancanti: valore predefinito. Parametri sconosciuti: errore.
    public static (Dictionary<string, string> Valori, Dictionary<string, object> Puliti, List<string> Errori)
        Valida(Oggetto oggetto, IReadOnlyDictionary<string, JsonElement>? ricevuti)
    {
        var valori = new Dictionary<string, string>();
        var puliti = new Dictionary<string, object>();
        var errori = new List<string>();
        ricevuti ??= new Dictionary<string, JsonElement>();

        foreach (var nome in ricevuti.Keys.Where(k => oggetto.Parametri.All(p => p.Nome != k)))
            errori.Add($"Parametro sconosciuto: {nome}.");

        foreach (var p in oggetto.Parametri)
        {
            var presente = ricevuti.TryGetValue(p.Nome, out var v) && v.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined);
            switch (p.Tipo)
            {
                case Tipi.Numero:
                {
                    decimal n;
                    if (!presente) n = Convert.ToDecimal(p.Predefinito, CultureInfo.InvariantCulture);
                    else if (v.ValueKind != JsonValueKind.Number || !v.TryGetDecimal(out n))
                    {
                        errori.Add($"{p.Etichetta}: inserisci un numero.");
                        continue;
                    }
                    if (n < p.Min || n > p.Max)
                    {
                        errori.Add($"{p.Etichetta}: deve essere tra {p.Min?.ToString(CultureInfo.GetCultureInfo("it-IT"))} e {p.Max?.ToString(CultureInfo.GetCultureInfo("it-IT"))}.");
                        continue;
                    }
                    valori[p.Nome] = n.ToString(CultureInfo.InvariantCulture);
                    puliti[p.Nome] = n;
                    break;
                }
                case Tipi.Testo:
                {
                    var s = !presente ? (string)p.Predefinito : v.ValueKind == JsonValueKind.String ? v.GetString()!.Trim() : null;
                    if (s == null || s.Length > p.MaxLunghezza || !TestoAmmesso().IsMatch(s))
                    {
                        errori.Add($"{p.Etichetta}: al massimo {p.MaxLunghezza} caratteri, solo lettere, numeri, spazi e . , - ' ! ? & + # ( ).");
                        continue;
                    }
                    valori[p.Nome] = "\"" + s + "\"";
                    puliti[p.Nome] = s;
                    break;
                }
                case Tipi.Scelta:
                {
                    var s = !presente ? (string)p.Predefinito : v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                    if (s == null || p.Scelte!.All(c => c.Valore != s))
                    {
                        errori.Add($"{p.Etichetta}: scelta non valida.");
                        continue;
                    }
                    valori[p.Nome] = "\"" + s + "\"";
                    puliti[p.Nome] = s;
                    break;
                }
                case Tipi.SiNo:
                {
                    bool b;
                    if (!presente) b = (bool)p.Predefinito;
                    else if (v.ValueKind is JsonValueKind.True or JsonValueKind.False) b = v.GetBoolean();
                    else
                    {
                        errori.Add($"{p.Etichetta}: valore non valido.");
                        continue;
                    }
                    valori[p.Nome] = b ? "true" : "false";
                    puliti[p.Nome] = b;
                    break;
                }
            }
        }

        // Controlli tra parametri.
        if (oggetto.Codice == "distanziale" && errori.Count == 0 &&
            (decimal)puliti["diametro_interno"] >= (decimal)puliti["diametro_esterno"] - 1)
            errori.Add("Il diametro esterno deve superare quello interno di almeno 1 mm.");

        return (valori, puliti, errori);
    }
}
