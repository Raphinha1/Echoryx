# Echoryx

Videojuego 3D de plataformas y resolución de desafíos en Unity 6 con URP. El jugador controla a una persona con traje tecnológico y usa una grabación temporal llamada Echo y el Rewind de objetos controlados para superar tres niveles cortos y un desafío final.

## Requisitos y apertura

- Unity Editor **6000.3.22f1** (ver `ProjectSettings/ProjectVersion.txt`).
- Abrir la raíz del repositorio desde Unity Hub. Unity Package Manager resuelve las dependencias declaradas en `Packages/manifest.json`.
- URP, Input System y Cinemachine ya están declarados; no agregues versiones duplicadas ni cambies sus versiones sin revisar compatibilidad.

## Estado

S01, S02 y S03 están completos y validados por el usuario en `fc73688`. `Prototype` conserva movimiento y cámara, Q/E para Echo, placa/puerta y RMB para Rewind. S04-A añade checkpoints y reaparición con R o al caer: solo reinicia los participantes inscritos en la sala del checkpoint activo. S04-B cancela Echo y libera RMB inmediatamente; restaura los mecanismos e historiales de la sala activa antes de reaparecer. S04-A/B/C están validados manualmente por el usuario; el ajuste posterior del acceso B tiene pruebas automatizadas y una regresión visual pendiente. S04-C añade una sala combinada al final del puente derecho (x = 22): Q/E abren la primera barrera y RMB recupera la plataforma para cruzar la segunda. Consulta [solución y pruebas de S04-C](docs/testing.md#s04-c-sala-combinada) y [corrección del acceso B](docs/testing.md#corrección-final-s04-acceso-b-tras-reinicio).

Empieza por [GDD](docs/GDD.md), [arquitectura](docs/architecture.md) y [plan de desarrollo](docs/development-plan.md). Las decisiones y el estado breve se registran en [MEMORY.md](MEMORY.md).
