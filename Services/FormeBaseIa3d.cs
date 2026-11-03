using System.Globalization;
using System.Text.Json;

namespace GestionalePersone.Services;

// Forme base del modulo IA (pagina IA 3D): blocco, piastra, cilindro, tubo, sfera e cono,
// con fori passanti (blocco, piastra, cilindro) e angoli arrotondati (blocco, piastra).
// Un solo modello OpenSCAD, Modelli3d/ia_forme_base.scad, riceve le misure già controllate.
// La posizione dei fori la calcola il server: disegno 2D, anteprima 3D e file STL coincidono.
public static class FormeBaseIa3d
{
    public static class Forme
    {
        public const string Blocco = "blocco";
        public const string Piastra = "piastra";
        public const string Cilindro = "cilindro";
        public const string Tubo = "tubo";
        public const string Sfera = "sfera";
        public const string Cono = "cono";
    }

    public static class Posizioni
    {
        public const string Centro = "centro";
        public const string Angoli = "angoli";
        public const string Fila = "fila";
        public const string Cerchio = "cerchio";
    }

    // Misura massima per lato: circa un piano di stampa grande.
    public const decimal MisuraMassima = 250;

    // Materiale minimo tra un foro e il bordo, o tra due fori vicini.
    private const decimal PareteMinima = 0.8m;

    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");

    // Centro di un foro, in mm, rispetto al centro del pezzo.
    public record Foro(decimal X, decimal Y);

    public record Ingombro(decimal X, decimal Y, decimal Z);

    // Pezzo controllato: forma, tutte le misure, centri dei fori e ingombro.
    public record Pezzo(string Forma, string NomeForma, Dictionary<string, object> Parametri, Foro[] Fori,
        Ingombro Ingombro);

    private static CatalogoStampa3d.Parametro Numero(string nome, string etichetta, decimal predefinito,
        decimal min, decimal max, string unita = "mm") =>
        new(nome, etichetta, CatalogoStampa3d.Tipi.Numero, predefinito, unita, min, max);

    private static CatalogoStampa3d.Parametro SceltaTra(string nome, string etichetta, string predefinito,
        params CatalogoStampa3d.Scelta[] scelte) =>
        new(nome, etichetta, CatalogoStampa3d.Tipi.Scelta, predefinito, Scelte: scelte);

    // Tutte le misure del modello (stessi nomi dei parametri in ia_forme_base.scad).
    // I controlli dei singoli valori sono quelli del catalogo Stampa 3D (CatalogoStampa3d.Valida).
    public static readonly CatalogoStampa3d.Oggetto Definizione = new("ia_forme_base", "Forme base",
        "Forme base del modulo IA", "stars",
        [
            SceltaTra("forma", "Forma", Forme.Blocco,
                new(Forme.Blocco, "Blocco"), new(Forme.Piastra, "Piastra"), new(Forme.Cilindro, "Cilindro"),
                new(Forme.Tubo, "Tubo"), new(Forme.Sfera, "Sfera"), new(Forme.Cono, "Cono")),
            Numero("larghezza", "Larghezza", 40, 1, MisuraMassima),
            Numero("profondita", "Profondità", 20, 1, MisuraMassima),
            Numero("altezza", "Altezza", 10, 0.4m, MisuraMassima),
            Numero("diametro", "Diametro", 20, 1, MisuraMassima),
            Numero("diametro_interno", "Diametro interno", 0, 0, MisuraMassima),
            Numero("diametro_cima", "Diametro in cima", 0, 0, MisuraMassima),
            Numero("raggio_angoli", "Raggio degli angoli", 0, 0, MisuraMassima / 2),
            Numero("fori", "Numero di fori", 0, 0, 50, ""),
            Numero("diametro_fori", "Diametro dei fori", 4, 0.5m, 100),
            SceltaTra("posizione_fori", "Posizione dei fori", Posizioni.Centro,
                new(Posizioni.Centro, "Al centro"), new(Posizioni.Angoli, "Agli angoli"),
                new(Posizioni.Fila, "In fila"), new(Posizioni.Cerchio, "In cerchio")),
        ]);

    private static readonly Dictionary<string, string> Nomi = new()
    {
        [Forme.Blocco] = "Blocco",
        [Forme.Piastra] = "Piastra",
        [Forme.Cilindro] = "Cilindro",
        [Forme.Tubo] = "Tubo",
        [Forme.Sfera] = "Sfera",
        [Forme.Cono] = "Cono",
    };

    public static string NomeForma(string forma) => Nomi.GetValueOrDefault(forma, forma);

    // Codice → nome di tutte le forme, nell'ordine in cui si propongono.
    public static IReadOnlyDictionary<string, string> NomiForme => Nomi;

