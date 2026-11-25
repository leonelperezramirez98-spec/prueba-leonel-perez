namespace MaintenanceSystem.Domain.Enums;

public enum UnitType
{
    Departamento = 1,
    Local = 2
}

public enum TechnicianSpecialty
{
    Plomeria = 1,
    Electricidad = 2,
    General = 3
}

public enum RequestPriority
{
    Baja = 1,
    Media = 2,
    Alta = 3,
    Urgente = 4
}

public enum RequestStatus
{
    Abierta = 1,
    Asignada = 2,
    EnProgreso = 3,
    Cerrada = 4,
    Cancelada = 5
}