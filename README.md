# Prueba técnica — .NET Developer

Construye una API de punta a punta: modelo de datos, base de datos, datos de prueba y consultas que crucen varias tablas. Se evalúan el criterio, la claridad del código y la justificación de las decisiones.

- **Stack obligatorio:** C# y .NET 10 (LTS), SQL Server 2022 o 2025 (compatible con Azure SQL).
- **Sobre .NET Framework:** la prueba es en .NET moderno, la plataforma vigente para proyectos nuevos. El lenguaje y los conceptos son los mismos que en .NET Framework.
- **Tiempo estimado:** 4 a 6 horas. El plazo de entrega te lo indica el equipo de reclutamiento.
- **Punto de partida:** crea tu repositorio con **Use this template** en GitHub. Incluye este enunciado y el entorno Docker.
- **Entrega:** el link a tu repositorio público.

---

## Contexto

Una empresa administra edificios en renta. Cada edificio tiene varias unidades (departamentos o locales) y cada unidad puede tener un inquilino con contrato vigente. Los inquilinos reportan problemas de mantenimiento (una fuga, una puerta rota, una falla eléctrica) y la empresa asigna técnicos que visitan la unidad hasta resolverlos.

Hoy todo se lleva en hojas de cálculo. Necesitan una API que sirva de base para el sistema.

## Modelo de datos

Este es el modelo mínimo. Puedes agregar columnas, tablas, índices o restricciones si lo justificas.

| Entidad | Descripción | Campos mínimos |
| --- | --- | --- |
| **Edificio** (`Property`) | Inmueble administrado | nombre, dirección, ciudad |
| **Unidad** (`Unit`) | Departamento o local dentro de un edificio | edificio, número, piso, tipo (`Departamento`, `Local`) |
| **Inquilino** (`Tenant`) | Persona que renta | nombre completo, email (único), teléfono |
| **Contrato** (`Lease`) | Relación inquilino ↔ unidad en el tiempo | unidad, inquilino, fecha inicio, fecha fin (nullable), renta mensual |
| **Técnico** (`Technician`) | Persona que atiende las solicitudes | nombre, especialidad (`Plomería`, `Electricidad`, `General`), activo |
| **Solicitud** (`MaintenanceRequest`) | Problema reportado | unidad, inquilino que reporta, técnico asignado (nullable), categoría, prioridad (`Baja`, `Media`, `Alta`, `Urgente`), estado, descripción, fecha de creación, fecha de cierre (nullable) |
| **Visita** (`Visit`) | Cada vez que un técnico atiende una solicitud | solicitud, técnico, fecha, notas, costo |

### Reglas de negocio

1. Una unidad tiene **como máximo un contrato vigente** a la vez. Un contrato está vigente si la fecha actual está entre su inicio y su fin (o no tiene fin).
2. Solo puede crear una solicitud el **inquilino con contrato vigente** en esa unidad.
3. El estado de una solicitud avanza así: `Abierta → Asignada → EnProgreso → Cerrada`. También puede pasar a `Cancelada` desde `Abierta` o `Asignada`. Ninguna otra transición es válida.
4. Para pasar a `Asignada` se necesita un técnico **activo**.
5. Para pasar a `Cerrada` la solicitud debe tener **al menos una visita**. Al cerrar se guarda la fecha de cierre.
6. Los costos de las visitas no pueden ser negativos.

## Entorno incluido

