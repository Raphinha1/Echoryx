# Echoryx

Videojuego 3D de plataformas y resolución de desafíos en Unity 6 con URP. El jugador controla a una persona con traje tecnológico y usa una grabación temporal llamada Echo y el Rewind de objetos controlados para superar tres niveles cortos y un desafío final.

## Requisitos y apertura

- Unity Editor **6000.3.22f1** (ver `ProjectSettings/ProjectVersion.txt`).
- Abrir la raíz del repositorio desde Unity Hub. Unity Package Manager resuelve las dependencias declaradas en `Packages/manifest.json`.
- URP, Input System y Cinemachine ya están declarados; no agregues versiones duplicadas ni cambies sus versiones sin revisar compatibilidad.

## Estado

S00 quedó cerrado en `aa2f2c5`. S01 incluye movimiento, cámara en tercera persona y un circuito en `Assets/_Echoryx/Scenes/Prototype.unity`. Q graba hasta cinco segundos y E reproduce la trayectoria con un clon; S02 añade una placa y puerta. S03 permite seleccionar un mecanismo con la cámara y rebobinarlo manteniendo RMB. El usuario confirmó S01, S02, las 10 validaciones manuales de S03-B y 11 de la primera versión de S03-C. En `Prototype`, la plataforma verde azulado viaja una vez hasta el muelle de llegada y se detiene: hay que recuperarla con Rewind para cruzar. La solución tras este ajuste requiere validación en Play Mode. Consulta [validaciones](docs/testing.md#s03-c-plataforma-móvil-y-cruce).

Empieza por [GDD](docs/GDD.md), [arquitectura](docs/architecture.md) y [plan de desarrollo](docs/development-plan.md). Las decisiones y el estado breve se registran en [MEMORY.md](MEMORY.md).