    // Forme su cui si possono fare fori passanti.
    public static bool ConFori(string forma) => forma is Forme.Blocco or Forme.Piastra or Forme.Cilindro;

    // Posizioni dei fori possibili per la forma.
    public static string[] PosizioniPer(string forma) => forma switch
    {
        Forme.Blocco or Forme.Piastra => new[] { Posizioni.Centro, Posizioni.Angoli, Posizioni.Fila },
        Forme.Cilindro => new[] { Posizioni.Centro, Posizioni.Cerchio },
        _ => Array.Empty<string>(),
    };

    // Misure che contano per ogni forma, con il nome da mostrare (in ordine).
    public static (string Nome, string Etichetta)[] MisureDi(string forma) => forma switch
    {
        Forme.Blocco => new[]
        {
            ("larghezza", "Larghezza"), ("profondita", "Profondità"), ("altezza", "Altezza"),
            ("raggio_angoli", "Raggio degli angoli"),
        },
        Forme.Piastra => new[]
        {
            ("larghezza", "Larghezza"), ("profondita", "Profondità"), ("altezza", "Spessore"),
            ("raggio_angoli", "Raggio degli angoli"),
        },
        Forme.Cilindro => new[] { ("diametro", "Diametro"), ("altezza", "Altezza") },
        Forme.Tubo => new[] { ("diametro", "Diametro esterno"), ("diametro_interno", "Diametro interno"), ("altezza", "Altezza") },
        Forme.Sfera => new[] { ("diametro", "Diametro") },
        Forme.Cono => new[] { ("diametro", "Diametro alla base"), ("diametro_cima", "Diametro in cima"), ("altezza", "Altezza") },
        _ => Array.Empty<(string, string)>(),
    };

    // Nome di una misura per la forma scelta (es. "altezza" della piastra = "Spessore").
    public static string Etichetta(string forma, string nome) =>
        MisureDi(forma).FirstOrDefault(m => m.Nome == nome).Etichetta
        ?? Definizione.Parametri.FirstOrDefault(p => p.Nome == nome)?.Etichetta
        ?? nome;

    public static string NomePosizione(string posizione) => posizione switch
    {
        Posizioni.Centro => "al centro",
        Posizioni.Angoli => "agli angoli",
        Posizioni.Fila => "in fila",
        Posizioni.Cerchio => "in cerchio",
        _ => posizione,
    };

    // 4.5 → "4,5 mm"
    public static string Mm(decimal valore) => valore.ToString("0.##", Italiano) + " mm";

    // Controlla le misure ricevute e le converte in valori OpenSCAD (-D nome=valore),
    // compresi i centri dei fori (posizioni_fori). Pezzo null se ci sono errori.
    public static (Dictionary<string, string> Valori, Pezzo? Pezzo, List<string> Errori)
        Valida(IReadOnlyDictionary<string, JsonElement>? ricevuti)
    {
        var (valori, puliti, errori) = CatalogoStampa3d.Valida(Definizione, ricevuti);
        if (errori.Count > 0)
            return (valori, null, errori);

        var forma = (string)puliti["forma"];
        decimal N(string nome) => (decimal)puliti[nome];
        var larghezza = N("larghezza");
        var profondita = N("profondita");
        var diametro = N("diametro");
        var raggio = N("raggio_angoli");

        switch (forma)
        {
            case Forme.Blocco or Forme.Piastra when raggio > Math.Min(larghezza, profondita) / 2:
                errori.Add("Il raggio degli angoli può essere al massimo metà del lato più corto.");
                break;
            case Forme.Tubo when N("diametro_interno") <= 0:
                errori.Add("Il tubo ha bisogno di un diametro interno maggiore di zero (controlla diametro e parete).");
                break;
            case Forme.Tubo when N("diametro_interno") > diametro - 2 * PareteMinima:
                errori.Add($"Il diametro esterno deve superare quello interno di almeno {Mm(2 * PareteMinima)}.");
                break;
        }

        var quanti = N("fori");
        if (quanti != decimal.Truncate(quanti))
            errori.Add("Il numero di fori deve essere un numero intero.");

        var fori = Array.Empty<Foro>();
        if (errori.Count == 0 && quanti > 0)
        {
            var (calcolati, errore) = CalcolaFori(forma, (int)quanti, (string)puliti["posizione_fori"],
                N("diametro_fori"), larghezza, profondita, raggio, diametro);
            if (errore != null) errori.Add(errore);
            else fori = calcolati;
        }

        if (errori.Count > 0)
            return (valori, null, errori);

        // Valori calcolati qui (non scritti dall'utente): vettore OpenSCAD [[x, y], ...].
        valori["posizioni_fori"] = "[" + string.Join(",", fori.Select(f => $"[{Testo(f.X)},{Testo(f.Y)}]")) + "]";
        return (valori, new Pezzo(forma, NomeForma(forma), puliti, fori, CalcolaIngombro(forma, puliti)), errori);
    }

