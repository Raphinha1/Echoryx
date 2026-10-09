# Echoryx

Videojuego 3D de plataformas y resolución de desafíos en Unity 6 con URP. El jugador controla a una persona con traje tecnológico y usa una grabación temporal llamada Echo y el Rewind de objetos controlados para superar tres niveles cortos y un desafío final.

## Requisitos y apertura

- Unity Editor **6000.3.22f1** (ver `ProjectSettings/ProjectVersion.txt`).
- Abrir la raíz del repositorio desde Unity Hub. Unity Package Manager resuelve las dependencias declaradas en `Packages/manifest.json`.
- URP, Input System y Cinemachine ya están declarados; no agregues versiones duplicadas ni cambies sus versiones sin revisar compatibilidad.

## Estado

S00 quedó cerrado en `aa2f2c5`. S01 incluye un controlador con cápsula provisional, cámara en tercera persona y un circuito en `Assets/_Echoryx/Scenes/Prototype.unity`. Q graba hasta cinco segundos; E inicia la reproducción de esa trayectoria mediante un clon holográfico. S02-C añade una placa y puerta al costado del circuito: graba varios segundos quieto sobre la placa, acércate a la puerta y pulsa E para cruzar mientras el Echo la mantiene abierta. El usuario confirmó S02-B en Play Mode; S02-C requiere validación jugable. Consulta [validaciones](docs/testing.md#s02-c-placa-y-puerta-echo).

Empieza por [GDD](docs/GDD.md), [arquitectura](docs/architecture.md) y [plan de desarrollo](docs/development-plan.md). Las decisiones y el estado breve se registran en [MEMORY.md](MEMORY.md).
