# Sistema de Gestión de Mantenimiento Predial (.NET 10 + SQL Server 2022)

API RESTful desarrollada en **.NET 10** y **SQL Server 2022** para administrar solicitudes de mantenimiento, contratos de arrendamiento, visitas técnicas y reportes operativos de multiples edificios.

## Requisitos previos
- Docker Desktop (con WSL 2 y al menos 3 GB de RAM asignados para SQL Server 2022, de lo contrario podría fallar por la poca RAM disponible).

## Cómo ejecutar el proyecto
1. Clonar el repositorio y ubicarse en la carpeta raíz:
   ```bash
   git clone https://github.com/leonelperezramirez98-spec/prueba-leonel-perez.git
   cd prueba-leonel-perez

## Supuestos realizados
- Fecha de referencia para vigencia y reportes: Todas las validaciones de contratos vigentes (`StartDate <= hoy <= EndDate`) y ventanas de tiempo (`último mes`, `últimos 6 meses`) se evalúan en UTC (`DateTime.UtcNow`).
- Unidades desocupadas: En el seed inicial, 20 de las 30 unidades tienen contrato vigente y 10 permanecen disponibles para verificar que `GET /api/properties/{id}/units` devuelva `currentTenant: null` sin errores.
- Contratos sin fecha de fin: Se asume que un contrato con `EndDate = null` es indefinido y permanece vigente a partir de su `StartDate`; a nivel de base de datos se restringe a máximo un contrato abierto por unidad mediante índice único filtrado.
- Cancelación de solicitudes: Se permite cancelar únicamente desde `Abierta` o `Asignada` (antes de iniciar trabajo en sitio), sin exigir visitas registradas ni registrar `ClosedAt`.
- Reinicio limpio de datos: Para volver a ejecutar el seed desde cero tras pruebas de escritura, basta con reiniciar el volumen con `docker compose down -v && docker compose up -d`.