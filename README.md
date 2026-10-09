# Echoryx

Videojuego 3D de plataformas y resolución de desafíos en Unity 6 con URP. El jugador controla a una persona con traje tecnológico y usa una grabación temporal llamada Echo y el Rewind de objetos controlados para superar tres niveles cortos y un desafío final.

## Requisitos y apertura

- Unity Editor **6000.3.22f1** (ver `ProjectSettings/ProjectVersion.txt`).
- Abrir la raíz del repositorio desde Unity Hub. Unity Package Manager resuelve las dependencias declaradas en `Packages/manifest.json`.
- URP, Input System y Cinemachine ya están declarados; no agregues versiones duplicadas ni cambies sus versiones sin revisar compatibilidad.

## Estado

El slice **S00** prepara estructura, documentación y validaciones de Editor. No incluye mecánicas implementadas. Las escenas `Prototype` y `Laboratory` están pendientes de crearse y guardarse desde Unity Editor; consulta [validaciones y pasos de Editor](docs/testing.md#preparación-en-unity-editor).

Empieza por [GDD](docs/GDD.md), [arquitectura](docs/architecture.md) y [plan de desarrollo](docs/development-plan.md). Las decisiones y el estado breve se registran en [MEMORY.md](MEMORY.md).
