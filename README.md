# Echoryx

Videojuego 3D de plataformas y resolución de desafíos en Unity 6 con URP. El jugador controla a una persona con traje tecnológico y usa una grabación temporal llamada Echo y el Rewind de objetos controlados para superar tres niveles cortos y un desafío final.

## Requisitos y apertura

- Unity Editor **6000.3.22f1** (ver `ProjectSettings/ProjectVersion.txt`).
- Abrir la raíz del repositorio desde Unity Hub. Unity Package Manager resuelve las dependencias declaradas en `Packages/manifest.json`.
- URP, Input System y Cinemachine ya están declarados; no agregues versiones duplicadas ni cambies sus versiones sin revisar compatibilidad.

## Estado

S01, S02 y S03 están completos y validados por el usuario en `fc73688`. `Prototype` conserva movimiento y cámara, Q/E para Echo, placa/puerta y RMB para Rewind. S04-A añade checkpoints y reaparición con R o al caer: solo reinicia los participantes inscritos en la sala del checkpoint activo. S04-B cancela Echo y libera RMB inmediatamente; restaura los mecanismos e historiales de la sala activa antes de reaparecer. S04-A/B/C están validados manualmente por el usuario; el ajuste posterior del acceso B tiene pruebas automatizadas y una regresión visual pendiente. S04-C añade una sala combinada al final del puente derecho (x = 22): Q/E abren la primera barrera y RMB recupera la plataforma para cruzar la segunda. Consulta [solución y pruebas de S04-C](docs/testing.md#s04-c-sala-combinada) y [corrección del acceso B](docs/testing.md#corrección-final-s04-acceso-b-tras-reinicio).

Empieza por [GDD](docs/GDD.md), [arquitectura](docs/architecture.md) y [plan de desarrollo](docs/development-plan.md). Las decisiones y el estado breve se registran en [MEMORY.md](MEMORY.md).

## Assets visuales S05-A1

Biblioteca mínima gratuita de Quaternius Standard (CC0), materiales URP y galería independiente `VisualAssetLab`. Consulta [procedencia, selección y límites](docs/visual-assets.md). Abrir la galería para revisar escala, paleta y superficies; no reemplaza Prototype ni el personaje.

## Sala de referencia S05-A2

Abrir `Assets/_Echoryx/Scenes/VisualReferenceRoom.unity` y entrar en Play Mode para recorrer la sala con el Player y la cámara existentes. Hay cinco prefabs reutilizables de estructura con colliders simples; la puerta norte es una muestra cerrada, no un mecanismo. [Disposición, capturas y validaciones](docs/visual-reference-room.md).

## Personajes evaluados S05-B1/B2

Cybernet Warrior (S05-B1) fue descartado por falta de rig y retirado de Assets; [registro y trazabilidad](docs/character-visual-assets.md). SCI-FI SuperSoldier (SihaGames, CC BY 4.0; casco Udon-San) tiene Avatar Humanoid y locomoción Idle/Walk/Run/Jump/Fall/Land aprobados en laboratorios aislados. S05-B2/B3 están cerrados; S05-B4, integración con PlayerMotor, es el siguiente paso pendiente. [Validación del modelo](docs/supersoldier-validation.md).


## Laboratorio de locomoción S05-B3

Abrir `Assets/_Echoryx/Scenes/SuperSoldierLocomotionLab.unity` en Play Mode: muestra automáticamente Idle/Walk/Run/Jump/Fall/Land con el Avatar del SuperSoldier y Root Motion desactivado. El usuario aprobó manualmente Idle/Walk/Run/Jump/Fall/Land, contacto y transiciones tras corregir la preparación de Jump. La fase aérea del laboratorio no simula el salto físico, que se integrará en S05-B4. Playback Rate también permite inspeccionar la demo a velocidad reducida. El laboratorio ofrece Individual Clip (sin controller, con pausa/scrub) y parámetros manuales al desactivar Automatic Demo. El siguiente paso es S05-B4, aún sin implementar. [Fuentes Mixamo, controller, resultados y pendientes](docs/supersoldier-locomotion.md). El Player aún conserva la cápsula provisional.
