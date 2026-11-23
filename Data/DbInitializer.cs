using MaRa.Api.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MaRa.Api.Data;

/// <summary>
/// Données initiales pour le prototype MA-RA CAMRAIL.
/// Idempotent : vérifie l'existence avant toute insertion.
/// Aucune valeur technique sans source : les mesures renvoient à la valeur validée par CAMRAIL.
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
            await AlignerDemoAsync(db, log);
        }

        await SeedDemoEssieuT17Async(db, log);

        // Gammes créées avant l'ajout du statut : elles sont des brouillons.
        var sansStatut = await db.Gammes.Where(g => g.Statut == "").ExecuteUpdateAsync(s => s.SetProperty(g => g.Statut, "BROUILLON"));
        if (sansStatut > 0) log.LogInformation("{Count} gammes sans statut passées en brouillon", sansStatut);
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
            ("securite",     "securite@camrail.cm",     "Secu@CMSF2025!",     "CMSF"),
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
    // ÉQUIPEMENTS  (12 classes IA  DMAT + DIF), fictifs
    // ─────────────────────────────────────────────────────────────────────────

    private const string DescriptionDemo =
        "Équipement fictif de démonstration. Caractéristiques à fournir par CAMRAIL.";

    private static Equipement Equip(string id, string code, string direction, string classe,
        string designation, string marqueur) => new()
    {
        Id          = Guid.Parse(id),
        Code        = code,
        Direction   = direction,
        Classe      = classe,
        Designation = designation,
        Marqueur    = marqueur,
        Statut      = "actif",
        Description = DescriptionDemo,
    };

    private static List<Equipement> EquipementsDemo() => new()
    {
        // ── DMAT ─────────────────────────────────────────────────────────
        Equip("11000000-0000-0000-0000-000000000001", "LOC-001", "DMAT", "moteur_traction",
            "Moteur de traction de locomotive diesel", "VUF_MTR_001"),
        Equip("11000000-0000-0000-0000-000000000002", "BOG-001", "DMAT", "bogie_essieux",
            "Bogie de wagon à deux essieux", "VUF_BOG_001"),
        Equip("11000000-0000-0000-0000-000000000003", "COMP-001", "DMAT", "compresseur",
            "Compresseur d'air de frein", "VUF_COMP_001"),
        Equip("11000000-0000-0000-0000-000000000004", "POMP-001", "DMAT", "pompe_eau",
            "Pompe à eau de refroidissement de moteur diesel", "VUF_POMP_001"),
        Equip("11000000-0000-0000-0000-000000000005", "GRF-001", "DMAT", "grue_ferroviaire",
            "Grue ferroviaire automotrice", "VUF_GRF_001"),
        Equip("11000000-0000-0000-0000-000000000006", "PR-001", "DMAT", "pont_roulant",
            "Pont roulant d'atelier", "VUF_PR_001"),
        Equip("11000000-0000-0000-0000-000000000007", "TRA-001", "DMAT", "tours_atelier",
            "Tour en fosse pour reprofilage des roues", "VUF_TRA_001"),
        Equip("11000000-0000-0000-0000-000000000008", "DRA-001", "DMAT", "draisine",
            "Draisine à moteur thermique", "VUF_DRA_001"),
        // ── DIF ──────────────────────────────────────────────────────────
        Equip("22000000-0000-0000-0000-000000000001", "AIG-001", "DIF", "aiguillage",
            "Appareil de voie à commande électrique", "VUF_AIG_001"),
        Equip("22000000-0000-0000-0000-000000000002", "TRS-001", "DIF", "transformateur_ht",
            "Transformateur de puissance", "VUF_TRS_001"),
        Equip("22000000-0000-0000-0000-000000000003", "GE-001", "DIF", "groupe_electrogene",
            "Groupe électrogène de secours", "VUF_GE_001"),
        Equip("22000000-0000-0000-0000-000000000004", "SIG-001", "DIF", "signal_ferroviaire",
            "Signal lumineux à LED", "VUF_SIG_001"),
    };

    private static async Task SeedEquipementsAsync(AppDbContext db, ILogger log)
    {
        var equipements = EquipementsDemo();
        db.Equipements.AddRange(equipements);
        await db.SaveChangesAsync();
        log.LogInformation("{Count} équipements créés", equipements.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DÉMONSTRATION ESSIEU T17
    // Aucune cote : les valeurs viendront de la gamme validée par CAMRAIL.
    // ─────────────────────────────────────────────────────────────────────────

    private const string PointArret =
        "Point d'arrêt : en cas de doute, signaler un écart et appeler l'expert";

    private static async Task SeedDemoEssieuT17Async(AppDbContext db, ILogger log)
    {
        var equipementId = Guid.Parse("11000000-0000-0000-0000-000000000117");
        if (await db.Equipements.AnyAsync(e => e.Id == equipementId))
        {
            var ancienne = await db.Etapes.FirstOrDefaultAsync(e =>
                e.GammeId == NewId(117, 1) && e.Description.StartsWith("En cas de doute"));
            if (ancienne is not null) ancienne.Description = PointArret;
            var gammeT17 = await db.Gammes.Include(g => g.Etapes).FirstOrDefaultAsync(g => g.Id == NewId(117, 1));
            if (gammeT17 is not null) AppliquerRefsMaquette(gammeT17);
            await db.SaveChangesAsync();
            return;
        }

        db.Equipements.Add(new Equipement
        {
            Id          = equipementId,
            Code        = "ESS-T17-DEMO",
            Direction   = "DMAT",
            Classe      = "bogie_essieux",
            Designation = "Essieu T17 (exemple de démonstration)",
            Marqueur    = "QR_ESS_T17",
            Statut      = "actif",
            Description = "Équipement fictif pour la démonstration. Numéro et données à remplacer " +
                          "par ceux de CAMRAIL.",
        });

        var etape = 0;
        Etape E(string texte, string securite = "NONE") => Step(++etape, texte, securite);

        db.Gammes.Add(new Gamme
        {
            Id               = NewId(117, 1),
            Code             = "GESS-T17-DEMO",
            Titre            = MentionDemo + "Visite d'un essieu T17  déroulé type",
            ClasseEquipement = "bogie_essieux",
            Direction        = "DMAT",
            EquipementId     = equipementId,
            Version          = 1,
            IsActive         = true,
            Etapes           = new List<Etape>
            {
                E("Identifier l'essieu : relever son numéro et ouvrir son historique"),
                E("Vérifier la gamme applicable et l'habilitation de l'opérateur", "ORANGE"),
                E("Sécuriser le poste et la manutention selon les règles CAMRAIL", "ROUGE"),
                E("Nettoyer l'essieu pour permettre l'examen"),
                E("Examiner visuellement les roues, le corps et les fusées, avec photo"),
                E("Mesurer les roues et saisir les valeurs : limites à fournir par CAMRAIL"),
                E("Contrôler les boîtes et roulements selon la gamme CAMRAIL"),
                E("Réaliser les contrôles non destructifs requis : personnel qualifié", "ORANGE"),
                E(PointArret, "ROUGE"),
                E("Clôturer : signature de l'opérateur et compte rendu de l'essieu", "ORANGE"),
            },
        });
        AppliquerRefsMaquette(db.Gammes.Local.First(g => g.Id == NewId(117, 1)));

        await db.SaveChangesAsync();
        log.LogInformation("Démonstration essieu T17 créée");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GAMMES DE DÉMONSTRATION  (max 80 caractères par étape)
    // Déroulés types sans valeur chiffrée : chaque mesure se compare à la valeur
    // validée de la gamme CAMRAIL.
    // ─────────────────────────────────────────────────────────────────────────

    private const string MentionDemo = "[DÉMO, valeurs non validées] ";

    private static Gamme G(int equip, int index, string code, string titre, string classe,
        string direction, string equipementId, params Etape[] etapes) => new()
    {
        Id               = NewId(equip, index),
        Code             = code,
        Titre            = MentionDemo + titre,
        ClasseEquipement = classe,
        Direction        = direction,
        EquipementId     = Guid.Parse(equipementId),
        Version          = 1,
        IsActive         = true,
        Etapes           = etapes.ToList(),
    };

    private const string Mtr = "11000000-0000-0000-0000-000000000001";
    private const string Bog = "11000000-0000-0000-0000-000000000002";
    private const string Cmp = "11000000-0000-0000-0000-000000000003";
    private const string Pmp = "11000000-0000-0000-0000-000000000004";
    private const string Grf = "11000000-0000-0000-0000-000000000005";
    private const string Pr  = "11000000-0000-0000-0000-000000000006";
    private const string Tra = "11000000-0000-0000-0000-000000000007";
    private const string Dra = "11000000-0000-0000-0000-000000000008";
    private const string Aig = "22000000-0000-0000-0000-000000000001";
    private const string Trs = "22000000-0000-0000-0000-000000000002";
    private const string Ge  = "22000000-0000-0000-0000-000000000003";
    private const string Sig = "22000000-0000-0000-0000-000000000004";

    private static List<Gamme> GammesDemo()
    {
        var gammes = GammesDemoTextes();
        foreach (var g in gammes) AppliquerRefsMaquette(g);
        return gammes;
    }

    private static List<Gamme> GammesDemoTextes() => new()
    {
        G(1, 1, "GMT-001", "Révision des balais et du collecteur, moteur de traction",
            "moteur_traction", "DMAT", Mtr,
            Step(1, "Consigner l'engin et verrouiller le sectionneur principal", "ROUGE"),
            Step(2, "Vérifier l'absence de tension aux bornes du moteur", "ORANGE"),
            Step(3, "Déposer les capots d'accès aux porte-balais"),
            Step(4, "Mesurer la longueur des balais et comparer à la valeur validée"),
            Step(5, "Contrôler l'état du collecteur : rainures, traces d'arc"),
            Step(6, "Nettoyer le collecteur selon la gamme du constructeur"),
            Step(7, "Remplacer les balais usés et régler la pression des ressorts", "ORANGE"),
            Step(8, "Mesurer l'isolement des enroulements, comparer au seuil validé", "ORANGE"),
            Step(9, "Vérifier le serrage des connexions au couple de la gamme"),
            Step(10, "Reposer les capots, déconsigner et faire l'essai à vide")),

        G(1, 2, "GMT-002", "Contrôle de l'isolation thermique, moteur de traction",
            "moteur_traction", "DMAT", Mtr,
            Step(1, "Consigner l'engin avec une fiche de consignation signée", "ROUGE"),
            Step(2, "Déposer le capot d'inspection latéral du moteur"),
            Step(3, "Mesurer la température des enroulements à froid (référence)"),
            Step(4, "Inspecter les vernis d'imprégnation : absence de fissure"),
            Step(5, "Vérifier le circuit de ventilation : aucune obstruction"),
            Step(6, "Reposer le capot, déconsigner et faire l'essai en charge"),
            Step(7, "Mesurer la température à chaud et comparer au seuil validé", "ORANGE")),

        G(2, 1, "GBG-001", "Inspection périodique d'un bogie à deux essieux",
            "bogie_essieux", "DMAT", Bog,
            Step(1, "Immobiliser le véhicule avec des cales sous les roues", "ROUGE"),
            Step(2, "Soulever la caisse aux points de levage prévus", "ROUGE"),
            Step(3, "Déposer le bogie selon les règles de manutention", "ORANGE"),
            Step(4, "Mesurer le jeu des roulements et comparer à la valeur validée"),
            Step(5, "Mesurer l'usure des roues et comparer aux limites validées"),
            Step(6, "Inspecter les boîtes d'essieux : fuite, échauffement"),
            Step(7, "Graisser les boîtes selon la gamme : produit et quantité"),
            Step(8, "Vérifier la suspension : ressorts, absence de fissure"),
            Step(9, "Contrôler le jeu de la traverse danseuse (valeur validée)"),
            Step(10, "Reposer le bogie et régler la hauteur de caisse", "ORANGE"),
            Step(11, "Retirer les cales et faire l'essai de roulage prévu")),

        G(3, 1, "GCMP-001", "Vérification des filtres et soupapes, compresseur",
            "compresseur", "DMAT", Cmp,
            Step(1, "Mettre le compresseur hors service par sectionnement", "ROUGE"),
            Step(2, "Purger le réservoir d'air jusqu'à pression nulle", "ORANGE"),
            Step(3, "Déposer et nettoyer les filtres d'admission d'air"),
            Step(4, "Remplacer le filtre s'il est colmaté"),
            Step(5, "Contrôler les soupapes d'aspiration et de refoulement"),
            Step(6, "Vérifier le niveau d'huile au voyant"),
            Step(7, "Contrôler la tension des courroies (valeur validée)"),
            Step(8, "Remettre en service et vérifier la montée en pression"),
            Step(9, "Écouter le fonctionnement : absence de bruit anormal")),

        G(4, 1, "GPMP-001", "Contrôle de la garniture et du débit, pompe à eau",
            "pompe_eau", "DMAT", Pmp,
            Step(1, "Arrêter le moteur diesel et le laisser refroidir", "ORANGE"),
            Step(2, "Vidanger le circuit de refroidissement", "ORANGE"),
            Step(3, "Déposer la pompe en préservant les joints"),
            Step(4, "Inspecter la garniture mécanique : usure, rayures"),
            Step(5, "Contrôler la roue à aubes : érosion, cavitation"),
            Step(6, "Remplacer la garniture en cas de fuite"),
            Step(7, "Reposer la pompe aux couples de serrage du plan"),
            Step(8, "Remplir le circuit et purger l'air"),
            Step(9, "Mesurer le débit et comparer à la valeur validée")),

        G(5, 1, "GGRF-001", "Inspection des câbles de levage, grue ferroviaire",
            "grue_ferroviaire", "DMAT", Grf,
            Step(1, "Garer la grue, frein de stationnement serré", "ROUGE"),
            Step(2, "Dérouler le câble principal sur l'aire prévue", "ORANGE"),
            Step(3, "Inspecter le câble sur toute sa longueur, en éclairage rasant", "ORANGE"),
            Step(4, "Repérer les fils rompus et comparer au critère de dépose", "ROUGE"),
            Step(5, "Mesurer le diamètre du câble et comparer au minimum validé"),
            Step(6, "Graisser le câble avec le produit prévu"),
            Step(7, "Contrôler les serre-câbles et les cosses d'ancrage"),
            Step(8, "Faire l'essai de levage prévu : absence de glissement", "ORANGE")),

        G(6, 1, "GPR-001", "Vérification périodique du pont roulant",
            "pont_roulant", "DMAT", Pr,
            Step(1, "Condamner le pont roulant et poser la pancarte", "ROUGE"),
            Step(2, "Contrôler la structure et les soudures porteuses", "ORANGE"),
            Step(3, "Inspecter le crochet et son linguet : aucune déformation", "ORANGE"),
            Step(4, "Vérifier les fins de course haut et bas du palan"),
            Step(5, "Contrôler les galets de roulement sur les rails"),
            Step(6, "Mesurer l'écartement des rails et comparer à la valeur validée"),
            Step(7, "Faire l'essai en charge prévu par l'organisme agréé", "ORANGE"),
            Step(8, "Contrôler le freinage après coupure du moteur", "ORANGE"),
            Step(9, "Lever la condamnation et remettre en service")),

        G(7, 1, "GTRA-001", "Reprofilage des roues au tour en fosse",
            "tours_atelier", "DMAT", Tra,
            Step(1, "Amener le véhicule sur la fosse à vitesse réduite", "ROUGE"),
            Step(2, "Freiner et poser le dispositif anti-dérive", "ROUGE"),
            Step(3, "Mesurer le profil actuel de la roue au gabarit"),
            Step(4, "Saisir les cotes de reprise dans le logiciel de la machine"),
            Step(5, "Effectuer la passe d'ébauche aux paramètres de la gamme"),
            Step(6, "Contrôler la rugosité après ébauche (valeur validée)"),
            Step(7, "Effectuer la passe de finition aux paramètres de la gamme"),
            Step(8, "Contrôler le profil final au gabarit (tolérance validée)"),
            Step(9, "Retirer le dispositif, dégager le véhicule, nettoyer la fosse")),

        G(8, 1, "GDRA-001", "Révision du moteur et des freins, draisine",
            "draisine", "DMAT", Dra,
            Step(1, "Consigner la draisine avec une fiche de consignation signée", "ROUGE"),
            Step(2, "Vidanger le moteur à chaud avec l'huile prévue"),
            Step(3, "Remplacer les filtres à huile et à carburant"),
            Step(4, "Contrôler la courroie de distribution", "ORANGE"),
            Step(5, "Régler le jeu des soupapes aux valeurs du constructeur"),
            Step(6, "Mesurer l'épaisseur des patins de frein (minimum validé)"),
            Step(7, "Régler le jeu de frein à la valeur du constructeur"),
            Step(8, "Refaire les niveaux d'huile et de carburant"),
            Step(9, "Déconsigner et faire l'essai sur voie de service")),

        G(9, 1, "GAIG-001", "Inspection périodique d'un appareil de voie",
            "aiguillage", "DIF", Aig,
            Step(1, "Consigner le canton : accord du régulateur, blocage de zone", "ROUGE"),
            Step(2, "Déverrouiller et tester la commande manuelle", "ORANGE"),
            Step(3, "Contrôler les lames d'aiguille : absence de fissure", "ORANGE"),
            Step(4, "Mesurer le jeu en pointe côté fermé (valeur validée)"),
            Step(5, "Contrôler le talonnage et le ressort de rappel"),
            Step(6, "Nettoyer et lubrifier les coussinets de glissement"),
            Step(7, "Mesurer le temps de manœuvre électrique (valeur validée)"),
            Step(8, "Vérifier le contrôle électrique de position"),
            Step(9, "Lever la consigne et faire l'essai de passage prévu")),

        G(9, 2, "GAIG-002", "Remplacement des lames d'aiguille",
            "aiguillage", "DIF", Aig,
            Step(1, "Consigner le canton et bloquer la circulation", "ROUGE"),
            Step(2, "Démonter le mécanisme de commande en notant les réglages", "ORANGE"),
            Step(3, "Déposer les lames usées avec l'outillage prévu", "ORANGE"),
            Step(4, "Contrôler la planéité des appuis (valeur validée)"),
            Step(5, "Poser les nouvelles lames et graisser les coussinets"),
            Step(6, "Régler le jeu en pointe à la valeur validée"),
            Step(7, "Remonter le mécanisme et ajuster les tringles"),
            Step(8, "Faire les manœuvres d'essai prévues par la gamme"),
            Step(9, "Lever la consigne après accord du régulateur")),

        G(10, 1, "GTRS-001", "Inspection visuelle d'un transformateur immergé à cuve hermétique",
            "transformateur_ht", "DIF", Trs,
            Step(1, "Consigner le transformateur : ouvrir et verrouiller les départs", "ROUGE"),
            Step(2, "Vérifier l'absence de tension côté HT avec la perche", "ROUGE"),
            Step(3, "Mettre à la terre et en court-circuit les phases HT et BT", "ROUGE"),
            Step(4, "Prélever un échantillon d'huile au robinet de fond", "ORANGE"),
            Step(5, "Faire analyser la rigidité diélectrique (seuil validé)"),
            Step(6, "Repérer les zones d'étanchéité de la cuve"),
            Step(7, "Examiner les parties visibles des traversées HT et BT"),
            Step(8, "Inspecter les parois ondulées : fuite, dépôts"),
            Step(9, "Contrôler le serrage des bornes HT et BT", "ORANGE"),
            Step(10, "Lever la mise à la terre, déconsigner, remettre en service", "ROUGE")),

        G(11, 1, "GGE-001", "Essai et entretien du groupe électrogène",
            "groupe_electrogene", "DIF", Ge,
            Step(1, "Vérifier le niveau de carburant et compléter si besoin"),
            Step(2, "Contrôler le niveau d'huile moteur à la jauge"),
            Step(3, "Vérifier le niveau du liquide de refroidissement"),
            Step(4, "Inspecter la batterie de démarrage : bornes propres"),
            Step(5, "Démarrer et noter le temps de démarrage"),
            Step(6, "Laisser chauffer à vide selon la gamme"),
            Step(7, "Brancher la charge d'essai et surveiller la tension", "ORANGE"),
            Step(8, "Mesurer tension et fréquence, comparer aux valeurs validées"),
            Step(9, "Faire fonctionner en charge et relever les valeurs"),
            Step(10, "Délester, arrêter et noter la consommation")),

        G(12, 1, "GSIG-001", "Contrôle de l'alignement et de l'intensité, signal LED",
            "signal_ferroviaire", "DIF", Sig,
            Step(1, "Consigner la voie : accord du régulateur, blocage de zone", "ROUGE"),
            Step(2, "Inspecter l'état mécanique du mât et de la platine"),
            Step(3, "Vérifier l'alignement du signal depuis le point de visée prévu"),
            Step(4, "Mesurer l'intensité lumineuse (minimum validé)"),
            Step(5, "Contrôler chaque indication du signal"),
            Step(6, "Vérifier l'étanchéité du coffret et la fixation des optiques"),
            Step(7, "Nettoyer les lentilles au chiffon doux"),
            Step(8, "Lever la consigne et tester les changements d'indication")),
    };

    private static async Task SeedGammesAsync(AppDbContext db, ILogger log)
    {
        var gammes = GammesDemo();
        db.Gammes.AddRange(gammes);
        await db.SaveChangesAsync();
        log.LogInformation("{Count} gammes avec leurs étapes créées", gammes.Count);
    }

    /// <summary>
    /// Remplace, dans une base déjà initialisée, les anciens textes de démonstration
    /// (tolérances et caractéristiques sans source) par les textes actuels.
    /// </summary>
    private static async Task AlignerDemoAsync(AppDbContext db, ILogger log)
    {
        var modifies = 0;

        var equipements = EquipementsDemo().ToDictionary(e => e.Id);
        foreach (var e in await db.Equipements.Where(e => equipements.Keys.Contains(e.Id)).ToListAsync())
        {
            var ref_ = equipements[e.Id];
            if (e.Designation == ref_.Designation && e.Description == ref_.Description) continue;
            e.Designation = ref_.Designation;
            e.Description = ref_.Description;
            modifies++;
        }

        var gammes = GammesDemo().ToDictionary(g => g.Code);
        var codes  = gammes.Keys.ToList();
        foreach (var g in await db.Gammes.Include(g => g.Etapes).Where(g => codes.Contains(g.Code)).ToListAsync())
        {
            var ref_ = gammes[g.Code];
            if (g.Titre != ref_.Titre) { g.Titre = ref_.Titre; modifies++; }
            foreach (var etape in g.Etapes)
            {
                var modele = ref_.Etapes.FirstOrDefault(r => r.OrdreIndex == etape.OrdreIndex);
                if (modele is null) continue;
                if (etape.Description != modele.Description) { etape.Description = modele.Description; modifies++; }
                if (etape.ArContentRef != modele.ArContentRef) { etape.ArContentRef = modele.ArContentRef; modifies++; }
            }
        }

        if (modifies == 0) return;
        await db.SaveChangesAsync();
        log.LogInformation("{Count} textes de démonstration remplacés", modifies);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LIENS ÉTAPE → MAQUETTE  (catalogue ma_ra_dotnet/Maquettes)
    // Format : maquette:<actif>?piece=<pièce ou organe>&vue=<vue>
    // ─────────────────────────────────────────────────────────────────────────

    private static string Ref(string actif, string? piece = null, string? vue = null)
    {
        var q = new List<string>();
        if (piece is not null) q.Add($"piece={piece}");
        if (vue is not null) q.Add($"vue={vue}");
        return $"maquette:{actif}" + (q.Count > 0 ? "?" + string.Join("&", q) : "");
    }

    private const string AxeEg6 = "axe-eg6", Bogie = "bogie-y25-metrique", Pompe = "pompe-eau-qsk19",
        Pont = "pont-roulant-palan", Moteur = "moteur-traction-cc", Compresseur = "compresseur-pistons",
        Tour = "tour-en-fosse", Aiguillage = "aiguillage-principe", Grue = "grue-ferroviaire",
        Draisine = "draisine-vmt", Transfo = "transformateur-immerge", Groupe = "groupe-electrogene",
        Signal = "signal-led";

    private static readonly Dictionary<string, Dictionary<int, string>> RefsMaquette = new()
    {
        ["GESS-T17-DEMO"] = new() { [1] = Ref(AxeEg6), [4] = Ref(AxeEg6), [5] = Ref(AxeEg6, "corps"),
            [6] = Ref(Bogie, "roue_1g"), [7] = Ref(Bogie, "boite_1g", "eclate") },
        ["GBG-001"] = new() { [3] = Ref(Bogie, "chassis"), [4] = Ref(Bogie, "roulement_ext_1g", "eclate"),
            [5] = Ref(Bogie, "roue_1g"), [6] = Ref(Bogie, "boite_1g"), [7] = Ref(Bogie, "boite_1g", "eclate"),
            [8] = Ref(Bogie, "ressorts_1g"), [9] = Ref(Bogie, "traverse"), [10] = Ref(Bogie, "chassis") },
        ["GPMP-001"] = new() { [3] = Ref(Pompe, "volute"), [4] = Ref(Pompe, "garniture", "eclate"),
            [5] = Ref(Pompe, "roue", "eclate"), [6] = Ref(Pompe, "garniture", "eclate"), [7] = Ref(Pompe, "vis_volute"),
            [8] = Ref(Pompe, "volute"), [9] = Ref(Pompe, "volute") },
        ["GPR-001"] = new() { [2] = Ref(Pont, "poutre"), [3] = Ref(Pont, "crochet"), [4] = Ref(Pont, "fin_de_course"),
            [5] = Ref(Pont, "galets_pont"), [6] = Ref(Pont, "chemins"), [8] = Ref(Pont, "frein") },
        ["GMT-001"] = new() { [3] = Ref(Moteur, "trappe_1"), [4] = Ref(Moteur, "porte_balais_1"),
            [5] = Ref(Moteur, "collecteur", "coupe"), [6] = Ref(Moteur, "collecteur", "coupe"),
            [7] = Ref(Moteur, "porte_balais_1"), [8] = Ref(Moteur, "induit", "coupe"), [9] = Ref(Moteur, "carcasse") },
        ["GMT-002"] = new() { [2] = Ref(Moteur, "trappe_1"), [3] = Ref(Moteur, "poles", "coupe") },
        ["GCMP-001"] = new() { [3] = Ref(Compresseur, "filtre_admission"), [4] = Ref(Compresseur, "filtre_admission"),
            [5] = Ref(Compresseur, "soupapes_bp", "coupe"), [7] = Ref(Compresseur, "accouplement") },
        ["GGRF-001"] = new() { [2] = Ref(Grue, "treuil"), [7] = Ref(Grue, "tete_fleche"), [8] = Ref(Grue, "crochet_grue") },
        ["GTRA-001"] = new() { [1] = Ref(Tour, "essieu_usine"), [3] = Ref(Tour, "roue_tour_g"),
            [5] = Ref(Tour, "tete_outil_g"), [7] = Ref(Tour, "tete_outil_d"), [8] = Ref(Tour, "palpeurs") },
        ["GDRA-001"] = new() { [2] = Ref(Draisine, "moteur_diesel"), [4] = Ref(Draisine, "moteur_diesel"),
            [6] = Ref(Draisine, "freins_draisine_1"), [7] = Ref(Draisine, "freins_draisine_1") },
        ["GAIG-001"] = new() { [2] = Ref(Aiguillage, "moteur_aiguille"), [3] = Ref(Aiguillage, "lame_droite"),
            [4] = Ref(Aiguillage, "lame_gauche"), [6] = Ref(Aiguillage, "traverses"), [7] = Ref(Aiguillage, "moteur_aiguille") },
        ["GAIG-002"] = new() { [2] = Ref(Aiguillage, "moteur_aiguille"), [3] = Ref(Aiguillage, "lame_droite"),
            [7] = Ref(Aiguillage, "tringle") },
        ["GTRS-001"] = new() { [6] = Ref(Transfo, "cuve"), [7] = Ref(Transfo, "traversee"), [8] = Ref(Transfo, "ondes"),
            [9] = Ref(Transfo, "traversees_bt") },
        ["GGE-001"] = new() { [2] = Ref(Groupe, "moteur_ge"), [3] = Ref(Groupe, "radiateur"), [4] = Ref(Groupe, "batterie"),
            [8] = Ref(Groupe, "alternateur") },
        ["GSIG-001"] = new() { [2] = Ref(Signal, "mat"), [3] = Ref(Signal, "feu_vert"), [6] = Ref(Signal, "module_vert", "eclate"),
            [7] = Ref(Signal, "module_vert") },
    };

    /// <summary>Renseigne le lien vers la maquette des étapes de démonstration ; les autres gardent leur valeur.</summary>
    private static void AppliquerRefsMaquette(Gamme g)
    {
        if (!RefsMaquette.TryGetValue(g.Code, out var refs)) return;
        foreach (var e in g.Etapes)
            if (refs.TryGetValue(e.OrdreIndex, out var r)) e.ArContentRef = r;
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
