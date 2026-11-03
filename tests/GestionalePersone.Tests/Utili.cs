using System.Diagnostics;
using System.Text.Json;

namespace GestionalePersone.Tests;

internal static class Utili
{
    // I parametri come arrivano al server in una richiesta JSON.
    public static Dictionary<string, JsonElement> InJson(Dictionary<string, object> parametri) =>
        JsonSerializer.SerializeToElement(parametri).EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

    // Ingombro (x, y, z) di un file STL binario.
    public static (double X, double Y, double Z) IngombroStl(byte[] stl)
    {
        var triangoli = BitConverter.ToUInt32(stl, 80);
        double[] min = [double.MaxValue, double.MaxValue, double.MaxValue];
        double[] max = [double.MinValue, double.MinValue, double.MinValue];
        for (var t = 0; t < triangoli; t++)
            for (var v = 0; v < 3; v++)
                for (var asse = 0; asse < 3; asse++)
                {
                    var c = BitConverter.ToSingle(stl, 84 + t * 50 + 12 + v * 12 + asse * 4);
                    min[asse] = Math.Min(min[asse], c);
                    max[asse] = Math.Max(max[asse], c);
                }
        return (max[0] - min[0], max[1] - min[1], max[2] - min[2]);
    }

    public static readonly bool OpenScadDisponibile = Prova();

    private static bool Prova()
    {
        try
        {
            var percorso = Environment.GetEnvironmentVariable("OPENSCAD_PATH") is { Length: > 0 } p ? p : "openscad";
            using var processo = Process.Start(new ProcessStartInfo(percorso, "--version")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            });
            processo?.WaitForExit(10_000);
            return processo is { ExitCode: 0 };
        }
        catch
        {
            return false;
        }
    }
}
