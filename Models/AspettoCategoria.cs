namespace GestionalePersone.Models;

// Icone e colori ammessi per le categorie (scelti dall'amministratore).
public static class AspettoCategoria
{
    // Nomi delle icone di Bootstrap Icons (classe "bi-<nome>").
    public static readonly string[] Icone =
    [
        "house-door", "cart3", "basket", "cup-hot", "lightning-charge", "droplet",
        "wifi", "phone", "car-front", "fuel-pump", "bus-front", "airplane",
        "bicycle", "heart-pulse", "controller", "film", "gift", "bag",
        "mortarboard", "briefcase", "tools", "piggy-bank", "credit-card", "tag",
    ];

    // Tavolozza verificata per il daltonismo (la stessa dei grafici) più un grigio.
    public static readonly string[] Colori =
    [
        "#2a78d6", "#eb6834", "#1baf7a", "#eda100", "#e87ba4", "#008300", "#6250d6", "#e34948", "#6c757d",
    ];

    public const string IconaPredefinita = "tag";
    public const string ColorePredefinito = "#6c757d";
}
