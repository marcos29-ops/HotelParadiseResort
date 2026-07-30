using HotelParadiseResort.Application.Abstracciones;
using HotelParadiseResort.Application.DTOs.Usuarios;
using HotelParadiseResort.Application.Servicios.Interfaces;
using HotelParadiseResort.Domain.Entidades;
using HotelParadiseResort.Domain.Enumeraciones;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Shared.Paginacion;
using HotelParadiseResort.Shared.Resultados;
using Microsoft.Extensions.Logging;

namespace HotelParadiseResort.Application.Servicios;

/// <summary>
/// Administración del personal con acceso al sistema. Solo el rol Administrador opera
/// este servicio, conforme al diagrama de casos de uso.
/// </summary>
public sealed class ServicioUsuarios : IServicioUsuarios
{
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IServicioContrasena _servicioContrasena;
    private readonly IUsuarioActual _usuarioActual;
    private readonly ILogger<ServicioUsuarios> _registro;

    public ServicioUsuarios(
        IUnidadDeTrabajo unidadDeTrabajo,
        IServicioContrasena servicioContrasena,
        IUsuarioActual usuarioActual,
        ILogger<ServicioUsuarios> registro)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _servicioContrasena = servicioContrasena;
        _usuarioActual = usuarioActual;
        _registro = registro;
    }

    public async Task<Resultado<ResultadoPaginado<UsuarioDto>>> ListarAsync(
        ParametrosPaginacion parametros, CancellationToken cancelacion = default)
    {
        var pagina = await _unidadDeTrabajo.Usuarios.BuscarAsync(parametros, cancelacion);

        var elementos = pagina.Elementos.Select(Mapear).ToList();

        return Resultado.Exitoso(new ResultadoPaginado<UsuarioDto>(
            elementos, pagina.TotalRegistros, pagina.Pagina, pagina.TamanoPagina));
    }

    public async Task<Resultado<UsuarioDto>> ObtenerPorIdAsync(int id, CancellationToken cancelacion = default)
    {
        var usuario = await _unidadDeTrabajo.Usuarios.ObtenerPorIdAsync(id, cancelacion);

        return usuario is null
            ? Resultado.Fallo<UsuarioDto>("El usuario indicado no existe.", TipoError.NoEncontrado)
            : Resultado.Exitoso(Mapear(usuario));
    }

    public async Task<Resultado<UsuarioDto>> CrearAsync(
        CrearUsuarioDto solicitud, CancellationToken cancelacion = default)
    {
        if (!Enum.TryParse<RolUsuario>(solicitud.Rol, ignoreCase: true, out var rol))
        {
            return Resultado.Fallo<UsuarioDto>(
                $"El rol '{solicitud.Rol}' no es válido.", TipoError.Validacion);
        }

        if (await _unidadDeTrabajo.Usuarios.ExisteNombreUsuarioAsync(solicitud.NombreUsuario, null, cancelacion))
        {
            return Resultado.Fallo<UsuarioDto>(
                $"Ya existe un usuario con el nombre '{solicitud.NombreUsuario}'.", TipoError.Conflicto);
        }

        var usuario = new Usuario
        {
            Nombre = solicitud.Nombre.Trim(),
            NombreUsuario = solicitud.NombreUsuario.Trim(),
            Correo = solicitud.Correo.Trim(),
            ContrasenaHash = _servicioContrasena.Hashear(solicitud.Contrasena),
            Rol = rol,
            Activo = true
        };

        await _unidadDeTrabajo.Usuarios.AgregarAsync(usuario, cancelacion);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation(
            "El usuario {UsuarioId} creó la cuenta {NuevoUsuarioId} con rol {Rol}.",
            _usuarioActual.Id, usuario.Id, rol);

        return Resultado.Exitoso(Mapear(usuario));
    }

    public async Task<Resultado<UsuarioDto>> ActualizarAsync(
        int id, ActualizarUsuarioDto solicitud, CancellationToken cancelacion = default)
    {
        var usuario = await _unidadDeTrabajo.Usuarios.ObtenerPorIdAsync(id, cancelacion);

        if (usuario is null)
        {
            return Resultado.Fallo<UsuarioDto>("El usuario indicado no existe.", TipoError.NoEncontrado);
        }

        if (!Enum.TryParse<RolUsuario>(solicitud.Rol, ignoreCase: true, out var rol))
        {
            return Resultado.Fallo<UsuarioDto>(
                $"El rol '{solicitud.Rol}' no es válido.", TipoError.Validacion);
        }

        // Impide que el último administrador activo pierda su rol o quede desactivado,
        // lo que dejaría al sistema sin nadie capaz de administrar usuarios.
        if (usuario.EsAdministrador && (rol != RolUsuario.Administrador || !solicitud.Activo))
        {
            var resultado = await ValidarNoEsUltimoAdministradorAsync(usuario.Id, cancelacion);
            if (resultado.EsFallido)
            {
                return Resultado.Fallo<UsuarioDto>(resultado.Error!, resultado.TipoError);
            }
        }

        usuario.Nombre = solicitud.Nombre.Trim();
        usuario.Correo = solicitud.Correo.Trim();
        usuario.Rol = rol;
        usuario.Activo = solicitud.Activo;

        _unidadDeTrabajo.Usuarios.Actualizar(usuario);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation(
            "El usuario {UsuarioId} actualizó la cuenta {CuentaId}.", _usuarioActual.Id, id);

        return Resultado.Exitoso(Mapear(usuario));
    }

    public async Task<Resultado> RestablecerContrasenaAsync(
        int id, RestablecerContrasenaDto solicitud, CancellationToken cancelacion = default)
    {
        var usuario = await _unidadDeTrabajo.Usuarios.ObtenerPorIdAsync(id, cancelacion);

        if (usuario is null)
        {
            return Resultado.Fallo("El usuario indicado no existe.", TipoError.NoEncontrado);
        }

        usuario.ContrasenaHash = _servicioContrasena.Hashear(solicitud.ContrasenaNueva);
        _unidadDeTrabajo.Usuarios.Actualizar(usuario);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation(
            "El usuario {UsuarioId} restableció la contraseña de la cuenta {CuentaId}.",
            _usuarioActual.Id, id);

        return Resultado.Exitoso();
    }

    public async Task<Resultado> DesactivarAsync(int id, CancellationToken cancelacion = default)
    {
        var usuario = await _unidadDeTrabajo.Usuarios.ObtenerPorIdAsync(id, cancelacion);

        if (usuario is null)
        {
            return Resultado.Fallo("El usuario indicado no existe.", TipoError.NoEncontrado);
        }

        if (usuario.Id == _usuarioActual.Id)
        {
            return Resultado.Fallo(
                "No es posible desactivar la propia cuenta en sesión.", TipoError.ReglaNegocio);
        }

        if (usuario.EsAdministrador)
        {
            var resultado = await ValidarNoEsUltimoAdministradorAsync(usuario.Id, cancelacion);
            if (resultado.EsFallido)
            {
                return resultado;
            }
        }

        usuario.Activo = false;
        _unidadDeTrabajo.Usuarios.Actualizar(usuario);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation(
            "El usuario {UsuarioId} desactivó la cuenta {CuentaId}.", _usuarioActual.Id, id);

        return Resultado.Exitoso();
    }

    private async Task<Resultado> ValidarNoEsUltimoAdministradorAsync(
        int usuarioId, CancellationToken cancelacion)
    {
        var usuarios = await _unidadDeTrabajo.Usuarios.ObtenerTodosAsync(cancelacion);

        var otrosAdministradoresActivos = usuarios.Count(
            u => u.Id != usuarioId && u.Rol == RolUsuario.Administrador && u.Activo);

        return otrosAdministradoresActivos > 0
            ? Resultado.Exitoso()
            : Resultado.Fallo(
                "El sistema debe conservar al menos un administrador activo.", TipoError.ReglaNegocio);
    }

    private static UsuarioDto Mapear(Usuario usuario) => new(
        usuario.Id,
        usuario.Nombre,
        usuario.NombreUsuario,
        usuario.Correo,
        usuario.Rol.ToString(),
        usuario.Activo,
        usuario.UltimoAcceso,
        usuario.FechaCreacion);
}
