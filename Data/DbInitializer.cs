using MaRa.Api.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MaRa.Api.Data;

/// <summary>
/// Données initiales pour le prototype MA-RA CAMRAIL.
/// Idempotent : vérifie l'existence avant toute insertion.
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db      = services.GetRequiredService<AppDbContext>();
        var userMgr = services.GetRequiredService<UserManager<ApplicationUser>>();
        var log     = services.GetRequiredService<ILogger<AppDbContext>>();

        await SeedUsersAsync(userMgr, log);

        if (!await db.Equipements.AnyAsync())
        {
            await SeedEquipementsAsync(db, log);
            await SeedGammesAsync(db, log);
        }
        else
        {
            log.LogInformation("Données référentiel déjà présentes  seed ignoré");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UTILISATEURS
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> mgr, ILogger log)
    {
        var usersToCreate = new[]
        {
            ("admin",        "admin@camrail.cm",        "Admin@Camrail2025!", "ADMIN"),
            ("tech.dmat",    "tech.dmat@camrail.cm",    "Tech@Dmat2025!",     "DMAT"),
            ("chef.dmat",    "chef.dmat@camrail.cm",    "Chef@Dmat2025!",     "DMAT"),
            ("tech.dif",     "tech.dif@camrail.cm",     "Tech@Dif2025!",      "DIF"),
            ("chef.dif",     "chef.dif@camrail.cm",     "Chef@Dif2025!",      "DIF"),
            ("coordinateur", "coord.info@camrail.cm",   "Coord@CI2025!",      "CI"),
            ("formateur",    "formateur@camrail.cm",    "Form@CIF2025!",      "CIF"),
        };

        foreach (var (username, email, password, groupe) in usersToCreate)
        {
            if (await mgr.FindByNameAsync(username) is not null)
                continue;

            var user = new ApplicationUser
            {
                UserName  = username,
                Email     = email,
                Groupe    = groupe,
                IsActive  = true,
                CreatedAt = DateTime.UtcNow,
            };

            var result = await mgr.CreateAsync(user, password);
            if (result.Succeeded)
                log.LogInformation("Utilisateur créé : {User} [{Groupe}]", username, groupe);
            else
                log.LogWarning("Échec création {User} : {Errors}", username,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ÉQUIPEMENTS  (12 classes IA  DMAT + DIF)
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task SeedEquipementsAsync(AppDbContext db, ILogger log)
    {
        var equipements = new List<Equipement>
        {
            // ── DMAT ─────────────────────────────────────────────────────────
            new() {
                Id = Guid.Parse("11000000-0000-0000-0000-000000000001"),
                Code = "LOC-001", Direction = "DMAT", Classe = "moteur_traction",
                Designation = "Moteur de traction BB-1300 CAMRAIL",
                Marqueur    = "VUF_MTR_001",
                Statut      = "actif",
                Description = "Moteur de traction principal des locomotives série BB-1300. " +
                              "Puissance : 1 350 kW. Révision : tous les 120 000 km.",
            },
            new() {
                Id = Guid.Parse("11000000-0000-0000-0000-000000000002"),
                Code = "BOG-001", Direction = "DMAT", Classe = "bogie_essieux",
                Designation = "Bogie à essieux diesel  série Y25",
                Marqueur    = "VUF_BOG_001",
                Statut      = "actif",
                Description = "Bogie à 2 essieux porteurs. Charge max. : 22,5 t/essieu. " +
                              "Révision complète : 60 000 km.",
            },
            new() {
                Id = Guid.Parse("11000000-0000-0000-0000-000000000003"),
                Code = "COMP-001", Direction = "DMAT", Classe = "compresseur",
                Designation = "Compresseur ATS à pistons  25 bar",
                Marqueur    = "VUF_COMP_001",
                Statut      = "actif",
                Description = "Compresseur air frein 2 étages. Pression service : 7-9 bar. " +
                              "Vérification filtre : 30 000 km.",
            },
            new() {
                Id = Guid.Parse("11000000-0000-0000-0000-000000000004"),
                Code = "POMP-001", Direction = "DMAT", Classe = "pompe_eau",
                Designation = "Pompe à eau de refroidissement moteur diesel",
                Marqueur    = "VUF_POMP_001",
                Statut      = "actif",
                Description = "Pompe centrifuge circuit eau moteur. Débit : 600 L/min. " +
                              "Contrôle garniture : 20 000 km.",
            },
            new() {
                Id = Guid.Parse("11000000-0000-0000-0000-000000000005"),
                Code = "GRF-001", Direction = "DMAT", Classe = "grue_ferroviaire",
                Designation = "Grue ferroviaire automotrice GE-25T",
                Marqueur    = "VUF_GRF_001",
                Statut      = "actif",
                Description = "Grue ferroviaire à portée variable 25 t. Révision câbles : 6 mois. " +
                              "Levage interdire à la pluie.",
            },
            new() {
                Id = Guid.Parse("11000000-0000-0000-0000-000000000006"),
                Code = "PR-001", Direction = "DMAT", Classe = "pont_roulant",
                Designation = "Pont roulant atelier DMAT  10 tonnes",
                Marqueur    = "VUF_PR_001",
                Statut      = "actif",
                Description = "Pont roulant biportique atelier Yaoundé DMAT. CMU : 10 t. " +
                              "Vérification annuelle obligatoire par organisme agréé.",
            },
            new() {
                Id = Guid.Parse("11000000-0000-0000-0000-000000000007"),
                Code = "TRA-001", Direction = "DMAT", Classe = "tours_atelier",
                Designation = "Tour en fosse pour reprofilage des roues",
                Marqueur    = "VUF_TRA_001",
                Statut      = "actif",
                Description = "Tour en fosse Hegenscheidt  reprofilage sans démontage essieux. " +
                              "Précision : ±0,05 mm. Capacité : Ø 650 à 1 100 mm.",
            },
            new() {
                Id = Guid.Parse("11000000-0000-0000-0000-000000000008"),
                Code = "DRA-001", Direction = "DMAT", Classe = "draisine",
                Designation = "Draisine DMAT à moteur thermique",
                Marqueur    = "VUF_DRA_001",
                Statut      = "actif",
                Description = "Draisine légère à moteur diesel. Vitesse max : 80 km/h. " +
                              "Révision annuelle moteur et freins.",
            },
            // ── DIF ──────────────────────────────────────────────────────────
            new() {
                Id = Guid.Parse("22000000-0000-0000-0000-000000000001"),
                Code = "AIG-001", Direction = "DIF", Classe = "aiguillage",
                Designation = "Appareil de voie type 46 à commande électrique",
                Marqueur    = "VUF_AIG_001",
                Statut      = "actif",
                Description = "Aiguillage simple type 46, commande par moteur SH. " +
                              "Vitesse déviation : 60 km/h. Inspection mensuelle.",
            },
            new() {
                Id = Guid.Parse("22000000-0000-0000-0000-000000000002"),
                Code = "TRS-001", Direction = "DIF", Classe = "transformateur_ht",
                Designation = "Transformateur HT 225/15 kV  40 MVA",
                Marqueur    = "VUF_TRS_001",
                Statut      = "actif",
                Description = "Transformateur de puissance huile minérale. " +
                              "Ratio : 225/15 kV. Contrôle huile : annuel.",
            },
            new() {
                Id = Guid.Parse("22000000-0000-0000-0000-000000000003"),
                Code = "GE-001", Direction = "DIF", Classe = "groupe_electrogene",
                Designation = "Groupe électrogène de secours 500 kVA",
                Marqueur    = "VUF_GE_001",
                Statut      = "actif",
                Description = "GE diesel Caterpillar 500 kVA. Démarrage auto sur coupure. " +
                              "Essai mensuel à charge nominale.",
            },
            new() {
                Id = Guid.Parse("22000000-0000-0000-0000-000000000004"),
                Code = "SIG-001", Direction = "DIF", Classe = "signal_ferroviaire",
                Designation = "Signal lumineux à LED  entrée gare Yaoundé",
                Marqueur    = "VUF_SIG_001",
                Statut      = "actif",
                Description = "Signal carré lumineux technologie LED. Portée : 300 m. " +
                              "Vérification alignement optique : semestrielle.",
            },
        };

        db.Equipements.AddRange(equipements);
        await db.SaveChangesAsync();
        log.LogInformation("{Count} équipements créés", equipements.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GAMMES + ÉTAPES  (max 80 chars / étape  exigence RealWear)
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task SeedGammesAsync(AppDbContext db, ILogger log)
    {
        var gammes = new List<Gamme>
        {
            // ══ MOTEUR DE TRACTION ════════════════════════════════════════════
            new() {
                Id = NewId(1, 1), Code = "GMT-001",
                Titre = "Révision balais et collecteur  moteur traction",
                ClasseEquipement = "moteur_traction", Direction = "DMAT",
                EquipementId = Guid.Parse("11000000-0000-0000-0000-000000000001"),
                Version = 1, IsActive = true,
                Etapes = new List<Etape>
                {
                    Step(1, "Consigner l'engin  verrouiller le sectionneur principal",  "ROUGE"),
                    Step(2, "Vérifier absence de tension aux bornes du moteur",           "ORANGE"),
                    Step(3, "Déposer les capots d'accès aux porte-balais",               "NONE"),
                    Step(4, "Mesurer la longueur des balais  seuil mini : 25 mm",       "NONE"),
                    Step(5, "Contrôler état du collecteur  pas de rainures > 0,3 mm",   "NONE"),
                    Step(6, "Nettoyer collecteur à la toile abrasive P120",              "NONE"),
                    Step(7, "Remplacer balais usés  régler pression ressort à 0,4 N",  "ORANGE"),
                    Step(8, "Mesurer isolement enroulements  résistance > 1 MΩ",        "ORANGE"),
                    Step(9, "Vérifier serrage connexions et bornes (couple 5 daN.m)",    "NONE"),
                    Step(10, "Reposer capots, déconsigner  essai 5 min à vide",         "NONE"),
                },
            },
            new() {
                Id = NewId(1, 2), Code = "GMT-002",
                Titre = "Contrôle isolation thermique  moteur de traction",
                ClasseEquipement = "moteur_traction", Direction = "DMAT",
                EquipementId = Guid.Parse("11000000-0000-0000-0000-000000000001"),
                Version = 1, IsActive = true,
                Etapes = new List<Etape>
                {
                    Step(1, "Consigner l'engin  fiche de consignation signée",          "ROUGE"),
                    Step(2, "Déposer le capot d'inspection latéral moteur",              "NONE"),
                    Step(3, "Mesurer température enroulements à froid (valeur réf.)",    "NONE"),
                    Step(4, "Inspecter visuel lacques d'imprégnation  pas de fissure",  "NONE"),
                    Step(5, "Vérifier circuit de ventilation  obstruction interdite",   "NONE"),
                    Step(6, "Reposer capot, déconsigner, essai 30 min en charge",        "NONE"),
                    Step(7, "Mesurer température enroulements à chaud  max 155 °C",    "ORANGE"),
                },
            },

            // ══ BOGIE ESSIEUX ════════════════════════════════════════════════
            new() {
                Id = NewId(2, 1), Code = "GBG-001",
                Titre = "Inspection périodique bogie Y25  60 000 km",
                ClasseEquipement = "bogie_essieux", Direction = "DMAT",
                EquipementId = Guid.Parse("11000000-0000-0000-0000-000000000002"),
                Version = 1, IsActive = true,
                Etapes = new List<Etape>
                {
                    Step(1, "Immobiliser le véhicule  cales-roues aux 4 coins",         "ROUGE"),
                    Step(2, "Soulever la caisse au pont roulant (4 points de levage)",   "ROUGE"),
                    Step(3, "Déposer le bogie  2 personnes minimum",                    "ORANGE"),
                    Step(4, "Mesurer le jeu de roulement : tolérance ±0,3 mm",          "NONE"),
                    Step(5, "Contrôler état bandages roues  usure maxi : 30 mm",        "NONE"),
                    Step(6, "Inspecter boîtes d'essieux  pas de fuite ni surchauffe",  "NONE"),
                    Step(7, "Graisser les boîtes d'essieux (graisse EP2 150 g/boîte)",  "NONE"),
                    Step(8, "Vérifier suspensions primaires  pas de fissure",           "NONE"),
                    Step(9, "Contrôler la traverse danseuse  jeu axial 10–15 mm",      "NONE"),
                    Step(10, "Reposer bogie, régler hauteur de caisse (±5 mm)",          "ORANGE"),
                    Step(11, "Retirer les cales-roues et effectuer essai à 10 km/h",    "NONE"),
                },
            },

            // ══ COMPRESSEUR ══════════════════════════════════════════════════
            new() {
                Id = NewId(3, 1), Code = "GCMP-001",
                Titre = "Vérification filtre et soupapes  compresseur ATS",
                ClasseEquipement = "compresseur", Direction = "DMAT",
                EquipementId = Guid.Parse("11000000-0000-0000-0000-000000000003"),
                Version = 1, IsActive = true,
                Etapes = new List<Etape>
                {
                    Step(1, "Mettre hors service le compresseur  sectionnement",        "ROUGE"),
                    Step(2, "Purger le réservoir d'air jusqu'à pression nulle",          "ORANGE"),
                    Step(3, "Déposer et nettoyer les filtres d'admission à l'air",       "NONE"),
                    Step(4, "Remplacer le filtre si colmatage > 50% (papier coloré)",   "NONE"),
                    Step(5, "Contrôler état des soupapes d'aspiration et refoulement",   "NONE"),
                    Step(6, "Vérifier niveau d'huile  repère mi-hauteur viseur",       "NONE"),
                    Step(7, "Contrôler les courroies d'entraînement  flèche 10 mm",   "NONE"),
                    Step(8, "Remettre en service et vérifier montée pression à 9 bar", "NONE"),
                    Step(9, "Écouter fonctionnement  absence de bruit anormal",        "NONE"),
                },
            },

            // ══ POMPE À EAU ══════════════════════════════════════════════════
            new() {
                Id = NewId(4, 1), Code = "GPMP-001",
                Titre = "Contrôle garniture et débit  pompe eau moteur",
                ClasseEquipement = "pompe_eau", Direction = "DMAT",
                EquipementId = Guid.Parse("11000000-0000-0000-0000-000000000004"),
                Version = 1, IsActive = true,
                Etapes = new List<Etape>
                {
                    Step(1, "Arrêter le moteur diesel  refroidissement 30 min",         "ORANGE"),
                    Step(2, "Purger le circuit de refroidissement  robinet de vidange", "ORANGE"),
                    Step(3, "Déposer la pompe en préservant les joints toriques",        "NONE"),
                    Step(4, "Inspecter la garniture mécanique  pas de gorge > 0,1 mm","NONE"),
                    Step(5, "Contrôler la roue aubagée  pas d'érosion cavitation",    "NONE"),
                    Step(6, "Remplacer garniture si fuite > 3 gouttes/min",             "NONE"),
                    Step(7, "Reposer la pompe  couples de serrage selon plan",          "NONE"),
                    Step(8, "Remplir circuit  purger les bulles d'air à la purge",     "NONE"),
                    Step(9, "Vérifier débit au débitmètre : 600 ±30 L/min",            "NONE"),
                },
            },

            // ══ GRUE FERROVIAIRE ═════════════════════════════════════════════
            new() {
                Id = NewId(5, 1), Code = "GGRF-001",
                Titre = "Inspection câbles de levage  grue GE-25T",
                ClasseEquipement = "grue_ferroviaire", Direction = "DMAT",
                EquipementId = Guid.Parse("11000000-0000-0000-0000-000000000005"),
                Version = 1, IsActive = true,
                Etapes = new List<Etape>
                {
                    Step(1, "Garer la grue  frein de stationnement serré",              "ROUGE"),
                    Step(2, "Dérouler entièrement le câble principal sur l'aire prévue", "ORANGE"),
                    Step(3, "Inspecter visuellement mètre par mètre  éclairage rasant","ORANGE"),
                    Step(4, "Repérer les fils rompus  tolérance : 0 fil rompu/3 lay.",  "ROUGE"),
                    Step(5, "Mesurer le diamètre câble  réduction maxi 10%",           "NONE"),
                    Step(6, "Graisser le câble au pinceau (graisse câble FRX)",         "NONE"),
                    Step(7, "Contrôler les coins de serrage et les cosses d'ancrage",   "NONE"),
                    Step(8, "Essai de levage à 50% CMU  vérifier absence défilement", "ORANGE"),
                },
            },

            // ══ PONT ROULANT ═════════════════════════════════════════════════
            new() {
                Id = NewId(6, 1), Code = "GPR-001",
                Titre = "Vérification annuelle pont roulant 10 T",
                ClasseEquipement = "pont_roulant", Direction = "DMAT",
                EquipementId = Guid.Parse("11000000-0000-0000-0000-000000000006"),
                Version = 1, IsActive = true,
                Etapes = new List<Etape>
                {
                    Step(1, "Condamner le pont roulant  pancarte interdiction",         "ROUGE"),
                    Step(2, "Contrôler visuellement structure et soudures porteuses",    "ORANGE"),
                    Step(3, "Inspecter crochet et émousse  pas de déformation visible", "ORANGE"),
                    Step(4, "Vérifier fin de course haut et bas du palan",              "NONE"),
                    Step(5, "Contrôler l'état des galets de roulement sur rails",       "NONE"),
                    Step(6, "Mesurer l'écartement des rails (tolérance ±2 mm)",         "NONE"),
                    Step(7, "Essai à charge nominale 10 T  levée 200 mm pendant 5 min","ORANGE"),
                    Step(8, "Contrôler frein : arrêt en < 200 mm après coupure moteur", "ORANGE"),
                    Step(9, "Lever la condamnation  remettre en service",               "NONE"),
                },
            },

            // ══ TOURS ATELIER ════════════════════════════════════════════════
            new() {
                Id = NewId(7, 1), Code = "GTRA-001",
                Titre = "Reprofilage roues en fosse  tour Hegenscheidt",
                ClasseEquipement = "tours_atelier", Direction = "DMAT",
                EquipementId = Guid.Parse("11000000-0000-0000-0000-000000000007"),
                Version = 1, IsActive = true,
                Etapes = new List<Etape>
                {
                    Step(1, "Amener le véhicule sur la fosse  vitesse 5 km/h maxi",   "ROUGE"),
                    Step(2, "Freiner  mettre patin de sécurité anti-dérive",           "ROUGE"),
                    Step(3, "Mesurer le profil actuel de la roue (gabarit de contrôle)","NONE"),
                    Step(4, "Saisir les cotes de reprise dans le logiciel machine",     "NONE"),
                    Step(5, "Effectuer passe d'ébauche  avance 0,3 mm/tr",            "NONE"),
                    Step(6, "Contrôler rugosité après ébauche  Ra < 3,2 µm",          "NONE"),
                    Step(7, "Effectuer passe de finition  avance 0,1 mm/tr",          "NONE"),
                    Step(8, "Contrôler profil final au gabarit  tolérance ±0,05 mm",  "NONE"),
                    Step(9, "Retirer le patin, déplacer le véhicule, nettoyer fosse",  "NONE"),
                },
            },

            // ══ DRAISINE ════════════════════════════════════════════════════
            new() {
                Id = NewId(8, 1), Code = "GDRA-001",
                Titre = "Révision annuelle moteur et freins  draisine DMAT",
                ClasseEquipement = "draisine", Direction = "DMAT",
                EquipementId = Guid.Parse("11000000-0000-0000-0000-000000000008"),
                Version = 1, IsActive = true,
                Etapes = new List<Etape>
                {
                    Step(1, "Consigner la draisine  fiche de consignation signée",     "ROUGE"),
                    Step(2, "Vidanger le moteur diesel à chaud (huile 15W40)",          "NONE"),
                    Step(3, "Remplacer filtre à huile et filtre à carburant",           "NONE"),
                    Step(4, "Contrôler courroie de distribution  remplacement si usée","ORANGE"),
                    Step(5, "Régler les soupapes  jeu admiss. 0,25 mm / échapp. 0,30","NONE"),
                    Step(6, "Contrôler freins : patins  épaisseur mini 5 mm",         "NONE"),
                    Step(7, "Régler le jeu de frein  1 mm entre patin et bandage",    "NONE"),
                    Step(8, "Remplir d'huile moteur (6 L), refaire le plein carburant","NONE"),
                    Step(9, "Déconsigner  essai à 20 km/h sur voie de service",       "NONE"),
                },
            },

            // ══ AIGUILLAGE ══════════════════════════════════════════════════
            new() {
                Id = NewId(9, 1), Code = "GAIG-001",
                Titre = "Inspection mensuelle  aiguillage type 46",
                ClasseEquipement = "aiguillage", Direction = "DIF",
                EquipementId = Guid.Parse("22000000-0000-0000-0000-000000000001"),
                Version = 1, IsActive = true,
                Etapes = new List<Etape>
                {
                    Step(1, "Consigner le canton  appel CDT et blocage de zone",       "ROUGE"),
                    Step(2, "Déverrouiller et tester la commande manuelle",             "ORANGE"),
                    Step(3, "Contrôler les lames d'aiguille  absence de fissure",     "ORANGE"),
                    Step(4, "Mesurer le jeu en pointe côté aiguille fermée  max 6 mm","NONE"),
                    Step(5, "Contrôler le talonnage  ressort de repose intact",        "NONE"),
                    Step(6, "Nettoyer les glissières et lubrifier (graisse EP0)",       "NONE"),
                    Step(7, "Tester la commande électrique  temps de manoeuvre < 5 s","NONE"),
                    Step(8, "Vérifier l'enclenchement électrique de position A/B",     "NONE"),
                    Step(9, "Lever la consigne  tester passage en marche à 30 km/h",  "NONE"),
                },
            },
            new() {
                Id = NewId(9, 2), Code = "GAIG-002",
                Titre = "Remplacement lames d'aiguille  type 46",
                ClasseEquipement = "aiguillage", Direction = "DIF",
                EquipementId = Guid.Parse("22000000-0000-0000-0000-000000000001"),
                Version = 1, IsActive = true,
                Etapes = new List<Etape>
                {
                    Step(1, "Consigner le canton  bloquer circulation des deux sens",   "ROUGE"),
                    Step(2, "Démonter le mécanisme de commande  noter les réglages",   "ORANGE"),
                    Step(3, "Déposer les lames usées avec la pince à écrous spéciale",  "ORANGE"),
                    Step(4, "Contrôler les appuis de lames  planéité < 0,5 mm",       "NONE"),
                    Step(5, "Poser les nouvelles lames  graisser les glissières",      "NONE"),
                    Step(6, "Régler le jeu en pointe à 2 mm en position fermée",       "NONE"),
                    Step(7, "Remonter le mécanisme  ajuster les tringles de renvoi",  "NONE"),
                    Step(8, "Essais fonctionnels : 10 manœuvres alternées A/B",         "NONE"),
                    Step(9, "Lever la consigne après accord CDT  vitesse limite 30",   "NONE"),
                },
            },

            // ══ TRANSFORMATEUR HT ════════════════════════════════════════════
            new() {
                Id = NewId(10, 1), Code = "GTRS-001",
                Titre = "Contrôle annuel huile et conservateur  transfo HT",
                ClasseEquipement = "transformateur_ht", Direction = "DIF",
                EquipementId = Guid.Parse("22000000-0000-0000-0000-000000000002"),
                Version = 1, IsActive = true,
                Etapes = new List<Etape>
                {
                    Step(1, "Consigner le transformateur  déclencher DDR et secteur",  "ROUGE"),
                    Step(2, "Vérifier absence tension 225 kV côté HT (perche d'essai)", "ROUGE"),
                    Step(3, "Mettre à la terre les 3 phases HT et BT  prise de terre","ROUGE"),
                    Step(4, "Prélever échantillon d'huile (500 mL) au robinet de fond", "ORANGE"),
                    Step(5, "Analyser rigidité diélectrique  seuil mini 30 kV/2,5 mm","NONE"),
                    Step(6, "Contrôler niveau huile conservateur  repère 25 °C",       "NONE"),
                    Step(7, "Vérifier silicagel déshydratant  bleu = actif / rouge = HS","NONE"),
                    Step(8, "Inspecter radiateurs  pas de fuite ni dépôt de boue",    "NONE"),
                    Step(9, "Contrôler serrage des bornes HT et BT",                   "ORANGE"),
                    Step(10, "Lever la mise à la terre  déconsigner  remettre en service","ROUGE"),
                },
            },

            // ══ GROUPE ÉLECTROGÈNE ══════════════════════════════════════════
            new() {
                Id = NewId(11, 1), Code = "GGE-001",
                Titre = "Essai mensuel et entretien  groupe électrogène 500 kVA",
                ClasseEquipement = "groupe_electrogene", Direction = "DIF",
                EquipementId = Guid.Parse("22000000-0000-0000-0000-000000000003"),
                Version = 1, IsActive = true,
                Etapes = new List<Etape>
                {
                    Step(1, "Vérifier niveau carburant  plein si < 50% (cuve 500 L)", "NONE"),
                    Step(2, "Contrôler niveau huile moteur  repère mi-jauge",          "NONE"),
                    Step(3, "Vérifier niveau liquide de refroidissement",               "NONE"),
                    Step(4, "Inspecter la batterie de démarrage  bornes propres",      "NONE"),
                    Step(5, "Démarrer manuellement  noter temps démarrage (< 10 s)",  "NONE"),
                    Step(6, "Laisser chauffer 5 min sans charge",                       "NONE"),
                    Step(7, "Connecter charge nominale 500 kVA  surveiller tension",  "ORANGE"),
                    Step(8, "Vérifier tension aux bornes : 400 V ±2% / fréq. 50 Hz",  "NONE"),
                    Step(9, "Fonctionnement 30 min à pleine charge  consigner valeurs","NONE"),
                    Step(10, "Délester et arrêt normal  noter consommation (L/h)",     "NONE"),
                },
            },

            // ══ SIGNAL FERROVIAIRE ══════════════════════════════════════════
            new() {
                Id = NewId(12, 1), Code = "GSIG-001",
                Titre = "Contrôle alignement et intensité  signal lumineux LED",
                ClasseEquipement = "signal_ferroviaire", Direction = "DIF",
                EquipementId = Guid.Parse("22000000-0000-0000-0000-000000000004"),
                Version = 1, IsActive = true,
                Etapes = new List<Etape>
                {
                    Step(1, "Consigner la voie  accord CDT et blocage zone signal",    "ROUGE"),
                    Step(2, "Inspecter l'état mécanique du mât et de la platine",      "NONE"),
                    Step(3, "Vérifier alignement axial du signal depuis 300 m de voie","NONE"),
                    Step(4, "Mesurer l'intensité lumineuse (luxmètre)  mini 50 cd",   "NONE"),
                    Step(5, "Contrôler chaque aspect : voie libre, ralentissement, stop","NONE"),
                    Step(6, "Vérifier étanchéité du coffret et fixation des optiques", "NONE"),
                    Step(7, "Nettoyer les lentilles de signalisation au chiffon doux", "NONE"),
                    Step(8, "Lever la consigne  tester avec CDT les changements aspects","NONE"),
                },
            },
        };

        db.Gammes.AddRange(gammes);
        await db.SaveChangesAsync();
        log.LogInformation("{Count} gammes avec leurs étapes créées", gammes.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static Guid NewId(int equipIndex, int gammeIndex) =>
        Guid.Parse($"AAAA{equipIndex:D4}-0000-0000-0000-{gammeIndex:D12}");

    private static Etape Step(int ordre, string description,
        string securite = "NONE", string? arRef = null, int? duree = null) =>
        new()
        {
            Id             = Guid.NewGuid(),
            OrdreIndex     = ordre,
            Description    = description,
            NiveauSecurite = securite,
            ArContentRef   = arRef,
            DureeSecondes  = duree,
        };
}
