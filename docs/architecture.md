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

Crear ambas en Unity Editor con plantilla 3D/URP apropiada, guardarlas bajo `Assets/_Echoryx/Scenes/` y verificar referencias/bake dentro del Editor. No generar YAML de escena manualmente. No añadirlas a Build Settings hasta que se acuerde el flujo ejecutable.
