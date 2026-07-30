IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE TABLE [Cliente] (
        [Id] int NOT NULL IDENTITY,
        [Identificacion] nvarchar(30) COLLATE Latin1_General_CI_AI NOT NULL,
        [Nombre] nvarchar(80) COLLATE Latin1_General_CI_AI NOT NULL,
        [Apellidos] nvarchar(120) COLLATE Latin1_General_CI_AI NOT NULL,
        [Correo] nvarchar(150) COLLATE Latin1_General_CI_AI NULL,
        [Telefono] nvarchar(30) COLLATE Latin1_General_CI_AI NULL,
        [Nacionalidad] nvarchar(80) NULL,
        [FechaNacimiento] datetime2 NULL,
        [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [VersionFila] rowversion NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [FechaModificacion] datetime2 NULL,
        CONSTRAINT [PK_Cliente] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE TABLE [ServicioAdicional] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(120) COLLATE Latin1_General_CI_AI NOT NULL,
        [Tipo] int NOT NULL,
        [Descripcion] nvarchar(400) COLLATE Latin1_General_CI_AI NULL,
        [PrecioBase] decimal(18,2) NOT NULL,
        [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [VersionFila] rowversion NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [FechaModificacion] datetime2 NULL,
        CONSTRAINT [PK_ServicioAdicional] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ServicioAdicional_Precio] CHECK ([PrecioBase] >= 0)
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE TABLE [TipoHabitacion] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(100) COLLATE Latin1_General_CI_AI NOT NULL,
        [Descripcion] nvarchar(400) NULL,
        [TarifaBasePorNoche] decimal(18,2) NOT NULL,
        [CapacidadMaxima] int NOT NULL,
        [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [VersionFila] rowversion NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [FechaModificacion] datetime2 NULL,
        CONSTRAINT [PK_TipoHabitacion] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_TipoHabitacion_Capacidad] CHECK ([CapacidadMaxima] > 0),
        CONSTRAINT [CK_TipoHabitacion_Tarifa] CHECK ([TarifaBasePorNoche] >= 0)
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE TABLE [Usuario] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(120) COLLATE Latin1_General_CI_AI NOT NULL,
        [NombreUsuario] nvarchar(50) COLLATE Latin1_General_CI_AI NOT NULL,
        [ContrasenaHash] nvarchar(500) NOT NULL,
        [Correo] nvarchar(150) COLLATE Latin1_General_CI_AI NOT NULL,
        [Rol] int NOT NULL,
        [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [UltimoAcceso] datetime2 NULL,
        [VersionFila] rowversion NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [FechaModificacion] datetime2 NULL,
        CONSTRAINT [PK_Usuario] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE TABLE [Habitacion] (
        [Id] int NOT NULL IDENTITY,
        [Numero] nvarchar(10) COLLATE Latin1_General_CI_AI NOT NULL,
        [Piso] int NOT NULL,
        [TipoHabitacionId] int NOT NULL,
        [Estado] int NOT NULL,
        [Observaciones] nvarchar(500) NULL,
        [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
        [VersionFila] rowversion NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [FechaModificacion] datetime2 NULL,
        CONSTRAINT [PK_Habitacion] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Habitacion_TipoHabitacion_TipoHabitacionId] FOREIGN KEY ([TipoHabitacionId]) REFERENCES [TipoHabitacion] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE TABLE [HistorialCliente] (
        [Id] int NOT NULL IDENTITY,
        [ClienteId] int NOT NULL,
        [Accion] nvarchar(50) NOT NULL,
        [Detalle] nvarchar(1000) NOT NULL,
        [UsuarioId] int NOT NULL,
        [FechaRegistro] datetime2 NOT NULL,
        [VersionFila] rowversion NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [FechaModificacion] datetime2 NULL,
        CONSTRAINT [PK_HistorialCliente] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HistorialCliente_Cliente_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Cliente] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_HistorialCliente_Usuario_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuario] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE TABLE [Reserva] (
        [Id] int NOT NULL IDENTITY,
        [Codigo] nvarchar(20) COLLATE Latin1_General_CI_AI NOT NULL,
        [ClienteId] int NOT NULL,
        [HabitacionId] int NOT NULL,
        [UsuarioRegistroId] int NOT NULL,
        [FechaEntrada] date NOT NULL,
        [FechaSalida] date NOT NULL,
        [CantidadHuespedes] int NOT NULL,
        [Estado] int NOT NULL,
        [CanalOrigen] int NOT NULL,
        [MontoEstimado] decimal(18,2) NOT NULL,
        [Observaciones] nvarchar(500) NULL,
        [FechaCancelacion] datetime2 NULL,
        [MotivoCancelacion] nvarchar(300) NULL,
        [VersionFila] rowversion NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [FechaModificacion] datetime2 NULL,
        CONSTRAINT [PK_Reserva] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Reserva_Fechas] CHECK ([FechaSalida] >= [FechaEntrada]),
        CONSTRAINT [CK_Reserva_Huespedes] CHECK ([CantidadHuespedes] > 0),
        CONSTRAINT [CK_Reserva_Monto] CHECK ([MontoEstimado] >= 0),
        CONSTRAINT [FK_Reserva_Cliente_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Cliente] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Reserva_Habitacion_HabitacionId] FOREIGN KEY ([HabitacionId]) REFERENCES [Habitacion] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Reserva_Usuario_UsuarioRegistroId] FOREIGN KEY ([UsuarioRegistroId]) REFERENCES [Usuario] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE TABLE [Estadia] (
        [Id] int NOT NULL IDENTITY,
        [ReservaId] int NOT NULL,
        [HabitacionId] int NOT NULL,
        [FechaCheckIn] datetime2 NOT NULL,
        [FechaCheckOut] datetime2 NULL,
        [UsuarioCheckInId] int NOT NULL,
        [UsuarioCheckOutId] int NULL,
        [CantidadHuespedes] int NOT NULL,
        [Estado] int NOT NULL,
        [Observaciones] nvarchar(500) NULL,
        [VersionFila] rowversion NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [FechaModificacion] datetime2 NULL,
        CONSTRAINT [PK_Estadia] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Estadia_FechaCheckOut] CHECK ([FechaCheckOut] IS NULL OR [FechaCheckOut] >= [FechaCheckIn]),
        CONSTRAINT [CK_Estadia_Huespedes] CHECK ([CantidadHuespedes] > 0),
        CONSTRAINT [FK_Estadia_Habitacion_HabitacionId] FOREIGN KEY ([HabitacionId]) REFERENCES [Habitacion] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Estadia_Reserva_ReservaId] FOREIGN KEY ([ReservaId]) REFERENCES [Reserva] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Estadia_Usuario_UsuarioCheckInId] FOREIGN KEY ([UsuarioCheckInId]) REFERENCES [Usuario] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Estadia_Usuario_UsuarioCheckOutId] FOREIGN KEY ([UsuarioCheckOutId]) REFERENCES [Usuario] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE TABLE [Consumo] (
        [Id] int NOT NULL IDENTITY,
        [EstadiaId] int NOT NULL,
        [ServicioAdicionalId] int NOT NULL,
        [Descripcion] nvarchar(300) COLLATE Latin1_General_CI_AI NOT NULL,
        [Cantidad] int NOT NULL,
        [PrecioUnitario] decimal(18,2) NOT NULL,
        [FechaConsumo] datetime2 NOT NULL,
        [UsuarioRegistroId] int NOT NULL,
        [VersionFila] rowversion NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [FechaModificacion] datetime2 NULL,
        CONSTRAINT [PK_Consumo] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Consumo_Cantidad] CHECK ([Cantidad] > 0),
        CONSTRAINT [CK_Consumo_PrecioUnitario] CHECK ([PrecioUnitario] >= 0),
        CONSTRAINT [FK_Consumo_Estadia_EstadiaId] FOREIGN KEY ([EstadiaId]) REFERENCES [Estadia] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Consumo_ServicioAdicional_ServicioAdicionalId] FOREIGN KEY ([ServicioAdicionalId]) REFERENCES [ServicioAdicional] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Consumo_Usuario_UsuarioRegistroId] FOREIGN KEY ([UsuarioRegistroId]) REFERENCES [Usuario] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE TABLE [Factura] (
        [Id] int NOT NULL IDENTITY,
        [Numero] nvarchar(20) COLLATE Latin1_General_CI_AI NOT NULL,
        [EstadiaId] int NOT NULL,
        [ClienteId] int NOT NULL,
        [FechaEmision] datetime2 NOT NULL,
        [Noches] int NOT NULL,
        [TarifaPorNoche] decimal(18,2) NOT NULL,
        [EstrategiaTarifa] nvarchar(40) NOT NULL,
        [SubtotalHospedaje] decimal(18,2) NOT NULL,
        [SubtotalConsumos] decimal(18,2) NOT NULL,
        [Descuento] decimal(18,2) NOT NULL,
        [JustificacionDescuento] nvarchar(300) NULL,
        [Total] decimal(18,2) NOT NULL,
        [MetodoPago] int NULL,
        [EstadoPago] int NOT NULL,
        [FechaPago] datetime2 NULL,
        [UsuarioEmisionId] int NOT NULL,
        [VersionFila] rowversion NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [FechaModificacion] datetime2 NULL,
        CONSTRAINT [PK_Factura] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Factura_Descuento] CHECK ([Descuento] <= [SubtotalHospedaje] + [SubtotalConsumos]),
        CONSTRAINT [CK_Factura_Montos] CHECK ([SubtotalHospedaje] >= 0 AND [SubtotalConsumos] >= 0 AND [Descuento] >= 0 AND [Total] >= 0),
        CONSTRAINT [FK_Factura_Cliente_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Cliente] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Factura_Estadia_EstadiaId] FOREIGN KEY ([EstadiaId]) REFERENCES [Estadia] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Factura_Usuario_UsuarioEmisionId] FOREIGN KEY ([UsuarioEmisionId]) REFERENCES [Usuario] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE TABLE [DetalleFactura] (
        [Id] int NOT NULL IDENTITY,
        [FacturaId] int NOT NULL,
        [Concepto] nvarchar(300) NOT NULL,
        [Cantidad] int NOT NULL,
        [PrecioUnitario] decimal(18,2) NOT NULL,
        [Subtotal] decimal(18,2) NOT NULL,
        [EsHospedaje] bit NOT NULL,
        [FechaConsumo] datetime2 NULL,
        [VersionFila] rowversion NULL,
        [FechaCreacion] datetime2 NOT NULL,
        [FechaModificacion] datetime2 NULL,
        CONSTRAINT [PK_DetalleFactura] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DetalleFactura_Factura_FacturaId] FOREIGN KEY ([FacturaId]) REFERENCES [Factura] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_Cliente_Apellidos_Nombre] ON [Cliente] ([Apellidos], [Nombre]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Cliente_Identificacion] ON [Cliente] ([Identificacion]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_Consumo_Estadia] ON [Consumo] ([EstadiaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_Consumo_Fecha] ON [Consumo] ([FechaConsumo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_Consumo_ServicioAdicionalId] ON [Consumo] ([ServicioAdicionalId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_Consumo_UsuarioRegistroId] ON [Consumo] ([UsuarioRegistroId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_DetalleFactura_Factura] ON [DetalleFactura] ([FacturaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_Estadia_FechaCheckIn] ON [Estadia] ([FechaCheckIn]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_Estadia_Habitacion_Estado] ON [Estadia] ([HabitacionId], [Estado]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Estadia_Reserva] ON [Estadia] ([ReservaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_Estadia_UsuarioCheckInId] ON [Estadia] ([UsuarioCheckInId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_Estadia_UsuarioCheckOutId] ON [Estadia] ([UsuarioCheckOutId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_Factura_ClienteId] ON [Factura] ([ClienteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Factura_Estadia] ON [Factura] ([EstadiaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_Factura_FechaEmision_EstadoPago] ON [Factura] ([FechaEmision], [EstadoPago]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Factura_Numero] ON [Factura] ([Numero]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_Factura_UsuarioEmisionId] ON [Factura] ([UsuarioEmisionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_Habitacion_Estado_Tipo] ON [Habitacion] ([Estado], [TipoHabitacionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Habitacion_Numero] ON [Habitacion] ([Numero]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_Habitacion_TipoHabitacionId] ON [Habitacion] ([TipoHabitacionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_HistorialCliente_Cliente_Fecha] ON [HistorialCliente] ([ClienteId], [FechaRegistro]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_HistorialCliente_UsuarioId] ON [HistorialCliente] ([UsuarioId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_Reserva_Cliente] ON [Reserva] ([ClienteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Reserva_Codigo] ON [Reserva] ([Codigo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_Reserva_Estado_FechaEntrada] ON [Reserva] ([Estado], [FechaEntrada]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_Reserva_Habitacion_Fechas_Estado] ON [Reserva] ([HabitacionId], [FechaEntrada], [FechaSalida], [Estado]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_Reserva_UsuarioRegistroId] ON [Reserva] ([UsuarioRegistroId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ServicioAdicional_Nombre] ON [ServicioAdicional] ([Nombre]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE INDEX [IX_ServicioAdicional_Tipo] ON [ServicioAdicional] ([Tipo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TipoHabitacion_Nombre] ON [TipoHabitacion] ([Nombre]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Usuario_Correo] ON [Usuario] ([Correo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Usuario_NombreUsuario] ON [Usuario] ([NombreUsuario]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727220529_MigracionInicial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260727220529_MigracionInicial', N'8.0.11');
END;
GO

COMMIT;
GO

