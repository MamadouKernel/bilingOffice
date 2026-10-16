BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016103512_AjoutEscaladeLigneBadt'
)
BEGIN
    ALTER TABLE [LignesActualisationBadt] ADD [Escalade] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016103512_AjoutEscaladeLigneBadt'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261016103512_AjoutEscaladeLigneBadt', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016112941_AjoutMesuresActualisationBadt'
)
BEGIN
    CREATE TABLE [MesuresActualisationBadt] (
        [Id] uniqueidentifier NOT NULL,
        [TicketId] uniqueidentifier NOT NULL,
        [LigneId] uniqueidentifier NULL,
        [NumeroBl] nvarchar(64) NOT NULL,
        [NumeroConteneur] nvarchar(32) NOT NULL,
        [Type] nvarchar(32) NOT NULL,
        [ActeurId] uniqueidentifier NOT NULL,
        [Cause] nvarchar(32) NULL,
        [NombreJoursAccordes] int NULL,
        [HorodatageUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_MesuresActualisationBadt] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MesuresActualisationBadt_Cause] CHECK ([Cause] IN (N'Terminale', N'NonTerminale')),
        CONSTRAINT [CK_MesuresActualisationBadt_Type] CHECK ([Type] IN (N'Creation', N'Depliage', N'Qualification', N'RejetAgent', N'Accord', N'Refus', N'Renvoi'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016112941_AjoutMesuresActualisationBadt'
)
BEGIN
    CREATE INDEX [IX_MesuresActualisationBadt_HorodatageUtc] ON [MesuresActualisationBadt] ([HorodatageUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016112941_AjoutMesuresActualisationBadt'
)
BEGIN
    CREATE INDEX [IX_MesuresActualisationBadt_LigneId] ON [MesuresActualisationBadt] ([LigneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261016112941_AjoutMesuresActualisationBadt'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261016112941_AjoutMesuresActualisationBadt', N'10.0.11');
END;

COMMIT;
GO

