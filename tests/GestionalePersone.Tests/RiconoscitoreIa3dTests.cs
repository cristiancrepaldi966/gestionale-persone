using System.Globalization;
using GestionalePersone.Services;

namespace GestionalePersone.Tests;

// Frasi come le scriverebbe una persona, domande del dialogo e risultato atteso.
public class RiconoscitoreIa3dTests
{
    // "nome=valore;nome=valore" → coppie in ordine (risposte o misure attese).
    private static List<(string Nome, string Valore)> Coppie(string testo) =>
        testo.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Split('=', 2))
            .Select(c => (c[0], c[1]))
            .ToList();

    private static void ControllaMisure(RiconoscitoreIa3d.Risultato r, string misure)
    {
        foreach (var (nome, atteso) in Coppie(misure))
        {
            var trovato = r.Parametri[nome];
            if (trovato is decimal d)
                Assert.True(d == decimal.Parse(atteso, CultureInfo.InvariantCulture), $"{nome}: {d} invece di {atteso}");
            else
                Assert.Equal(atteso, trovato);
        }
    }

    // ---------------------------------------------------------------------------
    // Frasi complete: nessuna domanda
    // ---------------------------------------------------------------------------

    public static TheoryData<string, string, string> FrasiComplete => new()
    {
        // Esempi della pagina
        { "piastra 100x60 spessore 4 con 4 fori da 5 agli angoli", "piastra", "larghezza=100;profondita=60;altezza=4;fori=4;diametro_fori=5;posizione_fori=angoli" },
        { "cilindro diametro 30 alto 50 con un foro centrale da 8", "cilindro", "diametro=30;altezza=50;fori=1;diametro_fori=8;posizione_fori=centro" },
        { "tubo diametro 20 interno 16 lungo 100", "tubo", "diametro=20;diametro_interno=16;altezza=100" },
        { "cubo 20 con angoli arrotondati", "blocco", "larghezza=20;profondita=20;altezza=20;raggio_angoli=3" },
        { "cono base 40 punta 10 alto 30", "cono", "diametro=40;diametro_cima=10;altezza=30" },
        { "sfera 4 cm", "sfera", "diametro=40" },

        // Unità, separatori, ordine delle parole
        { "Blocco 10 x 20 x 5 cm", "blocco", "larghezza=100;profondita=200;altezza=50" },
        { "una piastra di 80 per 40 per 3", "piastra", "larghezza=80;profondita=40;altezza=3" },
        { "lastra lunga 120 larga 40 spessa 2", "piastra", "larghezza=120;profondita=40;altezza=2" },
        { "50 di altezza, cilindro di diametro 25", "cilindro", "diametro=25;altezza=50" },
        { "piastra quadrata 50 spessore 3", "piastra", "larghezza=50;profondita=50;altezza=3" },
        { "Cilindro Ø30 h 12", "cilindro", "diametro=30;altezza=12" },
        { "perno raggio 4 lungo 60", "cilindro", "diametro=8;altezza=60" },
        { "cubo lato 3 cm", "blocco", "larghezza=30;profondita=30;altezza=30" },
        { "Profondità 30, larghezza 70, altezza 15: un blocchetto", "blocco", "larghezza=70;profondita=30;altezza=15" },
        { "🙂 cubo 10 😀", "blocco", "larghezza=10;profondita=10;altezza=10" },
        { "base 100x60 spessore 3", "piastra", "larghezza=100;profondita=60;altezza=3" },
        { "cono alto 30 diametro 20 a punta", "cono", "diametro=20;diametro_cima=0;altezza=30" },

        // Tubi
        { "distanziale esterno 16 interno 8,4 spessore 2", "tubo", "diametro=16;diametro_interno=8.4;altezza=2" },
        { "tubo 20x16 lungo 50", "tubo", "diametro=20;diametro_interno=16;altezza=50" },
        { "tubo diametro 30 parete 2 alto 40", "tubo", "diametro=30;diametro_interno=26;altezza=40" },
        { "rondella ø 20 foro interno 10 spessore 1,5", "tubo", "diametro=20;diametro_interno=10;altezza=1.5" },

        // Fori
        { "piastra 100x50x3 con 3 fori per viti M4 in fila arrotondata", "piastra", "fori=3;diametro_fori=4.5;posizione_fori=fila;raggio_angoli=3" },
        { "cilindro diametro 60 alto 10 con 6 fori da 4 in cerchio", "cilindro", "fori=6;diametro_fori=4;posizione_fori=cerchio" },
        { "cilindro 50 alto 5 con 5 fori da 3 in tondo", "cilindro", "diametro=50;altezza=5;fori=5;posizione_fori=cerchio" },
        { "piastra 80x40x5 con un foro da 6 mm di diametro", "piastra", "fori=1;diametro_fori=6;posizione_fori=centro" },
        { "piastra 80x40x5 con 3 fori da 4", "piastra", "fori=3;posizione_fori=fila" },
        { "sfera 30 con 2 fori", "sfera", "diametro=30;fori=0" },
    };

    [Theory]
    [MemberData(nameof(FrasiComplete))]
    public void FraseCompletaNonFaDomande(string testo, string forma, string misure)
    {
        var r = RiconoscitoreIa3d.Riconosci(testo);

        Assert.True(r.Domande.Count == 0, "Domande: " + string.Join(", ", r.Domande.Select(d => d.Codice)));
        Assert.Equal(forma, r.Forma);
        ControllaMisure(r, misure);
        var (_, pezzo, errori) = FormeBaseIa3d.Valida(Utili.InJson(r.Parametri));
        Assert.True(pezzo != null, string.Join(" ", errori));
    }

    // ---------------------------------------------------------------------------
    // Frasi incomplete: domanda dopo domanda, poi il pezzo completo
    // ---------------------------------------------------------------------------

    // Testo, domande in ordine (una per passo) con la risposta data, forma e misure finali.
    public static TheoryData<string, string, string, string> Dialoghi => new()
    {
        { "100x60x3", "forma=blocco", "blocco", "larghezza=100;profondita=60;altezza=3" },
        { "anello 30", "misura:diametro_interno=20;misura:altezza=5", "tubo", "diametro=30;diametro_interno=20;altezza=5" },
        { "piastra 50x50 con 2 fori al centro", "misura:altezza=3;fori:diametro=4.5", "piastra", "altezza=3;fori=2;diametro_fori=4.5;posizione_fori=fila" },
        { "piastra 100x60 con fori", "misura:altezza=3;fori:numero=4;fori:posizione=angoli;fori:diametro=5.5", "piastra", "fori=4;posizione_fori=angoli;diametro_fori=5.5" },
        { "piastra 100x60 con 4 fori da 6 e angoli arrotondati raggio 8", "misura:altezza=4;fori:posizione=fila", "piastra", "altezza=4;fori=4;posizione_fori=fila;raggio_angoli=8" },
        { "cubo e cilindro", "misura:lato=25", "blocco", "larghezza=25;profondita=25;altezza=25" },
        { "blocco alto 10 alto 20", "misura:larghezza=40;misura:profondita=30", "blocco", "larghezza=40;profondita=30;altezza=10" },
        { "palla blu da 5 cm", "parole=ignora", "sfera", "diametro=50" },
        { "cono base 40 alto 30", "misura:diametro_cima=0", "cono", "diametro=40;diametro_cima=0;altezza=30" },
        { "scatolina 50x30 con un buco", "forma=blocco;misura:altezza=20;fori:diametro=6", "blocco", "larghezza=50;profondita=30;altezza=20;fori=1;diametro_fori=6" },
        { "ciao come stai", "forma=sfera;parole=ignora;misura:diametro=30", "sfera", "diametro=30" },
        { "cilindro alto 10 con fori in cerchio", "misura:diametro=60;fori:numero=6;fori:diametro=3.4", "cilindro", "diametro=60;fori=6;posizione_fori=cerchio;diametro_fori=3.4" },
    };

    [Theory]
    [MemberData(nameof(Dialoghi))]
    public void DialogoFinoAlPezzoCompleto(string testo, string passi, string forma, string misure)
    {
        // Ogni passo: la prima domanda dev'essere quella attesa; poi si aggiunge la risposta.
        var risposte = new Dictionary<string, string>();
        foreach (var (codice, valore) in Coppie(passi))
        {
            var r = RiconoscitoreIa3d.Riconosci(testo, risposte);
            Assert.True(r.Domande.Count > 0, $"Mi aspettavo la domanda «{codice}», nessuna domanda.");
            Assert.Equal(codice, r.Domande[0].Codice);
            risposte[codice] = valore;
        }

        var finale = RiconoscitoreIa3d.Riconosci(testo, risposte);
        Assert.True(finale.Domande.Count == 0, "Domande rimaste: " + string.Join(", ", finale.Domande.Select(d => d.Codice)));
        Assert.Equal(forma, finale.Forma);
        ControllaMisure(finale, misure);
        var (_, pezzo, errori) = FormeBaseIa3d.Valida(Utili.InJson(finale.Parametri));
        Assert.True(pezzo != null, string.Join(" ", errori));
    }

    [Fact]
    public void SenzaFormaChiedeLaFormaProponendoQuellaDedotta()
    {
        var r = RiconoscitoreIa3d.Riconosci("100x60x3");
        Assert.Null(r.Forma);
        var domanda = Assert.Single(r.Domande);
        Assert.Equal("forma", domanda.Codice);
        Assert.Equal(RiconoscitoreIa3d.TipiDomanda.Scelta, domanda.Tipo);
        Assert.Equal("blocco", domanda.Opzioni[0].Valore);
        Assert.Contains("dalle misure", domanda.Opzioni[0].Etichetta);
        Assert.Equal(6, domanda.Opzioni.Length);
    }

    [Fact]
    public void LaParolaSconosciutaDiventaIlNomeDellaForma()
    {
        var r = RiconoscitoreIa3d.Riconosci("scatolina 50x30x20");
        Assert.Equal("Non conosco «scatolina»: che forma è?", r.Domande[0].Testo);

        var risposta = RiconoscitoreIa3d.Riconosci("scatolina 50x30x20", new Dictionary<string, string> { ["forma"] = "blocco" });
        Assert.Empty(risposta.Domande);
        Assert.Contains(risposta.Capito, p => p.Testo == "scatolina" && p.Significato == "blocco (tua risposta)");
        Assert.Empty(risposta.NonCapito);
    }

    [Fact]
    public void DomandaSullaMisuraHaLimitiEUnita()
    {
        var r = RiconoscitoreIa3d.Riconosci("cilindro diametro 30");
        var domanda = Assert.Single(r.Domande);
        Assert.Equal("misura:altezza", domanda.Codice);
        Assert.Equal("Che altezza deve avere?", domanda.Testo);
        Assert.Equal(RiconoscitoreIa3d.TipiDomanda.Numero, domanda.Tipo);
        Assert.Equal("mm", domanda.Unita);
        Assert.Equal(0.4m, domanda.Min);
        Assert.Equal(250m, domanda.Max);
    }

    [Fact]
    public void DomandaSulDiametroDeiForiProponeLeViti()
    {
        var r = RiconoscitoreIa3d.Riconosci("piastra 80x40x5 con un foro");
        var domanda = Assert.Single(r.Domande);
        Assert.Equal("fori:diametro", domanda.Codice);
        Assert.Contains(domanda.Opzioni, o => o.Valore == "4.5" && o.Etichetta.StartsWith("M4"));
    }

    [Theory]
    [InlineData("cilindro diametro 30", "misura:altezza", "abc")]
    [InlineData("cilindro diametro 30", "misura:altezza", "-5")]
    [InlineData("piastra 80x40x5 con fori da 4", "fori:numero", "2.5")]
    [InlineData("piastra 80x40x5 con fori da 4", "fori:numero", "0")]
    [InlineData("piastra 80x40x5 con 4 fori da 4", "fori:posizione", "cerchio")]
    [InlineData("100x60x3", "forma", "astronave")]
    public void UnaRispostaNonValidaRifaLaDomanda(string testo, string codice, string valore)
    {
        var r = RiconoscitoreIa3d.Riconosci(testo, new Dictionary<string, string> { [codice] = valore });
        Assert.Equal(codice, r.Domande[0].Codice);
    }

    [Fact]
    public void ParoleNonCapiteVengonoChiestePerPrime()
    {
        var r = RiconoscitoreIa3d.Riconosci("palla blu");
        Assert.Equal("parole", r.Domande[0].Codice);
        Assert.Equal("Non ho capito «blu». Cosa faccio?", r.Domande[0].Testo);
        Assert.Equal(new[] { "ignora", "riscrivi" }, r.Domande[0].Opzioni.Select(o => o.Valore));
        Assert.Equal("misura:diametro", r.Domande[1].Codice);
    }

    // ---------------------------------------------------------------------------
    // Spiegazioni, testo originale, valori predefiniti, misure impossibili
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData("cubo e cilindro", "una forma alla volta")]
    [InlineData("piastra 50x50x3 con 2 fori da 4 al centro", "solo foro")]
    [InlineData("piastra 50x50x3 con 3 fori da 4 agli angoli", "vanno 4 fori")]
    [InlineData("sfera 30 con 2 fori", "solo su blocco")]
    [InlineData("blocco alto 10 alto 20", "non usato")]
    [InlineData("piastra 100x60x3 arrotondata", "valore predefinito: raggio degli angoli 3 mm")]
    public void SpiegaCosaHaFatto(string testo, string avviso)
    {
        var r = RiconoscitoreIa3d.Riconosci(testo);
        Assert.Contains(r.Avvisi, a => a.Contains(avviso));
    }

    [Fact]
    public void MostraIlTestoScrittoDallUtente()
    {
        var r = RiconoscitoreIa3d.Riconosci("Profondità 30, larghezza 70, altezza 15: un blocchetto");
        Assert.Contains(r.Capito, p => p.Testo == "Profondità 30");
    }

    [Fact]
    public void SegnalaIValoriPredefiniti()
    {
        var r = RiconoscitoreIa3d.Riconosci("piastra 100x60x3 arrotondata");
        Assert.Contains(r.Misure, m => m.Nome == "raggio_angoli" && m.Predefinita);
        Assert.Contains(r.Misure, m => m.Nome == "larghezza" && !m.Predefinita);
    }

    [Theory]
    [InlineData("piastra 300x60x3", "Larghezza: deve essere tra 1 e 250.")]
    [InlineData("piastra 40x40x3 con 4 fori da 15 agli angoli", "I fori non ci stanno")]
    [InlineData("tubo diametro 10 parete 6 alto 20", "diametro interno")]
    [InlineData("cilindro alto 10 diametro 12345678901234567890123456789012345678901234567890", "Diametro: deve essere tra 1 e 250.")]
    public void MisureImpossibiliVengonoRifiutate(string testo, string errore)
    {
        var r = RiconoscitoreIa3d.Riconosci(testo);
        Assert.Empty(r.Domande);
        var (_, pezzo, errori) = FormeBaseIa3d.Valida(Utili.InJson(r.Parametri));
        Assert.Null(pezzo);
        Assert.Contains(errori, e => e.Contains(errore));
    }

    [Fact]
    public void TestoLungoESenzaSensoNonBlocca()
    {
        var testo = string.Concat(Enumerable.Repeat("x9 fori per da di ", 20))[..RiconoscitoreIa3d.LunghezzaMassima];
        var r = RiconoscitoreIa3d.Riconosci(testo, new Dictionary<string, string> { ["forma"] = "piastra" });
        Assert.NotNull(r);
    }

    [Fact]
    public void MisureDaMostrarePerUnPezzoCorrettoAMano()
    {
        var (_, pezzo, _) = FormeBaseIa3d.Valida(Utili.InJson(new Dictionary<string, object>
        {
            ["forma"] = "tubo", ["diametro"] = 30m, ["diametro_interno"] = 26m, ["altezza"] = 40m,
        }));
        var misure = RiconoscitoreIa3d.MisureDi(pezzo!.Parametri);
        Assert.Equal(new[] { "diametro", "diametro_interno", "altezza", "parete" }, misure.Select(m => m.Nome));
        Assert.Equal("2 mm", misure[^1].Valore);
    }
}
