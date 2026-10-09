# Arquitectura

## Plataforma y estado base

- Unity 6 **6000.3.22f1**; renderizado URP **17.3.0**.
- Paquetes directos existentes: Input System **1.20.1**, Cinemachine **3.1.7**. Las versiones se conservan desde `Packages/manifest.json` y `packages-lock.json`.
- `Assets/Settings/` y `Assets/Scenes/SampleScene.unity` son contenido existente. No se mueven ni sustituyen durante S00.
- Unity crea y mantiene los `.meta`; conservarlos en control de versiones.

## Estructura de trabajo

`Assets/_Echoryx/` separa `Art`, `Audio`, `Materials`, `Prefabs`, `Scenes`, `Scripts`, `Settings` y `Tests`. `Scripts` se divide en `Player`, `Echo`, `Rewind`, `Interactions`, `Game` y `UI`. Es una organización objetivo; los recursos de plantilla existentes permanecen donde están hasta que un cambio posterior justifique migrarlos.

## Límites previstos

- **Player:** representación/control del protagonista y puente con cámara.
- **Echo:** captura acotada y reproducción manual de datos de acción.
- **Rewind:** historial y restauración de estado para objetos explícitamente habilitados.
- **Interactions:** contratos entre jugador y objetos/desafíos.
- **Game:** estado de partida, checkpoints y flujo de escenas/niveles.
- **UI:** presentación de estado y ayudas al jugador.

Estas son responsabilidades de diseño, no componentes ya implementados. Mantener la captura temporal separada de la reproducción y limitar Rewind a objetos registrados evita acoplar las mecánicas al estado global. Definir contratos y formato de datos durante sus respectivos slices.

## Escenas

- `Prototype`: espacio pequeño para validar el ciclo de juego cuando las mecánicas estén disponibles.
- `Laboratory`: escena aislada para probar mecánicas y objetos.

Ambas escenas existen. `Prototype` contiene ahora el circuito S01 y `Laboratory` permanece sin cambios. Los recursos de escena y prefab de S01 se generaron mediante Unity Editor, sin escribir YAML manualmente.

## Implementación S01

- `PlayerInputReader` usa el mapa `Player` del asset existente `Assets/InputSystem_Actions.inputactions` y expone Move, Look y el flanco de Jump. No usa el sistema de entrada antiguo.
- `PlayerMotor` mueve un `CharacterController` respecto al plano horizontal de la cámara, con aceleración, frenado, control aéreo, giro, gravedad y salto. `JumpWindow` concentra coyote time y jump buffering para poder probar esa lógica sin escena.
- `PlayerCameraOrbit` mueve un objetivo independiente del giro del personaje. `CinemachineCamera` con `CinemachineThirdPersonFollow` sigue ese objetivo; su opción `AvoidObstacles` cubre colisiones con geometría de la capa Default.
- `Player.prefab` contiene raíz con `CharacterController`, `VisualRoot/Capsule Placeholder` y `CameraAnchor`. El modelo futuro puede reemplazar el hijo visual sin modificar el collider o el motor.
- `S01PrototypeBuilder` (menú `Echoryx/S01/Build Player and Prototype`) generó el prefab y el circuito usando APIs de Unity. Rechaza reconstruir una escena que ya contenga elementos S01.

La órbita y el motor usan parámetros serializados ajustables desde Inspector. La cámara y el circuito solo están configurados en `Prototype`; el prefab de Player no guarda referencias a objetos de escena.