    private static (Foro[] Fori, string? Errore) CalcolaFori(string forma, int quanti, string posizione,
        decimal df, decimal larghezza, decimal profondita, decimal raggio, decimal diametro)
    {
        const string NonCiStanno = "I fori non ci stanno: prova fori più piccoli o meno fori.";
        var nessuno = Array.Empty<Foro>();
        var interasseMinimo = df + PareteMinima;
        // Distanza del centro di un foro dal bordo: un diametro, e almeno mezzo diametro più 1,5 mm.
        var margine = Math.Max(df, df / 2 + 1.5m);

        if (forma is Forme.Blocco or Forme.Piastra)
        {
            switch (posizione)
            {
                case Posizioni.Centro:
                    if (quanti != 1)
                        return (nessuno, "Al centro va un solo foro: per più fori scegli in fila o agli angoli.");
                    if (df > Math.Min(larghezza, profondita) - 2 * PareteMinima)
                        return (nessuno, NonCiStanno);
                    return (new[] { new Foro(0, 0) }, null);

                case Posizioni.Angoli:
                {
                    if (quanti != 4)
                        return (nessuno, "Agli angoli vanno 4 fori.");
                    // Con angoli molto arrotondati il foro va al centro della curva.
                    var m = Math.Max(margine, raggio);
                    var x = larghezza / 2 - m;
                    var y = profondita / 2 - m;
                    if (2 * x < interasseMinimo || 2 * y < interasseMinimo)
                        return (nessuno, NonCiStanno);
                    x = Arrotonda(x);
                    y = Arrotonda(y);
                    return (new[] { new Foro(-x, -y), new Foro(x, -y), new Foro(x, y), new Foro(-x, y) }, null);
                }

                case Posizioni.Fila:
                {
                    if (df > profondita - 2 * PareteMinima)
                        return (nessuno, NonCiStanno);
                    if (quanti == 1)
                        return (new[] { new Foro(0, 0) }, null);
                    var x0 = larghezza / 2 - margine;
                    var passo = 2 * x0 / (quanti - 1);
                    if (x0 <= 0 || passo < interasseMinimo)
                        return (nessuno, NonCiStanno);
                    return (Enumerable.Range(0, quanti).Select(i => new Foro(Arrotonda(-x0 + i * passo), 0)).ToArray(), null);
                }

                default:
                    return (nessuno, "Sul blocco e sulla piastra i fori vanno al centro, agli angoli o in fila.");
            }
        }

        if (forma == Forme.Cilindro)
        {
            switch (posizione)
            {
                case Posizioni.Centro:
                    if (quanti != 1)
                        return (nessuno, "Al centro va un solo foro: per più fori scegli in cerchio.");
                    if (df > diametro - 2 * PareteMinima)
                        return (nessuno, NonCiStanno);
                    return (new[] { new Foro(0, 0) }, null);

                case Posizioni.Cerchio:
                {
                    if (quanti < 2)
                        return (nessuno, "In cerchio servono almeno 2 fori.");
                    var r = diametro / 2 - margine;
                    var corda = 2 * r * (decimal)Math.Sin(Math.PI / quanti);
                    if (r <= 0 || corda < interasseMinimo)
                        return (nessuno, NonCiStanno);
                    var fori = Enumerable.Range(0, quanti).Select(i =>
                    {
                        var angolo = Math.PI / 2 + 2 * Math.PI * i / quanti;   // il primo in alto
                        return new Foro(Arrotonda(r * (decimal)Math.Cos(angolo)), Arrotonda(r * (decimal)Math.Sin(angolo)));
                    }).ToArray();
                    return (fori, null);
                }

                default:
                    return (nessuno, "Sul cilindro i fori vanno al centro o in cerchio.");
            }
        }

        return (nessuno, "I fori si possono fare solo su blocco, piastra e cilindro.");
    }

    private static Ingombro CalcolaIngombro(string forma, Dictionary<string, object> p)
    {
        decimal N(string nome) => (decimal)p[nome];
        switch (forma)
        {
            case Forme.Blocco or Forme.Piastra:
                return new Ingombro(N("larghezza"), N("profondita"), N("altezza"));
            case Forme.Sfera:
                return new Ingombro(N("diametro"), N("diametro"), N("diametro"));
            case Forme.Cono:
            {
                var d = Math.Max(N("diametro"), N("diametro_cima"));
                return new Ingombro(d, d, N("altezza"));
            }
            default:   // cilindro, tubo
                return new Ingombro(N("diametro"), N("diametro"), N("altezza"));
        }
    }

    private static decimal Arrotonda(decimal valore) => Math.Round(valore, 2);

    private static string Testo(decimal valore) => valore.ToString(CultureInfo.InvariantCulture);
}
