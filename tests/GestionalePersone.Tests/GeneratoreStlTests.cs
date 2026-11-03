using GestionalePersone.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace GestionalePersone.Tests;

// File STL generati davvero con OpenSCAD (le prove si saltano se OpenSCAD non è installato).
public class GeneratoreStlTests
{
    private static readonly GeneratoreStl Generatore = new(new ConfigurationBuilder().Build(),
        new MemoryCache(new MemoryCacheOptions()), NullLogger<GeneratoreStl>.Instance);

    public static TheoryData<string> FrasiIa => new()
    {
        "piastra 100x60 spessore 4 con 4 fori da 5 agli angoli",
        "cilindro diametro 60 alto 10 con 6 fori da 4 in cerchio",
        "tubo diametro 20 interno 16 lungo 100",
        "cubo 20 con angoli arrotondati",
        "cono base 40 punta 10 alto 30",
        "sfera 4 cm",
        "piastra 120x30x3 con 4 fori per viti M3 in fila arrotondata",
    };

    [TheoryConOpenScad]
    [MemberData(nameof(FrasiIa))]
    public async Task StlDellaFormaBaseHaLIngombroAtteso(string testo)
    {
        var r = RiconoscitoreIa3d.Riconosci(testo);
        var (valori, pezzo, errori) = FormeBaseIa3d.Valida(Utili.InJson(r.Parametri));
        Assert.True(pezzo != null, string.Join(" ", errori));

        var stl = await Generatore.Genera(FormeBaseIa3d.Definizione, valori, CancellationToken.None);
        var (x, y, z) = Utili.IngombroStl(stl);
        // Sfere e cilindri sono poligoni: qualche decimo di tolleranza.
        Assert.InRange(x, (double)pezzo!.Ingombro.X - 0.3, (double)pezzo.Ingombro.X + 0.01);
        Assert.InRange(y, (double)pezzo.Ingombro.Y - 0.3, (double)pezzo.Ingombro.Y + 0.01);
        Assert.InRange(z, (double)pezzo.Ingombro.Z - 0.3, (double)pezzo.Ingombro.Z + 0.01);
    }

    public static TheoryData<string> OggettiCatalogo
    {
        get
        {
            var dati = new TheoryData<string>();
            foreach (var o in CatalogoStampa3d.Tutti) dati.Add(o.Codice);
            return dati;
        }
    }

    [TheoryConOpenScad]
    [MemberData(nameof(OggettiCatalogo))]
    public async Task OggettiDelCatalogoSiGeneranoConLeMisurePredefinite(string codice)
    {
        var oggetto = CatalogoStampa3d.Trova(codice)!;
        var (valori, _, errori) = CatalogoStampa3d.Valida(oggetto, null);
        Assert.Empty(errori);

        var stl = await Generatore.Genera(oggetto, valori, CancellationToken.None);
        Assert.True(stl.Length > 84, "STL vuoto");
    }
}

// Prova che richiede OpenSCAD (nel PATH o in OPENSCAD_PATH): senza, viene saltata.
public sealed class TheoryConOpenScadAttribute : TheoryAttribute
{
    public TheoryConOpenScadAttribute()
    {
        if (!Utili.OpenScadDisponibile) Skip = "OpenSCAD non è installato.";
    }
}
