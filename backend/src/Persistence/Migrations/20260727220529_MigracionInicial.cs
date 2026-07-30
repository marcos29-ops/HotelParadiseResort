using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelParadiseResort.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MigracionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Identificacion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, collation: "Latin1_General_CI_AI"),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false, collation: "Latin1_General_CI_AI"),
                    Apellidos = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false, collation: "Latin1_General_CI_AI"),
                    Correo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true, collation: "Latin1_General_CI_AI"),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true, collation: "Latin1_General_CI_AI"),
                    Nacionalidad = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    FechaNacimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cliente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicioAdicional",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false, collation: "Latin1_General_CI_AI"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true, collation: "Latin1_General_CI_AI"),
                    PrecioBase = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicioAdicional", x => x.Id);
                    table.CheckConstraint("CK_ServicioAdicional_Precio", "[PrecioBase] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "TipoHabitacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Latin1_General_CI_AI"),
                    Descripcion = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    TarifaBasePorNoche = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CapacidadMaxima = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoHabitacion", x => x.Id);
                    table.CheckConstraint("CK_TipoHabitacion_Capacidad", "[CapacidadMaxima] > 0");
                    table.CheckConstraint("CK_TipoHabitacion_Tarifa", "[TarifaBasePorNoche] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false, collation: "Latin1_General_CI_AI"),
                    NombreUsuario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, collation: "Latin1_General_CI_AI"),
                    ContrasenaHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false, collation: "Latin1_General_CI_AI"),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Habitacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, collation: "Latin1_General_CI_AI"),
                    Piso = table.Column<int>(type: "int", nullable: false),
                    TipoHabitacionId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Habitacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Habitacion_TipoHabitacion_TipoHabitacionId",
                        column: x => x.TipoHabitacionId,
                        principalTable: "TipoHabitacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistorialCliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialCliente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialCliente_Cliente_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistorialCliente_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reserva",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, collation: "Latin1_General_CI_AI"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    HabitacionId = table.Column<int>(type: "int", nullable: false),
                    UsuarioRegistroId = table.Column<int>(type: "int", nullable: false),
                    FechaEntrada = table.Column<DateTime>(type: "date", nullable: false),
                    FechaSalida = table.Column<DateTime>(type: "date", nullable: false),
                    CantidadHuespedes = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    CanalOrigen = table.Column<int>(type: "int", nullable: false),
                    MontoEstimado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCancelacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoCancelacion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reserva", x => x.Id);
                    table.CheckConstraint("CK_Reserva_Fechas", "[FechaSalida] >= [FechaEntrada]");
                    table.CheckConstraint("CK_Reserva_Huespedes", "[CantidadHuespedes] > 0");
                    table.CheckConstraint("CK_Reserva_Monto", "[MontoEstimado] >= 0");
                    table.ForeignKey(
                        name: "FK_Reserva_Cliente_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reserva_Habitacion_HabitacionId",
                        column: x => x.HabitacionId,
                        principalTable: "Habitacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reserva_Usuario_UsuarioRegistroId",
                        column: x => x.UsuarioRegistroId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Estadia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservaId = table.Column<int>(type: "int", nullable: false),
                    HabitacionId = table.Column<int>(type: "int", nullable: false),
                    FechaCheckIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCheckOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioCheckInId = table.Column<int>(type: "int", nullable: false),
                    UsuarioCheckOutId = table.Column<int>(type: "int", nullable: true),
                    CantidadHuespedes = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estadia", x => x.Id);
                    table.CheckConstraint("CK_Estadia_FechaCheckOut", "[FechaCheckOut] IS NULL OR [FechaCheckOut] >= [FechaCheckIn]");
                    table.CheckConstraint("CK_Estadia_Huespedes", "[CantidadHuespedes] > 0");
                    table.ForeignKey(
                        name: "FK_Estadia_Habitacion_HabitacionId",
                        column: x => x.HabitacionId,
                        principalTable: "Habitacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Estadia_Reserva_ReservaId",
                        column: x => x.ReservaId,
                        principalTable: "Reserva",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Estadia_Usuario_UsuarioCheckInId",
                        column: x => x.UsuarioCheckInId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Estadia_Usuario_UsuarioCheckOutId",
                        column: x => x.UsuarioCheckOutId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Consumo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstadiaId = table.Column<int>(type: "int", nullable: false),
                    ServicioAdicionalId = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false, collation: "Latin1_General_CI_AI"),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaConsumo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioRegistroId = table.Column<int>(type: "int", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consumo", x => x.Id);
                    table.CheckConstraint("CK_Consumo_Cantidad", "[Cantidad] > 0");
                    table.CheckConstraint("CK_Consumo_PrecioUnitario", "[PrecioUnitario] >= 0");
                    table.ForeignKey(
                        name: "FK_Consumo_Estadia_EstadiaId",
                        column: x => x.EstadiaId,
                        principalTable: "Estadia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Consumo_ServicioAdicional_ServicioAdicionalId",
                        column: x => x.ServicioAdicionalId,
                        principalTable: "ServicioAdicional",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consumo_Usuario_UsuarioRegistroId",
                        column: x => x.UsuarioRegistroId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Factura",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, collation: "Latin1_General_CI_AI"),
                    EstadiaId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Noches = table.Column<int>(type: "int", nullable: false),
                    TarifaPorNoche = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EstrategiaTarifa = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SubtotalHospedaje = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SubtotalConsumos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    JustificacionDescuento = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MetodoPago = table.Column<int>(type: "int", nullable: true),
                    EstadoPago = table.Column<int>(type: "int", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioEmisionId = table.Column<int>(type: "int", nullable: false),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factura", x => x.Id);
                    table.CheckConstraint("CK_Factura_Descuento", "[Descuento] <= [SubtotalHospedaje] + [SubtotalConsumos]");
                    table.CheckConstraint("CK_Factura_Montos", "[SubtotalHospedaje] >= 0 AND [SubtotalConsumos] >= 0 AND [Descuento] >= 0 AND [Total] >= 0");
                    table.ForeignKey(
                        name: "FK_Factura_Cliente_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Factura_Estadia_EstadiaId",
                        column: x => x.EstadiaId,
                        principalTable: "Estadia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Factura_Usuario_UsuarioEmisionId",
                        column: x => x.UsuarioEmisionId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetalleFactura",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FacturaId = table.Column<int>(type: "int", nullable: false),
                    Concepto = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EsHospedaje = table.Column<bool>(type: "bit", nullable: false),
                    FechaConsumo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleFactura", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetalleFactura_Factura_FacturaId",
                        column: x => x.FacturaId,
                        principalTable: "Factura",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_Apellidos_Nombre",
                table: "Cliente",
                columns: new[] { "Apellidos", "Nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_Identificacion",
                table: "Cliente",
                column: "Identificacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consumo_Estadia",
                table: "Consumo",
                column: "EstadiaId");

            migrationBuilder.CreateIndex(
                name: "IX_Consumo_Fecha",
                table: "Consumo",
                column: "FechaConsumo");

            migrationBuilder.CreateIndex(
                name: "IX_Consumo_ServicioAdicionalId",
                table: "Consumo",
                column: "ServicioAdicionalId");

            migrationBuilder.CreateIndex(
                name: "IX_Consumo_UsuarioRegistroId",
                table: "Consumo",
                column: "UsuarioRegistroId");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleFactura_Factura",
                table: "DetalleFactura",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_Estadia_FechaCheckIn",
                table: "Estadia",
                column: "FechaCheckIn");

            migrationBuilder.CreateIndex(
                name: "IX_Estadia_Habitacion_Estado",
                table: "Estadia",
                columns: new[] { "HabitacionId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Estadia_Reserva",
                table: "Estadia",
                column: "ReservaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Estadia_UsuarioCheckInId",
                table: "Estadia",
                column: "UsuarioCheckInId");

            migrationBuilder.CreateIndex(
                name: "IX_Estadia_UsuarioCheckOutId",
                table: "Estadia",
                column: "UsuarioCheckOutId");

            migrationBuilder.CreateIndex(
                name: "IX_Factura_ClienteId",
                table: "Factura",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Factura_Estadia",
                table: "Factura",
                column: "EstadiaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Factura_FechaEmision_EstadoPago",
                table: "Factura",
                columns: new[] { "FechaEmision", "EstadoPago" });

            migrationBuilder.CreateIndex(
                name: "IX_Factura_Numero",
                table: "Factura",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Factura_UsuarioEmisionId",
                table: "Factura",
                column: "UsuarioEmisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Habitacion_Estado_Tipo",
                table: "Habitacion",
                columns: new[] { "Estado", "TipoHabitacionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Habitacion_Numero",
                table: "Habitacion",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Habitacion_TipoHabitacionId",
                table: "Habitacion",
                column: "TipoHabitacionId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialCliente_Cliente_Fecha",
                table: "HistorialCliente",
                columns: new[] { "ClienteId", "FechaRegistro" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialCliente_UsuarioId",
                table: "HistorialCliente",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Reserva_Cliente",
                table: "Reserva",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Reserva_Codigo",
                table: "Reserva",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reserva_Estado_FechaEntrada",
                table: "Reserva",
                columns: new[] { "Estado", "FechaEntrada" });

            migrationBuilder.CreateIndex(
                name: "IX_Reserva_Habitacion_Fechas_Estado",
                table: "Reserva",
                columns: new[] { "HabitacionId", "FechaEntrada", "FechaSalida", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Reserva_UsuarioRegistroId",
                table: "Reserva",
                column: "UsuarioRegistroId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicioAdicional_Nombre",
                table: "ServicioAdicional",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicioAdicional_Tipo",
                table: "ServicioAdicional",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_TipoHabitacion_Nombre",
                table: "TipoHabitacion",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_Correo",
                table: "Usuario",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_NombreUsuario",
                table: "Usuario",
                column: "NombreUsuario",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Consumo");

            migrationBuilder.DropTable(
                name: "DetalleFactura");

            migrationBuilder.DropTable(
                name: "HistorialCliente");

            migrationBuilder.DropTable(
                name: "ServicioAdicional");

            migrationBuilder.DropTable(
                name: "Factura");

            migrationBuilder.DropTable(
                name: "Estadia");

            migrationBuilder.DropTable(
                name: "Reserva");

            migrationBuilder.DropTable(
                name: "Cliente");

            migrationBuilder.DropTable(
                name: "Habitacion");

            migrationBuilder.DropTable(
                name: "Usuario");

            migrationBuilder.DropTable(
                name: "TipoHabitacion");
        }
    }
}
