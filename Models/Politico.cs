namespace GestionalePersone.Models;

// Un politico del modulo Fantapolitica, scelto dai giocatori per le loro squadre.
// L'elenco lo gestisce solo il Super admin.
public class Politico
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Cognome { get; set; } = string.Empty;

    // Ruolo in amministrazione (vedi RuoliPolitico).
    public string Ruolo { get; set; } = RuoliPolitico.Consigliere;

    // Partito o lista (facoltativo).
    public string? Partito { get; set; }
}

public static class RuoliPolitico
{
    public const string Sindaco = "Sindaco";
    public const string Vicesindaco = "Vicesindaco";
    public const string Assessore = "Assessore";
    public const string Consigliere = "Consigliere";

    // Nell'ordine in cui compaiono negli elenchi.
    public static readonly string[] Tutti = [Sindaco, Vicesindaco, Assessore, Consigliere];
}

// La squadra di un membro in una lega: un nome e Squadra.PoliticiPerSquadra politici.
// Una per persona in ogni lega; se la persona esce dalla lega la squadra viene eliminata.
public class Squadra
{
    // Quanti politici deve avere una squadra.
    public const int PoliticiPerSquadra = 5;

    public int Id { get; set; }

    public int LegaId { get; set; }

    public int UtenteId { get; set; }

    public Utente Utente { get; set; } = null!;

    public string Nome { get; set; } = string.Empty;

    public List<PoliticoSquadra> Politici { get; set; } = [];

    public DateTime CreatoIl { get; set; } = DateTime.UtcNow;

    public DateTime AggiornatoIl { get; set; } = DateTime.UtcNow;
}

// Un politico scelto in una squadra (lo stesso politico può stare in più squadre).
public class PoliticoSquadra
{
    public int SquadraId { get; set; }

    public Squadra Squadra { get; set; } = null!;

    public int PoliticoId { get; set; }

    public Politico Politico { get; set; } = null!;
}
