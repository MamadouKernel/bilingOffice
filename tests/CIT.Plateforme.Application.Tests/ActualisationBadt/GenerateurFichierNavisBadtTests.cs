using System;
using System.Collections.Generic;
using System.Linq;
using CIT.Plateforme.Application.ActualisationBadt;
using Xunit;

namespace CIT.Plateforme.Application.Tests.ActualisationBadt;

public class GenerateurFichierNavisBadtTests
{
    // Construit une ligne Navis de 34 colonnes : conteneur en colonne 8 (index 7),
    // date de validité en colonne 20 (index 19), « ADD » en colonne 24 (index 23).
    private static string Ligne(string conteneur, string dateValidite)
    {
        var cols = Enumerable.Repeat(string.Empty, 34).ToArray();
        cols[0] = "20789793";
        cols[7] = conteneur;
        cols[19] = dateValidite;
        cols[23] = "ADD";
        cols[33] = "I1 - Riz";
        return string.Join(';', cols);
    }

    [Fact]
    public void Injecte_la_colonne_T_du_conteneur_accorde_et_laisse_les_autres_intacts()
    {
        var accorde = Ligne("MRKU9939436", "2026-10-16");
        var nonAccorde = Ligne("CAAU6666138", "2026-10-16");
        var csv = accorde + "\r\n" + nonAccorde;

        var dates = new Dictionary<string, DateOnly> { ["MRKU9939436"] = new DateOnly(2026, 10, 30) };

        var resultat = GenerateurFichierNavisBadt.Injecter(csv, dates);
        var lignes = resultat.Contenu.Split("\r\n");

        Assert.Equal("2026-10-30", lignes[0].Split(';')[19]); // injecté
        Assert.Equal("2026-10-16", lignes[1].Split(';')[19]); // intact
        Assert.Equal(34, lignes[0].Split(';').Length);        // structure préservée
        Assert.Equal("ADD", lignes[0].Split(';')[23]);        // colonne 24 intacte
        Assert.Equal("MRKU9939436", lignes[0].Split(';')[7]); // conteneur intact
        Assert.Equal(1, resultat.LignesModifiees);
        Assert.Equal(';', resultat.Separateur);
    }

    [Fact]
    public void Detecte_un_separateur_virgule_et_injecte_quand_meme()
    {
        // Même ligne mais séparée par des virgules : le séparateur n'est pas forcément « ; ».
        var csv = Ligne("MRKU9939436", "2026-10-16").Replace(';', ',');
        var dates = new Dictionary<string, DateOnly> { ["MRKU9939436"] = new DateOnly(2026, 10, 30) };

        var resultat = GenerateurFichierNavisBadt.Injecter(csv, dates);

        Assert.Equal(',', resultat.Separateur);
        Assert.Equal(1, resultat.LignesModifiees);
        Assert.Equal("2026-10-30", resultat.Contenu.Split(',')[19]);
    }

    [Fact]
    public void Rapproche_le_conteneur_sans_tenir_compte_de_la_casse_ni_des_espaces()
    {
        var csv = Ligne(" mrku9939436 ", "2026-10-16");
        var dates = new Dictionary<string, DateOnly> { ["MRKU9939436"] = new DateOnly(2026, 10, 30) };

        var resultat = GenerateurFichierNavisBadt.Injecter(csv, dates);

        Assert.Equal("2026-10-30", resultat.Contenu.Split(';')[19]);
    }

    [Fact]
    public void Laisse_les_lignes_vides_inchangees()
    {
        var csv = ";;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;";
        var resultat = GenerateurFichierNavisBadt.Injecter(csv, new Dictionary<string, DateOnly>());
        Assert.Equal(csv, resultat.Contenu);
        Assert.Equal(0, resultat.LignesModifiees);
    }
}
