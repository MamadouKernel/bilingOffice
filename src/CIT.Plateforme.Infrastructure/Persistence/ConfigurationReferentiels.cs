using System.Linq.Expressions;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Incidents;
using CIT.Plateforme.Domain.Notifications;
using CIT.Plateforme.Domain.Referentiels;
using CIT.Plateforme.Domain.SuiviTemps;
using CIT.Plateforme.Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// NORMALISATION (Merise, 3FN) DES ÉNUMÉRATIONS — deux traitements, selon la nature de la liste :
/// <list type="number">
///   <item><b>Référentiel métier</b> (valeurs affichées, libellés administrables) : table dédiée
///   et CLÉ ÉTRANGÈRE depuis chaque colonne qui la porte. Le libellé dépend de la seule clé.</item>
///   <item><b>Domaine de valeurs technique</b> (état d'un cycle de vie, stratégie, discriminant) :
///   pas d'entité — ce n'est qu'un domaine d'attribut au sens Merise — mais une contrainte CHECK
///   qui interdit en base toute valeur hors de l'énumération.</item>
/// </list>
/// Extrait de <see cref="PlateformeDbContext"/> pour garder ce dernier lisible.
/// </summary>
internal static class ConfigurationReferentiels
{
    private const int LongueurCode = 32;

    public static void Appliquer(ModelBuilder builder)
    {
        Referentiel<ReferentielGraviteIncident, GraviteIncident>(builder, "GravitesIncident");
        Referentiel<ReferentielMotifCloture, MotifClotureType>(builder, "MotifsCloture");
        Referentiel<ReferentielCategorieDocument, CategorieDocumentBillingImport>(builder, "CategoriesDocument");
        Referentiel<ReferentielCanalNotification, CanalNotification>(builder, "CanauxNotification");
        builder.Entity<ParametreTypeClient>().Property(p => p.Code).HasConversion<string>().HasMaxLength(LongueurCode);

        // Stockage homogène : toutes les colonnes d'énumération sont des codes texte. Ces deux-là
        // étaient encore des entiers (0, 1…) — illisibles en base et incompatibles avec la clé
        // étrangère vers leur référentiel.
        builder.Entity<Ticket>().Property(t => t.MotifCloture).HasConversion<string>().HasMaxLength(LongueurCode);
        builder.Entity<ParametreSlaAdministrable>().Property(p => p.TypeClient).HasConversion<string>().HasMaxLength(LongueurCode);
        builder.Entity<Ticket>().Property(t => t.Gravite).HasMaxLength(LongueurCode);

        Lier<SocieteCliente, ParametreTypeClient>(builder, s => s.Profil);
        Lier<Dossier, ParametreTypeClient>(builder, d => d.TypeClient);
        Lier<ParametreSlaAdministrable, ParametreTypeClient>(builder, p => p.TypeClient);
        Lier<ReglePrioriteAutomatique, ParametreTypeClient>(builder, r => r.TypeClient);
        Lier<Ticket, ReferentielGraviteIncident>(builder, t => t.Gravite);
        Lier<Ticket, ReferentielMotifCloture>(builder, t => t.MotifCloture);
        Lier<Document, ReferentielCategorieDocument>(builder, d => d.Categorie);
        Lier<EnvoiNotification, ReferentielCanalNotification>(builder, e => e.Canal);
        Lier<ModeleNotification, ReferentielCanalNotification>(builder, m => m.Canal);
        Lier<PreferenceCanalUtilisateur, ReferentielCanalNotification>(builder, p => p.Canal);
        Lier<RegleDiffusion, ReferentielCanalNotification>(builder, r => r.Canal);
    }

    /// <summary>
    /// Contrainte CHECK sur toute colonne d'énumération qui n'est PAS déjà garantie par une clé
    /// étrangère vers un référentiel. À appeler en dernier : parcourt le modèle complet, de sorte
    /// qu'une énumération ajoutée plus tard est couverte sans y penser.
    /// </summary>
    public static void ContraindreEnumerations(ModelBuilder builder)
    {
        foreach (var entite in builder.Model.GetEntityTypes().Where(e => !e.IsOwned() && e.GetTableName() is not null))
        {
            var table = entite.GetTableName()!;
            foreach (var propriete in entite.GetDeclaredProperties())
            {
                var typeEnum = Nullable.GetUnderlyingType(propriete.ClrType) ?? propriete.ClrType;
                if (!typeEnum.IsEnum || propriete.IsForeignKey() || propriete.IsKey()
                    || typeEnum.IsDefined(typeof(FlagsAttribute), inherit: false))
                {
                    continue;
                }

                var colonne = propriete.GetColumnName();
                entite.AddCheckConstraint($"CK_{table}_{colonne}", $"[{colonne}] IN ({ValeursSql(propriete, typeEnum)})");
            }
        }
    }

    private static string ValeursSql(IMutableProperty propriete, Type typeEnum) =>
        (propriete.GetProviderClrType() ?? propriete.GetValueConverter()?.ProviderClrType) == typeof(string)
            ? string.Join(", ", Enum.GetNames(typeEnum).Select(n => $"N'{n}'"))
            : string.Join(", ", Enum.GetValues(typeEnum).Cast<object>()
                .Select(v => Convert.ToInt64(v, System.Globalization.CultureInfo.InvariantCulture)));

    private static void Referentiel<TReferentiel, TCode>(ModelBuilder builder, string table)
        where TReferentiel : ValeurReferentielle<TCode>
        where TCode : struct, Enum =>
        builder.Entity<TReferentiel>(b =>
        {
            b.ToTable(table);
            b.HasKey(r => r.Code);
            b.Property(r => r.Code).HasConversion<string>().HasMaxLength(LongueurCode);
            b.Property(r => r.LibelleFr).HasMaxLength(ValeurReferentielle<TCode>.LongueurMaxLibelle).IsRequired();
            b.Property(r => r.LibelleEn).HasMaxLength(ValeurReferentielle<TCode>.LongueurMaxLibelle).IsRequired();
        });

    /// <summary>Clé étrangère vers un référentiel ; suppression interdite tant qu'une ligne le porte.</summary>
    private static void Lier<TEntite, TReferentiel>(ModelBuilder builder, Expression<Func<TEntite, object?>> colonne)
        where TEntite : class
        where TReferentiel : class =>
        builder.Entity<TEntite>()
            .HasOne<TReferentiel>()
            .WithMany()
            .HasForeignKey(colonne)
            .OnDelete(DeleteBehavior.Restrict);
}
