using FluentValidation;
using HotelParadiseResort.Application.Fachada;
using HotelParadiseResort.Application.Servicios;
using HotelParadiseResort.Application.Servicios.Interfaces;
using HotelParadiseResort.Application.Tarifas;
using HotelParadiseResort.Domain.Patrones.Strategy;
using Microsoft.Extensions.DependencyInjection;

namespace HotelParadiseResort.Application.Configuracion;

/// <summary>Registro de los servicios de aplicación y de las estrategias de tarifa.</summary>
public static class RegistroAplicacion
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        // PATRÓN STRATEGY — Incorporar una tarifa nueva consiste en registrarla aquí;
        // ningún otro punto del sistema cambia.
        servicios.AddSingleton<IEstrategiaTarifa, TarifaEstandar>();
        servicios.AddSingleton<IEstrategiaTarifa, TarifaTemporadaAlta>();
        servicios.AddSingleton<ISelectorEstrategiaTarifa, SelectorEstrategiaTarifa>();

        servicios.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();
        servicios.AddScoped<IServicioUsuarios, ServicioUsuarios>();
        servicios.AddScoped<IServicioClientes, ServicioClientes>();
        servicios.AddScoped<IServicioHabitaciones, ServicioHabitaciones>();
        servicios.AddScoped<IServicioReservas, ServicioReservas>();

        // Facturación reutiliza el ensamblaje de la cuenta que hace Estadías, de modo
        // que la vista previa y el comprobante emitido no puedan discrepar.
        servicios.AddScoped<ServicioEstadias>();
        servicios.AddScoped<IServicioEstadias>(p => p.GetRequiredService<ServicioEstadias>());
        servicios.AddScoped<IServicioFacturacion, ServicioFacturacion>();
        servicios.AddScoped<IServicioReportes, ServicioReportes>();

        // PATRÓN FACADE — Punto de entrada único de la capa de Servicios.
        servicios.AddScoped<IFachadaServiciosHotel, FachadaServiciosHotel>();
        servicios.AddScoped<FachadaServiciosHotel>();

        servicios.AddValidatorsFromAssemblyContaining<RegistroAplicacionAncla>();

        return servicios;
    }
}

/// <summary>Ancla de ensamblado para el descubrimiento de validadores.</summary>
public sealed class RegistroAplicacionAncla;
