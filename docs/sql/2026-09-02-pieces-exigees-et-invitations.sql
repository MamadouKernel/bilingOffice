BEGIN TRANSACTION;
CREATE TABLE [InvitationsCollaborateur] (
    [Id] uniqueidentifier NOT NULL,
    [SocieteId] uniqueidentifier NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [Jeton] nvarchar(64) NOT NULL,
    [CreeParId] uniqueidentifier NOT NULL,
    [Etat] nvarchar(32) NOT NULL,
    [DateCreationUtc] datetime2 NOT NULL,
    [DateExpirationUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_InvitationsCollaborateur] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InvitationsCollaborateur_Societes_SocieteId] FOREIGN KEY ([SocieteId]) REFERENCES [Societes] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [JustificatifsDemandeCompte] (
    [Id] uniqueidentifier NOT NULL,
    [SocieteId] uniqueidentifier NOT NULL,
    [PieceCode] nvarchar(64) NOT NULL,
    [NomFichier] nvarchar(512) NOT NULL,
    [TypeContenu] nvarchar(128) NOT NULL,
    [TailleOctets] bigint NOT NULL,
    [CleStockage] nvarchar(512) NOT NULL,
    [DateDepotUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_JustificatifsDemandeCompte] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_JustificatifsDemandeCompte_Societes_SocieteId] FOREIGN KEY ([SocieteId]) REFERENCES [Societes] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PiecesExigeesCompte] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(64) NOT NULL,
    [Libelle] nvarchar(256) NOT NULL,
    [Obligatoire] bit NOT NULL,
    [EstActif] bit NOT NULL,
    [Ordre] int NOT NULL,
    CONSTRAINT [PK_PiecesExigeesCompte] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_InvitationsCollaborateur_Jeton] ON [InvitationsCollaborateur] ([Jeton]);

CREATE INDEX [IX_InvitationsCollaborateur_SocieteId] ON [InvitationsCollaborateur] ([SocieteId]);

CREATE INDEX [IX_JustificatifsDemandeCompte_SocieteId] ON [JustificatifsDemandeCompte] ([SocieteId]);

CREATE UNIQUE INDEX [IX_JustificatifsDemandeCompte_SocieteId_PieceCode] ON [JustificatifsDemandeCompte] ([SocieteId], [PieceCode]);

CREATE UNIQUE INDEX [IX_PiecesExigeesCompte_Code] ON [PiecesExigeesCompte] ([Code]);

CREATE INDEX [IX_PiecesExigeesCompte_EstActif] ON [PiecesExigeesCompte] ([EstActif]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260902094053_AjoutPiecesExigeesEtInvitationsCollaborateur', N'10.0.11');

COMMIT;
GO

