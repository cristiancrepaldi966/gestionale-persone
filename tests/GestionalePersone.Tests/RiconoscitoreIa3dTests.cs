using GestionalePersone.Services;

namespace GestionalePersone.Tests;

// Frasi come le scriverebbe una persona e risultato atteso (forma e misure in mm).
public class RiconoscitoreIa3dTests
{
    // Misure attese nel formato "nome=valore;nome=valore".
    public static TheoryData<string, string, string> Frasi => new()
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

        // Tubi
        { "distanziale esterno 16 interno 8,4 spessore 2", "tubo", "diametro=16;diametro_interno=8.4;altezza=2" },
        { "tubo 20x16 lungo 50", "tubo", "diametro=20;diametro_interno=16;altezza=50" },
        { "tubo diametro 30 parete 2 alto 40", "tubo", "diametro=30;diametro_interno=26;altezza=40" },
        { "rondella ø 20 foro interno 10 spessore 1,5", "tubo", "diametro=20;diametro_interno=10;altezza=1.5" },
        { "anello 30", "tubo", "diametro=30;diametro_interno=26" },

        // Fori
        { "piastra 100x50x3 con 3 fori per viti M4 in fila arrotondata", "piastra", "fori=3;diametro_fori=4.5;posizione_fori=fila;raggio_angoli=3" },
        { "cilindro diametro 60 alto 10 con 6 fori da 4 in cerchio", "cilindro", "fori=6;diametro_fori=4;posizione_fori=cerchio" },
        { "cilindro 50 alto 5 con 5 fori da 3 in tondo", "cilindro", "diametro=50;altezza=5;fori=5;posizione_fori=cerchio" },
        { "piastra 50x50 con 2 fori al centro", "piastra", "fori=2;posizione_fori=fila" },
        { "piastra 100x60 con fori", "piastra", "fori=2;posizione_fori=fila;diametro_fori=4" },
        { "piastra 100x60 con 4 fori da 6 e angoli arrotondati raggio 8", "piastra", "fori=4;posizione_fori=angoli;raggio_angoli=8" },
        { "piastra 80x40x5 con un foro da 6 mm di diametro", "piastra", "fori=1;diametro_fori=6;posizione_fori=centro" },
        { "sfera 30 con 2 fori", "sfera", "diametro=30;fori=0" },

        // Forma dedotta o scritta in altro modo
        { "100x60x3", "blocco", "larghezza=100;profondita=60;altezza=3" },
        { "base 100x60 spessore 3", "piastra", "larghezza=100;profondita=60;altezza=3" },
        { "palla blu da 5 cm", "sfera", "diametro=50" },
        { "cono alto 30 diametro 20 a punta", "cono", "diametro=20;diametro_cima=0;altezza=30" },
        { "blocco alto 10 alto 20", "blocco", "altezza=10" },
    };

    [Theory]
    [MemberData(nameof(Frasi))]
    public void RiconosceFormaEMisure(string testo, string forma, string misure)
    {
        var r = RiconoscitoreIa3d.Riconosci(testo);

        Assert.Equal(forma, r.Forma);
        foreach (var coppia in misure.Split(';'))
        {
            var (nome, atteso) = (coppia.Split('=')[0], coppia.Split('=')[1]);
            var trovato = r.Parametri[nome];
            if (trovato is decimal d)
                Assert.True(d == decimal.Parse(atteso, System.Globalization.CultureInfo.InvariantCulture),
                    $"{nome}: {d} invece di {atteso}");
            else
                Assert.Equal(atteso, trovato);
        }
    }

    [Theory]
    [MemberData(nameof(Frasi))]
    public void LeMisureRiconosciutePassanoIControlli(string testo, string forma, string misure)
    {
        _ = forma;
        _ = misure;
        var r = RiconoscitoreIa3d.Riconosci(testo);
        var (_, pezzo, errori) = FormeBaseIa3d.Valida(Utili.InJson(r.Parametri));
        Assert.True(pezzo != null, string.Join(" ", errori));
    }

    [Fact]
    public void SenzaFormaNonRiconosceNulla()
    {
        var r = RiconoscitoreIa3d.Riconosci("ciao come stai");
        Assert.Null(r.Forma);
        Assert.Contains("ciao", r.NonCapito);
    }

    [Theory]
    [InlineData("100x60x3", "Non hai scritto la forma")]
    [InlineData("cubo e cilindro", "una forma alla volta")]
    [InlineData("piastra 50x50 con 2 fori al centro", "solo foro")]
    [InlineData("sfera 30 con 2 fori", "solo su blocco")]
    [InlineData("blocco alto 10 alto 20", "non usato")]
    [InlineData("piastra 100x60", "valore predefinito: spessore 3 mm")]
    public void SpiegaCosaHaFatto(string testo, string avviso)
    {
        var r = RiconoscitoreIa3d.Riconosci(testo);
        Assert.Contains(r.Avvisi, a => a.Contains(avviso));
    }

    [Fact]
    public void ElencaLeParoleNonCapite()
    {
        var r = RiconoscitoreIa3d.Riconosci("palla blu da 5 cm");
        Assert.Equal(new[] { "blu" }, r.NonCapito);
    }

    [Fact]
    public void MostraIlTestoScrittoDallUtente()
    {
        // Le parti capite riportano il testo originale (maiuscole e accenti compresi).
        var r = RiconoscitoreIa3d.Riconosci("Profondità 30, larghezza 70, altezza 15: un blocchetto");
        Assert.Contains(r.Capito, p => p.Testo == "Profondità 30");
    }

    [Fact]
    public void SegnalaIValoriPredefiniti()
    {
        var r = RiconoscitoreIa3d.Riconosci("piastra 100x60 con fori");
        Assert.Contains(r.Misure, m => m.Nome == "altezza" && m.Predefinita);
        Assert.Contains(r.Misure, m => m.Nome == "larghezza" && !m.Predefinita);
        Assert.Contains(r.Avvisi, a => a.Contains("diametro dei fori Ø 4 mm"));
    }

    [Theory]
    [InlineData("piastra 300x60", "Larghezza: deve essere tra 1 e 250.")]
    [InlineData("piastra 40x40 con 4 fori da 15 agli angoli", "I fori non ci stanno")]
    [InlineData("tubo diametro 10 parete 6", "diametro interno")]
    [InlineData("cilindro diametro 12345678901234567890123456789012345678901234567890", "Diametro: deve essere tra 1 e 250.")]
    public void MisureImpossibiliVengonoRifiutate(string testo, string errore)
    {
        var r = RiconoscitoreIa3d.Riconosci(testo);
        var (_, pezzo, errori) = FormeBaseIa3d.Valida(Utili.InJson(r.Parametri));
        Assert.Null(pezzo);
        Assert.Contains(errori, e => e.Contains(errore));
    }

    [Fact]
    public void TestoLungoESenzaSensoNonBlocca()
    {
        var testo = string.Concat(Enumerable.Repeat("x9 fori per da di ", 20))[..RiconoscitoreIa3d.LunghezzaMassima];
        var r = RiconoscitoreIa3d.Riconosci(testo);
        Assert.NotNull(r);
    }
}
