# Decisiones Técnicas y de Arquitectura

## 1. Organización en Capas y Reglas de Negocio
La solución está dividida en cuatro proyectos (`Domain`, `Application`, `Infrastructure`, `Api`) y un proyecto de pruebas (`UnitTests`).

Las reglas de negocio se concentraron directamente en las entidades del dominio (`MaintenanceRequest`, `Lease`, `Visit`) para evitar que la lógica quede repartida entre servicios y controladores:
* **Control de estado:** Propiedades como `Status`, `TechnicianId` y `ClosedAt` tienen setters privados. Las transiciones (`Abierta -> Asignada -> EnProgreso -> Cerrada` o `Cancelada`) pasan exclusivamente por `ChangeStatus()`, lo que impide cerrar solicitudes sin visitas registradas o asignar técnicos inactivos.
* **Manejo de excepciones:** Se definió `DomainException` con el código HTTP correspondiente (`400`, `403`, `404`, `409`), interceptada por `GlobalExceptionMiddleware` para devolver respuestas JSON consistentes sin llenar los controladores de bloques `try-catch`.

## 2. Uso Combinado de EF Core y Dapper (T-SQL)
Cada herramienta se utilizó según el tipo de operación:
* **Entity Framework Core:** Se empleó para las migraciones, el *Seeder* y las operaciones de escritura, aprovechando el *change tracking* y el manejo transaccional para persistir cambios en múltiples entidades.
* **Dapper con T-SQL:** Para los reportes (`/api/reports/properties-summary` y `/api/reports/technicians-workload`) se escribieron consultas T-SQL directamente utilizando CTEs (`WITH`), `DATEDIFF` y agregaciones condicionales. De este modo SQL Server resuelve los cálculos en una sola consulta y se evitan problemas de N+1 o agrupaciones en memoria.

## 3. Integridad Referencial, Concurrencia e Índices en SQL Server
Además de las validaciones en código, se agregaron restricciones a nivel de base de datos para garantizar consistencia ante peticiones concurrentes:
* **Un contrato vigente por unidad:** Se creó el índice único filtrado `UX_Leases_UnitId_OpenEnd` (`WHERE [EndDate] IS NULL`) junto con transacciones de aislamiento `Serializable` en las escrituras para evitar que dos solicitudes simultáneas registren contratos abiertos en la misma unidad.
* **Restricciones y borrados:** Se definió el `CHECK` constraint `CK_Visits_Cost_NonNegative` (`[Cost] >= 0`) en `Visits` y se configuró `DeleteBehavior.Restrict` en las relaciones principales para prevenir borrados en cascada accidentales y evitar ciclos de cascada en SQL Server.
* **Índices compuestos:** Se agregaron índices sobre `(Status, CreatedAt)`, `(UnitId, Status, CreatedAt)` y `(TechnicianId, VisitDate)` alineados con los filtros de paginación y los `JOIN` de los reportes.

## 4. Seed Determinista e Idempotente
`DatabaseSeeder` utiliza una semilla fija (`new Random(42)`) y verifica si ya existen registros en `Properties` antes de ejecutarse. Los 100 registros históricos se generan invocando los mismos métodos del dominio (`MaintenanceRequest.Create`, `ChangeStatus`, `RegisterVisit`), asegurando que los datos iniciales cumplan las mismas reglas que la API.

## 5. Puntos de Mejora a Futuro
* **Paginación por cursor o "Keyset pagination":** En `/api/maintenance-requests`, sustituir `Skip/Take` por paginación basada en `(CreatedAt, Id)` para mantener tiempos de respuesta constantes cuando crezca el volumen de registros.
* **Pruebas de integración:** Complementar los tests unitarios con pruebas de integración usando Testcontainers para validar contra una instancia real de SQL Server tanto los índices filtrados como las consultas T-SQL de Dapper.

## 6. Uso de Herramientas de IA
Se utilizaron herramientas de IA como apoyo de consulta técnica y revisión durante el desarrollo, principalmente para:
- **Revisión de redacción y claridad:** Validar la estructura y redacción de `README.md` y `DECISIONES.md` para asegurar que los supuestos, instrucciones y decisiones técnicas estuvieran documentados de forma clara.
- **Optimización de consultas y métodos:** Revisar las consultas T-SQL y la lógica de los servicios para evitar redundancias, ciclos innecesarios o problemas de rendimiento (como productos cartesianos en los `JOIN` de los reportes o consultas N+1).
- **Validación de configuración:** Contrastar compatibilidades de paquetes en .NET 10 y restricciones de esquema en SQL Server (`DeleteBehavior.Restrict` e índices filtrados) antes de aplicar las migraciones.