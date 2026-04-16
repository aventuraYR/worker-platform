namespace WorkerPlatform.Jobs.Ejemplo.Models;

public record EjemploRegistro(
    int     Id,
    string  Descripcion,
    string  Estado,
    DateTime FechaCreacion);