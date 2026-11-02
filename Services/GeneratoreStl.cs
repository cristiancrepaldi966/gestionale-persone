using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace GestionalePersone.Services;

// Genera il file STL di un oggetto del catalogo eseguendo OpenSCAD con i parametri già
// controllati (CatalogoStampa3d.Valida). Al massimo due generazioni insieme, tempo
// massimo 60 secondi; i risultati recenti restano in memoria (anteprima e download
// dello stesso oggetto non lo rigenerano).
public class GeneratoreStl
{
    public class OpenScadNonDisponibile(string messaggio) : Exception(messaggio);

    public class GenerazioneFallita(string messaggio) : Exception(messaggio);

    private static readonly TimeSpan TempoMassimo = TimeSpan.FromSeconds(60);
    private static readonly SemaphoreSlim Limite = new(2);

    private readonly string _eseguibile;
    private readonly string _cartellaModelli;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GeneratoreStl> _log;

    public GeneratoreStl(IConfiguration config, IMemoryCache cache, ILogger<GeneratoreStl> log)
    {
        _eseguibile = config["OPENSCAD_PATH"] is { Length: > 0 } percorso ? percorso : "openscad";
        _cartellaModelli = Path.Combine(AppContext.BaseDirectory, "Modelli3d");
        _cache = cache;
        _log = log;
    }

    public async Task<byte[]> Genera(CatalogoStampa3d.Oggetto oggetto, IReadOnlyDictionary<string, string> valori,
        CancellationToken annulla)
    {
        var modello = Path.Combine(_cartellaModelli, oggetto.FileScad);
        if (!File.Exists(modello))
            throw new GenerazioneFallita("Modello non trovato sul server.");

        // La data del file .scad fa parte della chiave: se il modello cambia, si rigenera.
        var chiave = $"stl:{oggetto.Codice}:{File.GetLastWriteTimeUtc(modello).Ticks}:{Impronta(valori)}";
        if (_cache.TryGetValue(chiave, out byte[]? pronto) && pronto != null)
            return pronto;

        await Limite.WaitAsync(annulla);
        var uscita = Path.Combine(Path.GetTempPath(), $"stampa3d-{Guid.NewGuid():N}.stl");
        try
        {
            var avvio = new ProcessStartInfo(_eseguibile)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            // Argomenti passati uno per uno (nessuna shell): i valori sono già controllati.
            avvio.ArgumentList.Add("-o");
            avvio.ArgumentList.Add(uscita);
            avvio.ArgumentList.Add("--export-format");
            avvio.ArgumentList.Add("binstl");
            foreach (var (nome, valore) in valori)
            {
                avvio.ArgumentList.Add("-D");
                avvio.ArgumentList.Add($"{nome}={valore}");
            }
            avvio.ArgumentList.Add(modello);

            Process processo;
            try
            {
                processo = Process.Start(avvio) ?? throw new OpenScadNonDisponibile("OpenSCAD non si avvia.");
            }
            catch (System.ComponentModel.Win32Exception)
            {
                throw new OpenScadNonDisponibile("OpenSCAD non è installato sul server.");
            }

            using (processo)
            {
                using var tempo = CancellationTokenSource.CreateLinkedTokenSource(annulla);
                tempo.CancelAfter(TempoMassimo);
                var errori = processo.StandardError.ReadToEndAsync(tempo.Token);
                _ = processo.StandardOutput.ReadToEndAsync(tempo.Token);
                try
                {
                    await processo.WaitForExitAsync(tempo.Token);
                }
                catch (OperationCanceledException)
                {
                    try { processo.Kill(entireProcessTree: true); } catch { /* già terminato */ }
                    if (annulla.IsCancellationRequested) throw;
                    throw new GenerazioneFallita("La generazione ha richiesto troppo tempo: prova misure più semplici.");
                }

                if (processo.ExitCode != 0 || !File.Exists(uscita))
                {
                    _log.LogWarning("OpenSCAD {Oggetto} uscito con {Codice}: {Errori}", oggetto.Codice, processo.ExitCode, await errori);
                    throw new GenerazioneFallita("Non è stato possibile generare il modello con queste misure.");
                }
            }

            var stl = await File.ReadAllBytesAsync(uscita, annulla);
            _cache.Set(chiave, stl, new MemoryCacheEntryOptions
            {
                Size = stl.Length,
                SlidingExpiration = TimeSpan.FromMinutes(30),
            });
            return stl;
        }
        finally
        {
            Limite.Release();
            try { File.Delete(uscita); } catch { /* file temporaneo */ }
        }
    }

    private static string Impronta(IReadOnlyDictionary<string, string> valori)
    {
        var testo = string.Join("\n", valori.OrderBy(v => v.Key, StringComparer.Ordinal).Select(v => v.Key + "=" + v.Value));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(testo)));
    }
}
