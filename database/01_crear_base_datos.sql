/* ===========================================================================
   Paradise Resort — Sistema de Gestión Hotelera
   Script 01: creación de la base de datos

   Ejecutar ANTES de 02_esquema.sql.

   La collation Latin1_General_CI_AI es insensible a mayúsculas y a acentos: sin
   ella, el buscador de clientes no encontraría a "Pérez" al escribir "Perez"
   ni a "Muñoz" al escribir "Munoz". Las columnas de texto sobre las que el
   sistema busca refuerzan además esta collation a nivel de columna.
   =========================================================================== */

IF DB_ID(N'HotelDB') IS NOT NULL
BEGIN
    PRINT N'La base de datos HotelDB ya existe. No se realizaron cambios.';
END
ELSE
BEGIN
    CREATE DATABASE [HotelDB] COLLATE Latin1_General_CI_AI;
    PRINT N'Base de datos HotelDB creada con collation Latin1_General_CI_AI.';
END
GO

USE [HotelDB];
GO

/* Nivel de aislamiento con versionado de filas: reduce los bloqueos de lectura
   manteniendo la consistencia exigida por RNF03 (50 usuarios concurrentes). */
IF (SELECT is_read_committed_snapshot_on FROM sys.databases WHERE name = N'HotelDB') = 0
BEGIN
    ALTER DATABASE [HotelDB] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
    PRINT N'READ_COMMITTED_SNAPSHOT habilitado.';
END
GO
