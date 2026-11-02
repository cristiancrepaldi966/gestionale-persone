using GestionalePersone.Models;

namespace GestionalePersone.Services;

// Ricerca di mercato di ogni modulo dell'app: idea, clienti, dimensione del mercato,
// concorrenti, posizionamento, piano di evoluzione, canali per trovare clienti e ipotesi
// della simulazione. I numeri sono STIME (vedi le fonti di ogni scheda) da rivedere man
// mano che arrivano dati veri; le ipotesi della simulazione si cambiano dalla pagina.
//
// Per analizzare un nuovo modulo basta aggiungere qui la sua scheda.
public static class SchedeMercato
{
    public record Segmento(string Nome, long Persone, string Descrizione);

    // Mercato totale (TAM), raggiungibile (SAM) e obiettivo realistico a 3 anni (SOM).
    public record Dimensione(long Persone, string Descrizione);

    public record Mercato(Dimensione Totale, Dimensione Raggiungibile, Dimensione Obiettivo);

    // Punteggio da 1 a 5 su un aspetto che conta per i clienti: il modulo e la media dei concorrenti.
    public record Asse(string Nome, int Modulo, int Concorrenti);

    public record Concorrente(string Nome, string Tipo, string Prezzo, string PuntiForza, string PuntiDeboli);

    public record Swot(string[] Forze, string[] Debolezze, string[] Opportunita, string[] Minacce);

    public record Fase(string Nome, int DalMese, int AlMese, string Obiettivo, string[] Attivita,
        int Ore, decimal Costo, int UtentiObiettivo);

    public record Canale(string Nome, string Descrizione, decimal CostoPerUtente, string Quando);

    public record Fonte(string Titolo, string Url);

    public record Scheda(
        string Modulo,
        string Idea,
        string Settore,
        string ModelloRicavi,
        string Posizionamento,
        Segmento[] Segmenti,
        Mercato Mercato,
        Asse[] Assi,
        Concorrente[] Concorrenti,
        Swot Swot,
        Fase[] Fasi,
        Canale[] Canali,
        string[] ProssimiPassi,
        ParametriSimulazione Predefiniti,
        Fonte[] Fonti);

    public static Scheda? Trova(string? modulo) => Tutte.FirstOrDefault(s => s.Modulo == modulo);