El repo trae un entorno Docker base. Solo necesitas [Docker](https://docs.docker.com/get-docker/); no hace falta instalar .NET ni SQL Server en tu máquina.

| Servicio | Contenido | Puerto en tu máquina |
| --- | --- | --- |
| `db` | SQL Server 2022 (Developer) | `1433` |
| `dev` | SDK de .NET 10, con el repo montado en `/workspace` | `8080` → `8080` del contenedor |

Conexión a SQL Server:

| Desde | Servidor | Usuario | Contraseña |
| --- | --- | --- | --- |
| El contenedor `dev` | `db,1433` | `sa` | `Dev_Passw0rd_2026` |
| Tu máquina | `localhost,1433` | `sa` | `Dev_Passw0rd_2026` |

```bash
docker compose up -d          # levanta el entorno
docker compose exec dev bash  # abre una terminal con .NET 10
docker compose down           # lo detiene
docker compose down -v        # lo detiene y borra los datos de SQL Server
```

- `sqlcmd` está disponible en el contenedor `db`: `docker compose exec db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P Dev_Passw0rd_2026 -C`
- Con VS Code puedes abrir el repo dentro del contenedor `dev` desde `.devcontainer/` ("Reopen in Container").
- Si un puerto está ocupado o quieres otra contraseña, copia `.env.example` a `.env` y cámbialo.
- En Mac con Apple Silicon, SQL Server corre emulado (`linux/amd64`) con Docker Desktop (Rosetta activado) u OrbStack.

Puedes modificar el entorno, reemplazarlo o no usarlo.

## Lo que debes construir

### 1. Base de datos

- Crea el esquema en **SQL Server** con llaves primarias, llaves foráneas y las restricciones que consideres necesarias.
- Usa migraciones (EF Core) o scripts SQL versionados en el repo. Lo importante es que cualquiera pueda recrear la base desde cero.
- Puedes usar EF Core, Dapper o ADO.NET. Elige y explica por qué.

### 2. Poblar la base de datos

Crea un mecanismo de seed **reproducible** (ejecutarlo dos veces no debe duplicar datos) con, como mínimo:

- 3 edificios
- 30 unidades repartidas entre los edificios
- 20 inquilinos, con contratos vigentes y algunos ya terminados
- 5 técnicos (al menos uno inactivo)
- 100 solicitudes distribuidas en los últimos 6 meses, en todos los estados, con sus visitas

Los datos deben ser coherentes con las reglas de negocio (por ejemplo, no debe haber solicitudes cerradas sin visitas).

### 3. API

API REST con documentación OpenAPI/Swagger. Endpoints mínimos:

**Escritura**

| Método | Ruta | Qué hace |
| --- | --- | --- |
| `POST` | `/api/maintenance-requests` | Crea una solicitud. Valida las reglas 1 y 2. |
| `POST` | `/api/maintenance-requests/{id}/visits` | Registra una visita (regla 6). |
| `PATCH` | `/api/maintenance-requests/{id}/status` | Cambia el estado (reglas 3, 4 y 5). Si asigna técnico, lo recibe en el body. |

**Consultas (varias tablas)**

| Método | Ruta | Qué devuelve |
| --- | --- | --- |
| `GET` | `/api/properties/{id}/units` | Las unidades del edificio con su inquilino vigente (o vacío si está desocupada). |
| `GET` | `/api/maintenance-requests` | Listado **paginado** con filtros opcionales por `status`, `priority`, `propertyId` y rango de fechas. Cada fila incluye edificio, número de unidad, inquilino y técnico. |
| `GET` | `/api/tenants/{id}/history` | Contratos del inquilino (unidad y edificio) y sus solicitudes con el costo total de visitas de cada una. |
| `GET` | `/api/reports/properties-summary?from=&to=` | Por edificio: solicitudes abiertas, cerradas, tiempo promedio de resolución en horas y costo total de visitas en el rango. |
| `GET` | `/api/reports/technicians-workload` | Por técnico activo: solicitudes asignadas o en progreso, visitas del último mes y costo acumulado, ordenado por carga. |

Al menos **uno de los dos reportes** debe estar escrito en **SQL (T-SQL) a mano**, ya sea como consulta parametrizada, vista o procedimiento almacenado.

Los errores deben responder con códigos HTTP adecuados (`400`, `404`, `409`, etc.) y un cuerpo consistente.

### 4. Cómo correrlo

El proyecto debe levantar con instrucciones claras en tu README. Lo ideal es que un solo `docker compose up` levante SQL Server y la API, aplique el esquema y cargue el seed. Puedes extender el `compose.yaml` incluido. Si no usas Docker, documenta los pasos exactos.

El entorno usa SQL Server 2022. También puedes usar la imagen `mcr.microsoft.com/mssql/server:2025-latest`.

## Extras (opcionales)

Suman puntos solo si lo mínimo funciona correctamente.

- Pruebas automatizadas (unitarias para las reglas de negocio, de integración para algún endpoint).
- Uso de transacciones donde haya más de una escritura que deba ser atómica, y explicación de por qué.
- Índices pensados para las consultas de los reportes, con la justificación.
- Logging estructurado y manejo global de errores.
- Validación de entrada con mensajes claros.
- Pipeline de CI (por ejemplo, GitHub Actions) que compile y corra las pruebas.

## Qué debes entregar

1. Un `README.md` con: cómo correr el proyecto, cómo probar los endpoints y supuestos que hiciste.
2. Un archivo `DECISIONES.md` breve con:
   - Las decisiones técnicas importantes y por qué (ORM vs SQL, estructura del proyecto, etc.).
   - Qué harías distinto con más tiempo.
   - Si usaste herramientas de IA: para qué y qué corregiste o validaste de lo que generaron. Su uso está permitido; se evalúa el criterio sobre el resultado.
3. Historial de commits que muestre el avance. Un único commit final resta puntos.

## Evaluación

| Criterio | Peso |
| --- | --- |
| Funciona: levanta con las instrucciones y los endpoints responden correctamente | 25% |
| Modelo de datos y SQL: esquema, restricciones, consultas correctas y eficientes | 25% |
| Reglas de negocio y manejo de errores | 20% |
| Calidad de código: organización, nombres, legibilidad | 15% |
| Documentación y justificación de decisiones | 15% |

Después de la entrega puede haber una sesión técnica para revisar la solución, hacer un cambio en vivo y discutir las decisiones.

## Dudas

Si algo no está claro, toma un supuesto razonable, documéntalo en tu README y continúa. El manejo de la ambigüedad también se evalúa.
