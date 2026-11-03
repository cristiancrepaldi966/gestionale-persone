using GestionalePersone.Services;

namespace GestionalePersone.Tests;

// Controlli delle misure e centri dei fori delle forme base (modulo IA).
public class FormeBaseIa3dTests
{
    private static FormeBaseIa3d.Pezzo Valido(params (string, object)[] parametri)
    {
        var (_, pezzo, errori) = FormeBaseIa3d.Valida(Utili.InJson(parametri.ToDictionary(p => p.Item1, p => p.Item2)));
        Assert.True(pezzo != null, string.Join(" ", errori));
        return pezzo!;
    }

    private static List<string> Errori(params (string, object)[] parametri) =>
        FormeBaseIa3d.Valida(Utili.InJson(parametri.ToDictionary(p => p.Item1, p => p.Item2))).Errori;

    [Fact]
    public void ValoriPredefinitiSonoValidi()
    {
        var pezzo = Valido();
        Assert.Equal(FormeBaseIa3d.Forme.Blocco, pezzo.Forma);
        Assert.Empty(pezzo.Fori);
    }

    [Fact]
    public void ForiAgliAngoliStannoAUnDiametroDalBordo()
    {
        var pezzo = Valido(("forma", "piastra"), ("larghezza", 100), ("profondita", 60),
            ("fori", 4), ("diametro_fori", 5), ("posizione_fori", "angoli"));
        Assert.Equal(new FormeBaseIa3d.Foro[] { new(-45, -25), new(45, -25), new(45, 25), new(-45, 25) }, pezzo.Fori);
    }

    [Fact]
    public void ConAngoliMoltoArrotondatiIlForoVaAlCentroDellaCurva()
    {
        var pezzo = Valido(("forma", "piastra"), ("larghezza", 100), ("profondita", 60), ("raggio_angoli", 10),
            ("fori", 4), ("diametro_fori", 5), ("posizione_fori", "angoli"));
        Assert.Contains(new FormeBaseIa3d.Foro(40, 20), pezzo.Fori);
    }

    [Fact]
    public void ForiInFilaSonoAllaStessaDistanza()
    {
        var pezzo = Valido(("forma", "piastra"), ("larghezza", 100), ("profondita", 50),
            ("fori", 3), ("diametro_fori", 4), ("posizione_fori", "fila"));
        Assert.Equal(new FormeBaseIa3d.Foro[] { new(-46, 0), new(0, 0), new(46, 0) }, pezzo.Fori);
    }

    [Fact]
    public void ForiInCerchioPartonoDallAlto()
    {
        var pezzo = Valido(("forma", "cilindro"), ("diametro", 60), ("altezza", 10),
            ("fori", 6), ("diametro_fori", 4), ("posizione_fori", "cerchio"));
        Assert.Equal(6, pezzo.Fori.Length);
        Assert.Equal(new FormeBaseIa3d.Foro(0, 26), pezzo.Fori[0]);
        Assert.All(pezzo.Fori, f => Assert.InRange(Math.Sqrt((double)(f.X * f.X + f.Y * f.Y)), 25.98, 26.02));
    }

    [Theory]
    [InlineData("angoli", 3, "Agli angoli vanno 4 fori.")]
    [InlineData("centro", 2, "Al centro va un solo foro")]
    [InlineData("cerchio", 4, "Sul blocco e sulla piastra")]
    public void PosizioniNonValideSulRettangolo(string posizione, int fori, string errore)
    {
        Assert.Contains(Errori(("forma", "piastra"), ("fori", fori), ("posizione_fori", posizione)), e => e.Contains(errore));
    }

    [Fact]
    public void ForiSoloSuBloccoPiastraECilindro()
    {
        Assert.Contains(Errori(("forma", "sfera"), ("fori", 1)), e => e.Contains("solo su blocco, piastra e cilindro"));
    }

    [Fact]
    public void NumeroDiForiIntero()
    {
        Assert.Contains(Errori(("forma", "piastra"), ("fori", 2.5)), e => e.Contains("numero intero"));
    }

    [Fact]
    public void RaggioDegliAngoliAlMassimoMetaDelLatoCorto()
    {
        Assert.Contains(Errori(("forma", "blocco"), ("larghezza", 40), ("profondita", 20), ("raggio_angoli", 11)),
            e => e.Contains("raggio degli angoli"));
    }

    [Fact]
    public void TuboConParetePiuSottileDelMinimo()
    {
        Assert.Contains(Errori(("forma", "tubo"), ("diametro", 10), ("diametro_interno", 9.5)),
            e => e.Contains("almeno 1,6 mm"));
    }

    [Fact]
    public void ICentriDeiForiNonSiPossonoInviare()
    {
        // Li calcola il server: un valore inviato dall'esterno è un parametro sconosciuto.
        Assert.Contains(Errori(("posizioni_fori", "[[1,1]]")), e => e.Contains("Parametro sconosciuto"));
    }

    [Fact]
    public void ValoriNonNumericiVengonoRifiutati()
    {
        Assert.Contains(Errori(("larghezza", "<script>")), e => e.Contains("inserisci un numero"));
    }

    [Theory]
    [InlineData("blocco", 40, 20, 10)]
    [InlineData("cilindro", 20, 20, 10)]
    [InlineData("sfera", 20, 20, 20)]
    public void IngombroDellaForma(string forma, decimal x, decimal y, decimal z)
    {
        var pezzo = Valido(("forma", forma));
        Assert.Equal(new FormeBaseIa3d.Ingombro(x, y, z), pezzo.Ingombro);
    }
}
