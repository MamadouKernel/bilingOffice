using CIT.Plateforme.Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// TPS-01 / TPS-05 — tient à jour le journal des passages en file, à partir des changements
/// observés sur les tickets au moment de l'enregistrement.
///
/// POURQUOI ICI, ET NON DANS CHAQUE CAS D'USAGE : la création, le transfert, la prise en charge et
/// la clôture d'un ticket sont invoqués depuis <b>34 endroits</b> de la couche Application. Les
/// modifier un à un aurait été long, et surtout faillible : il aurait suffi qu'un futur cas
/// d'usage oublie l'appel pour que la chronologie redevienne trouée — exactement le défaut que
/// cette entité corrige.
///
/// Le point d'enregistrement est en revanche <b>unique</b>, et EF Core y expose les valeurs
/// d'origine de chaque entité suivie. Comparer l'ancienne et la nouvelle file donne le fait à
/// enregistrer, sans qu'aucun appelant ait à y penser.
/// </summary>
internal static class SuiviPassagesEnFile
{
    /// <summary>
    /// À appeler juste avant <c>SaveChanges</c>. Ajoute et ferme les passages nécessaires dans le
    /// même graphe de suivi, pour qu'ils soient enregistrés dans la même transaction que le ticket.
    /// </summary>
    public static void Synchroniser(PlateformeDbContext dbContext, DateTime maintenantUtc)
    {
        var tickets = dbContext.ChangeTracker
            .Entries<Ticket>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .ToList();

        foreach (var entree in tickets)
        {
            if (entree.State == EntityState.Added)
            {
                OuvrirPremierPassage(dbContext, entree.Entity, maintenantUtc);
                continue;
            }

            SynchroniserTicketModifie(dbContext, entree, maintenantUtc);
        }
    }

    private static void OuvrirPremierPassage(PlateformeDbContext dbContext, Ticket ticket, DateTime maintenantUtc)
    {
        dbContext.PassagesEnFile.Add(
            PassageEnFile.Ouvrir(ticket.Id, ticket.FileCouranteId, rang: 1, ticket.DateCreationUtc));

        // Un ticket peut être créé déjà pris en charge — la prise en charge est alors portée par
        // le même passage.
        if (ticket.DatePriseEnChargeUtc is DateTime pec && ticket.OperateurAssigneId is Guid operateur)
        {
            DernierPassageOuvert(dbContext, ticket.Id)?.PrendreEnCharge(operateur, pec);
        }
    }

    private static void SynchroniserTicketModifie(
        PlateformeDbContext dbContext, EntityEntry<Ticket> entree, DateTime maintenantUtc)
    {
        var ticket = entree.Entity;

        var fileAvant = entree.Property(t => t.FileCouranteId).OriginalValue;
        var fileApres = ticket.FileCouranteId;

        var clotureAvant = entree.Property(t => t.DateClotureUtc).OriginalValue;
        var clotureApres = ticket.DateClotureUtc;

        var pecAvant = entree.Property(t => t.DatePriseEnChargeUtc).OriginalValue;
        var pecApres = ticket.DatePriseEnChargeUtc;

        var passageCourant = DernierPassageOuvert(dbContext, ticket.Id);

        // 1. Changement de file : on ferme l'étape traversée et on en ouvre une nouvelle.
        if (fileAvant != fileApres)
        {
            var instant = ticket.DateArriveeEnFileUtc;
            passageCourant?.Fermer(instant);

            var rang = (passageCourant?.Rang ?? RangMaximumConnu(dbContext, ticket.Id)) + 1;
            dbContext.PassagesEnFile.Add(PassageEnFile.Ouvrir(ticket.Id, fileApres, rang, instant));

            // La prise en charge éventuelle porte sur le NOUVEAU passage, pas sur celui qui vient
            // d'être fermé.
            passageCourant = DernierPassageOuvert(dbContext, ticket.Id);
        }

        // 2. Prise en charge nouvellement enregistrée.
        if (pecAvant is null && pecApres is DateTime priseEnCharge
            && ticket.OperateurAssigneId is Guid operateurId)
        {
            passageCourant?.PrendreEnCharge(operateurId, priseEnCharge);
        }

        // 3. Clôture : l'étape courante se termine avec le ticket.
        if (clotureAvant is null && clotureApres is DateTime cloture)
        {
            passageCourant?.Fermer(cloture);
        }

        // 4. Réouverture : le ticket repart, une nouvelle étape commence.
        if (clotureAvant is not null && clotureApres is null)
        {
            var rang = RangMaximumConnu(dbContext, ticket.Id) + 1;
            dbContext.PassagesEnFile.Add(
                PassageEnFile.Ouvrir(ticket.Id, fileApres, rang, ticket.DateArriveeEnFileUtc));
        }
    }

    /// <summary>
    /// Le passage encore ouvert du ticket. La recherche porte d'abord sur le graphe suivi — un
    /// passage ajouté dans le même enregistrement n'est pas encore en base.
    /// </summary>
    private static PassageEnFile? DernierPassageOuvert(PlateformeDbContext dbContext, Guid ticketId)
    {
        var local = dbContext.PassagesEnFile.Local
            .Where(p => p.TicketId == ticketId && p.EstEnCours)
            .OrderByDescending(p => p.Rang)
            .FirstOrDefault();

        if (local is not null)
        {
            return local;
        }

        return dbContext.PassagesEnFile
            .Where(p => p.TicketId == ticketId && p.DateSortieUtc == null)
            .OrderByDescending(p => p.Rang)
            .FirstOrDefault();
    }

    private static int RangMaximumConnu(PlateformeDbContext dbContext, Guid ticketId)
    {
        var rangLocal = dbContext.PassagesEnFile.Local
            .Where(p => p.TicketId == ticketId)
            .Select(p => p.Rang)
            .DefaultIfEmpty(0)
            .Max();

        var rangPersiste = dbContext.PassagesEnFile
            .Where(p => p.TicketId == ticketId)
            .Select(p => (int?)p.Rang)
            .Max() ?? 0;

        return Math.Max(rangLocal, rangPersiste);
    }
}