    public static readonly Scheda[] Tutte =
    [
        // -----------------------------------------------------------------------------
        // Gestione finanze
        // -----------------------------------------------------------------------------
        new(
            Modulo: Moduli.Finanze,
            Idea: "Le spese di casa condivise in famiglia, in coppia o tra coinquilini: ognuno inserisce " +
                  "le uscite in pochi secondi, tutti vedono categorie e grafici. Niente collegamento alla banca, " +
                  "i dati restano a chi li inserisce.",
            Settore: "Finanza personale · app per budget e spese condivise",
            ModelloRicavi: "Freemium per nucleo: gratis per le funzioni di base, Premium a pochi euro al mese " +
                           "per budget mensili, spese ricorrenti, foto degli scontrini ed esportazione. Niente pubblicità " +
                           "(la privacy è il punto di forza).",
            Posizionamento: "Tra Splitwise (divide i debiti tra amici) e le app di budget personali (pensate per " +
                            "una persona sola, spesso collegate alla banca): l'unica pensata per il bilancio COMUNE " +
                            "della casa, semplice come un foglio condiviso ma con grafici veri.",
            Segmenti:
            [
                new("Coppie e famiglie che dividono le spese", 9_000_000,
                    "Nuclei con almeno due adulti che gestiscono insieme la spesa di casa. Decide di solito " +
                    "chi tiene i conti: va convinta una persona per portare tutto il nucleo."),
                new("Coinquilini e studenti fuori sede", 900_000,
                    "Bollette, spesa e affitto da dividere ogni mese; molto attivi sui social, cambiano " +
                    "casa spesso (abbandono più alto)."),
                new("Piccoli gruppi e associazioni", 360_000,
                    "Associazioni, squadre amatoriali, gruppi di acquisto: hanno una cassa comune da " +
                    "rendicontare e oggi usano fogli di calcolo."),
            ],
            Mercato: new(
                new(19_000_000, "Italiani online che usano (15%) o vorrebbero provare (28%) un'app per " +
                                "tracciare le spese, su circa 44 milioni di utenti internet."),
                new(5_500_000, "Di questi, chi gestisce spese condivise con altre persone (circa 3 su 10)."),
                new(30_000, "Obiettivo a 3 anni: 0,5% del mercato raggiungibile, cioè circa 10.000 nuclei " +
                            "da 3 persone.")),
            Assi:
            [
                new("Semplicità", 5, 3),
                new("Spese condivise", 4, 3),
                new("Privacy dei dati", 5, 2),
                new("Automazione (banca, scontrini)", 1, 4),
                new("Prezzo", 5, 3),
            ],
            Concorrenti:
            [
                new("Splitwise", "Divisione spese tra amici", "Gratis; Pro qualche € al mese",
                    "Molto conosciuta, ottima per i viaggi e i debiti tra amici",
                    "Pensata per chi deve a chi, non per il bilancio della casa; limiti nella versione gratuita"),
                new("App di budget personale (Money Manager, Wallet...)", "Budget personale", "Freemium, 3–6 € al mese",
                    "Tante funzioni, statistiche dettagliate, alcune collegate alla banca",
                    "Una persona sola; complesse; collegare la banca spaventa molti utenti"),
                new("App della banca (Intesa, Revolut, N26...)", "Funzione inclusa nel conto", "Gratis con il conto",
                    "Categorizzazione automatica dei movimenti",
                    "Vedono solo un conto; i contanti e le spese degli altri restano fuori"),
                new("Foglio Excel / Google Sheets", "Fai da te", "Gratis",
                    "Flessibile, già conosciuto, condivisibile",
                    "Scomodo da telefono, niente grafici pronti, si abbandona dopo poche settimane"),
            ],
            Swot: new(
                Forze:
                [
                    "Già funzionante: dashboard, categorie con icone e colori, utenti e permessi",
                    "Semplice da usare da telefono",
                    "Nessun collegamento alla banca: zero timori sulla privacy",
                ],
                Debolezze:
                [
                    "Oggi tutte le uscite sono condivise tra tutti gli utenti: serve separare i nuclei (gruppi)",
                    "Nessuna registrazione autonoma: gli utenti li crea l'amministratore",
                    "Niente app negli store né funzionamento offline",
                ],
                Opportunita:
                [
                    "Il 28% degli italiani vorrebbe provare un'app per le spese ma non lo fa ancora",
                    "Caro bollette e inflazione: tenere d'occhio le spese di casa è un tema sentito",
                    "Ogni nucleo invita da solo gli altri membri: crescita naturale con il passaparola",
                ],
                Minacce:
                [
                    "Le banche aggiungono gratis le statistiche di spesa nelle loro app",
                    "Gli utenti abbandonano le app di budget dopo poche settimane",
                    "Concorrenti con grandi budget pubblicitari",
                ]),
            Fasi:
            [
                new("Validazione", 1, 3,
                    "Capire se 15–20 famiglie la usano ogni settimana per 2 mesi",
                    [
                        "Far provare l'app a parenti, amici e colleghi (2–3 persone per nucleo)",
                        "10 interviste: cosa usano oggi, cosa manca, quanto pagherebbero",
                        "Misurare quanti inseriscono almeno una spesa a settimana",
                    ],
                    Ore: 120, Costo: 150, UtentiObiettivo: 50),
                new("Prodotto per il pubblico", 4, 6,
                    "Permettere a chiunque di iscriversi e creare il proprio nucleo",
                    [
                        "Nuclei separati: ogni famiglia vede solo le sue uscite",
                        "Registrazione autonoma e invito dei membri via link",
                        "App installabile dal browser (PWA) ed esportazione CSV",
                        "Privacy policy e condizioni d'uso (GDPR)",
                    ],
                    Ore: 260, Costo: 900, UtentiObiettivo: 300),
                new("Lancio", 7, 12,
                    "Primi 3.000 utenti e primi abbonati Premium",
                    [
                        "Sito di presentazione e articoli \"come gestire le spese di casa\" (SEO)",
                        "Presenza nei gruppi Facebook e Reddit su risparmio e vita da coinquilini",
                        "Premium: budget mensili, spese ricorrenti, foto degli scontrini",
                        "Prime campagne a pagamento su Instagram con budget basso, per misurare il costo per utente",
                    ],
                    Ore: 300, Costo: 4_500, UtentiObiettivo: 3_000),
                new("Crescita", 13, 24,
                    "Arrivare a 15.000 utenti con un costo per utente sotto i 5 €",
                    [
                        "App su App Store e Google Play",
                        "Programma \"invita un amico\" (un mese di Premium gratis)",
                        "Collaborazioni con blog e creator di risparmio e finanza personale",
                        "Versione per associazioni con rendiconto annuale",
                    ],
                    Ore: 520, Costo: 14_000, UtentiObiettivo: 15_000),
            ],
            Canali:
            [
                new("Inviti nel nucleo", "Chi si iscrive invita partner e coinquilini: ogni nuovo nucleo vale 2–3 utenti.",
                    0, "Da subito"),
                new("Contenuti e ricerca su Google (SEO)", "Guide su bollette, budget familiare, divisione delle spese tra coinquilini.",
                    1.5m, "Dal lancio; rende dopo 4–6 mesi"),
                new("Community online", "Gruppi Facebook, Reddit, forum su risparmio e vita da fuori sede: tempo, non soldi.",
                    1, "Dalla validazione"),
                new("Creator di finanza personale", "Una storia o un video con codice Premium dedicato.",
                    3, "Dalla crescita"),
                new("Pubblicità Instagram / TikTok", "Brevi video \"quanto spendiamo davvero in casa\"; utile per misurare.",
                    6, "Dal lancio, con budget basso"),
                new("Google Ads su ricerche specifiche", "\"app spese condivise\", \"dividere bollette coinquilini\".",
                    8, "Solo se il Premium converte bene"),
            ],
            ProssimiPassi:
            [
                "Separare le uscite per nucleo: è il requisito per aprire l'app ad altre famiglie",
                "Fissare l'obiettivo di validazione: 15 nuclei attivi ogni settimana per 2 mesi",
                "Preparare 5 domande per le interviste e farle prima di scrivere nuove funzioni",
            ],
            Predefiniti: new()
            {
                UtentiIniziali = 10,
                PrezzoMensile = 2.99m,
                QuotaPaganti = 5,
                RicavoPubblicitario = 0,
                BudgetMarketing = 400,
                CostoPerUtente = 5,
                CrescitaOrganica = 9,
                Abbandono = 4,
                OreSettimanali = 8,
                CostoOrario = 25,
                CostiFissi = 30,
                Mesi = 24,
            },
            Fonti:
            [
                new("Revolut: il 15% degli italiani usa app per tracciare le spese, il 28% vorrebbe provarle",
                    "https://www.corrierecomunicazioni.it/?p=228331"),
                new("Klarna: per il 58% degli italiani il denaro è fonte di stress, due su tre usano app",
                    "https://www.punto-informatico.it/italiani-denaro-rapporto-klarna/"),
                new("Statista: uso delle app per spendere e trasferire denaro in Italia",
                    "https://es.statista.com/statistics/1061233/impact-of-mobile-apps-for-spending-or-transferring-money-in-italy/"),
            ]),

        // -----------------------------------------------------------------------------
        // Fantapolitica
        // -----------------------------------------------------------------------------
        new(
            Modulo: Moduli.Fantapolitica,
            Idea: "Un fanta-gioco sulla politica italiana: si crea una squadra di politici e si fanno punti " +
                  "con quello che succede davvero (voti, presenze, dichiarazioni, risultati elettorali), " +
                  "sfidando gli amici in leghe private.",
            Settore: "Intrattenimento · fanta-giochi e social game",
            ModelloRicavi: "Gratis per giocare. Ricavi da leghe Premium (statistiche, bonus personalizzati, nessun " +
                           "limite di partecipanti), sponsor della stagione e collaborazioni con testate e podcast.",
            Posizionamento: "Il modello del FantaSanremo applicato alla politica, ma tutto l'anno e con le stagioni " +
                            "legate alle elezioni; ironico e neutrale, senza schierarsi.",
            Segmenti:
            [
                new("Giocatori di fanta-giochi", 4_000_000,
                    "Chi gioca a FantaSanremo e Fantacalcio: conosce già le regole del genere e gioca in lega con gli amici."),
                new("Appassionati di politica e attualità", 2_500_000,
                    "Seguono talk show, podcast e social politici; cercano un modo divertente di commentare."),
                new("Studenti e giovani adulti 18–35", 1_500_000,
                    "Molto attivi su TikTok e Instagram, dove i meme politici girano velocemente."),
            ],
            Mercato: new(
                new(5_000_000, "Italiani che giocano ad almeno un fanta-gioco: il FantaSanremo da solo ha avuto " +
                               "circa 2,7–2,8 milioni di utenti e oltre 4 milioni di squadre."),
                new(800_000, "Di questi, chi segue con interesse la politica (circa 1 su 6)."),
                new(25_000, "Obiettivo a 3 anni: 3% del mercato raggiungibile, con il picco nella stagione delle " +
                            "elezioni politiche.")),
            Assi:
            [
                new("Divertimento e ironia", 4, 4),
                new("Gioco tutto l'anno", 4, 2),
                new("Neutralità politica", 5, 3),
                new("Aggiornamento dei punti", 2, 4),
                new("Notorietà", 1, 5),
            ],
            Concorrenti:
            [
                new("FantaSanremo", "Fanta-gioco a evento", "Gratis",
                    "Enorme notorietà, leghe tra amici, gioco diventato fenomeno di costume",
                    "Dura una settimana all'anno; non riguarda la politica (compete per l'attenzione)"),
                new("Fantacitorio e giochi legati a programmi TV", "Fanta-gioco politico", "Gratis",
                    "Ha dimostrato che la politica come fanta-gioco funziona",
                    "Legato a un programma e alla sua stagione, non è un prodotto indipendente"),
                new("Fantacalcio (app e leghe)", "Fanta-gioco sportivo", "Gratis / Premium",
                    "Milioni di giocatori abituali, app mature",
                    "Occupa il tempo dei giocatori da settembre a maggio"),
                new("Quiz e sondaggi politici online", "Attualità interattiva", "Gratis",
                    "Molto condivisi sui social",
                    "Contenuti usa e getta, nessuna sfida continua tra amici"),
            ],
            Swot: new(
                Forze:
                [
                    "Modulo già avviato dentro un'app con utenti, permessi e pagine di gestione",
                    "Idea immediata da spiegare: \"il FantaSanremo della politica\"",
                    "Le leghe tra amici fanno crescere il gioco da sole (8–10 persone per lega)",
                ],
                Debolezze:
                [
                    "Assegnare i punti a mano richiede molte ore ogni settimana",
                    "Gioco e regolamento ancora da costruire",
                    "Nessuna notorietà iniziale",
                ],
                Opportunita:
                [
                    "Elezioni politiche previste entro il 2027: la stagione perfetta per il lancio",
                    "Dati aperti della Camera e del Senato (votazioni, presenze) per calcolare punti in automatico",
                    "Podcast e creator di politica cercano format da proporre al loro pubblico",
                ],
                Minacce:
                [
                    "Rischio di essere percepiti come schierati o di pubblicare malus offensivi su persone reali",
                    "Interesse a picchi: tra un'elezione e l'altra molti smettono di giocare",
                    "Un media grande potrebbe lanciare un gioco simile con molta più visibilità",
                ]),
            Fasi:
            [
                new("Gioco minimo", 1, 3,
                    "Una lega tra amici che gioca davvero per 4 settimane",
                    [
                        "Regolamento: rosa, capitano, bonus e malus (solo fatti verificabili, mai offese)",
                        "Elenco dei politici e pannello per inserire gli eventi e i punti",
                        "Classifica della lega e della settimana",
                    ],
                    Ore: 200, Costo: 100, UtentiObiettivo: 100),
                new("Stagione pilota", 4, 8,
                    "Una stagione completa su un evento (es. elezioni regionali) con 2.000 giocatori",
                    [
                        "Leghe private con codice di invito",
                        "Profili Instagram e TikTok con i punti della settimana in video",
                        "Primi contatti con 5–10 podcast e pagine di satira politica",
                    ],
                    Ore: 320, Costo: 1_500, UtentiObiettivo: 2_000),
                new("Stagione delle elezioni politiche", 9, 18,
                    "Diventare il fanta-gioco di riferimento della campagna elettorale",
                    [
                        "Punti calcolati in automatico dai dati aperti del Parlamento",
                        "Leghe Premium e uno sponsor per la stagione",
                        "Collaborazione con una testata o un podcast come \"lega ufficiale\"",
                        "Moderazione e regole chiare su nomi e contenuti",
                    ],
                    Ore: 650, Costo: 12_000, UtentiObiettivo: 25_000),
                new("Gioco tutto l'anno", 19, 24,
                    "Tenere i giocatori tra un'elezione e l'altra",
                    [
                        "Stagioni brevi a tema (legge di bilancio, europee, comunali)",
                        "Classifiche per regione e per scuola o università",
                        "Statistiche Premium sui politici",
                    ],
                    Ore: 360, Costo: 6_000, UtentiObiettivo: 20_000),
            ],
            Canali:
            [
                new("Leghe tra amici", "Chi crea una lega invita in media 8–10 persone: è il motore del gioco.",
                    0, "Da subito"),
                new("Video brevi su TikTok e Instagram", "I punti della settimana raccontati con ironia: costa tempo, non soldi.",
                    0.5m, "Dalla stagione pilota"),
                new("Podcast e creator di politica", "Una lega ufficiale con il pubblico del podcast.",
                    1.5m, "Dalla stagione pilota"),
                new("Stampa e comunicati", "Classifiche curiose durante la campagna elettorale: notizia facile per le testate.",
                    0.3m, "Stagione elettorale"),
                new("Pubblicità Instagram / TikTok", "Solo nelle settimane calde (dibattiti, giorno del voto).",
                    2.5m, "Stagione elettorale"),
            ],
            ProssimiPassi:
            [
                "Scrivere il regolamento in una pagina e farlo provare a una lega di amici",
                "Scegliere l'evento della stagione pilota e la data di partenza",
                "Verificare quali dati aperti di Camera e Senato servono per i punti automatici",
            ],
            Predefiniti: new()
            {
                UtentiIniziali = 30,
                PrezzoMensile = 1.99m,
                QuotaPaganti = 3,
                RicavoPubblicitario = 0.08m,
                BudgetMarketing = 500,
                CostoPerUtente = 2.5m,
                CrescitaOrganica = 12,
                Abbandono = 9,
                OreSettimanali = 10,
                CostoOrario = 25,
                CostiFissi = 40,
                Mesi = 24,
            },
            Fonti:
            [
                new("FantaSanremo 2025: oltre 4 milioni di squadre iscritte (askanews / Libero)",
                    "https://www.libero.it/tv/torna-il-fantasanremo-con-oltre-4-milioni-di-squadre-iscritte_bc6368638959112"),
                new("FantaSanremo: utenti, squadre e leghe record (il Resto del Carlino)",
                    "https://www.ilrestodelcarlino.it/cultura/fantasanremo-record-iscritti-qbkt6gvq"),
                new("Fantacalcio: un mercato da circa 40 milioni di euro (Calcio e Finanza)",
                    "https://www.calcioefinanza.it/?p=216996"),
                new("Dati aperti della Camera dei deputati", "https://dati.camera.it/"),
            ]),
    ];
}
