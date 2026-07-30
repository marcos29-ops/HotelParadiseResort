using HotelParadiseResort.Application.Abstracciones;
using HotelParadiseResort.Application.DTOs.Autenticacion;
using HotelParadiseResort.Application.Servicios.Interfaces;
using HotelParadiseResort.Domain.Repositorios;
using HotelParadiseResort.Shared.Resultados;
using Microsoft.Extensions.Logging;

namespace HotelParadiseResort.Application.Servicios;

/// <summary>
/// Autenticación del personal del hotel (RNF01). Las credenciales se verifican contra
/// el hash almacenado; la contraseña en claro nunca se persiste ni se registra.
/// </summary>
public sealed class ServicioAutenticacion : IServicioAutenticacion
{
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IServicioContrasena _servicioContrasena;
    private readonly IServicioToken _servicioToken;
    private readonly IProveedorFechaHora _reloj;
    private readonly ILogger<ServicioAutenticacion> _registro;

    public ServicioAutenticacion(
        IUnidadDeTrabajo unidadDeTrabajo,
        IServicioContrasena servicioContrasena,
        IServicioToken servicioToken,
        IProveedorFechaHora reloj,
        ILogger<ServicioAutenticacion> registro)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _servicioContrasena = servicioContrasena;
        _servicioToken = servicioToken;
        _reloj = reloj;
        _registro = registro;
    }

    public async Task<Resultado<RespuestaInicioSesionDto>> IniciarSesionAsync(
        SolicitudInicioSesionDto solicitud, CancellationToken cancelacion = default)
    {
        var usuario = await _unidadDeTrabajo.Usuarios
            .ObtenerPorNombreUsuarioAsync(solicitud.NombreUsuario, cancelacion);

        // Se responde lo mismo ante usuario inexistente y ante contraseña incorrecta,
        // para no revelar qué nombres de usuario existen.
        if (usuario is null || !_servicioContrasena.Verificar(solicitud.Contrasena, usuario.ContrasenaHash))
        {
            _registro.LogWarning(
                "Intento de inicio de sesión fallido para el usuario {NombreUsuario}.",
                solicitud.NombreUsuario);

            return Resultado.Fallo<RespuestaInicioSesionDto>(
                "Usuario o contraseña incorrectos.", TipoError.NoAutenticado);
        }

        if (!usuario.Activo)
        {
            _registro.LogWarning(
                "El usuario {UsuarioId} intentó ingresar con la cuenta desactivada.", usuario.Id);

            return Resultado.Fallo<RespuestaInicioSesionDto>(
                "La cuenta se encuentra desactivada. Contacte al administrador.", TipoError.NoAutorizado);
        }

        var (token, expiracion) = _servicioToken.GenerarToken(usuario);

        usuario.RegistrarAcceso(_reloj.Ahora);
        _unidadDeTrabajo.Usuarios.Actualizar(usuario);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation("El usuario {UsuarioId} inició sesión correctamente.", usuario.Id);

        var respuesta = new RespuestaInicioSesionDto(
            token,
            expiracion,
            new UsuarioAutenticadoDto(
                usuario.Id,
                usuario.Nombre,
                usuario.NombreUsuario,
                usuario.Correo,
                usuario.Rol.ToString()));

        return Resultado.Exitoso(respuesta);
    }

    public async Task<Resultado> CambiarContrasenaAsync(
        int usuarioId, CambioContrasenaDto solicitud, CancellationToken cancelacion = default)
    {
        var usuario = await _unidadDeTrabajo.Usuarios.ObtenerPorIdAsync(usuarioId, cancelacion);

        if (usuario is null)
        {
            return Resultado.Fallo("El usuario indicado no existe.", TipoError.NoEncontrado);
        }

        if (!_servicioContrasena.Verificar(solicitud.ContrasenaActual, usuario.ContrasenaHash))
        {
            return Resultado.Fallo("La contraseña actual no es correcta.", TipoError.Validacion);
        }

        usuario.ContrasenaHash = _servicioContrasena.Hashear(solicitud.ContrasenaNueva);
        _unidadDeTrabajo.Usuarios.Actualizar(usuario);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation("El usuario {UsuarioId} cambió su contraseña.", usuarioId);

        return Resultado.Exitoso();
    }
}
