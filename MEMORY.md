# Memoria del proyecto

## Decisiones confirmadas

- Protagonista humano con traje tecnológico; cámara en tercera persona.
- Echo conserva hasta cinco segundos y su reproducción se inicia manualmente.
- Rewind afecta objetos controlados, no implica rebobinar todo el mundo.
- Al terminar Rewind, el mecanismo continúa su movimiento normal desde el estado recuperado; no vuelve automáticamente al estado previo al retroceso.
- La experiencia tendrá checkpoints, tres niveles cortos y un desafío final.

## Estado

- Base: Unity 6000.3.22f1, URP 17.3.0, Input System 1.20.1 y Cinemachine 3.1.7 declarados.
- S00: cerrado en `aa2f2c5`; `Prototype` y `Laboratory` existen.
- S01: cerrado según el estado informado por el usuario; controlador, cámara y circuito de `Prototype` presentes.
- S02-A: captura con Q hasta cinco segundos a 20 Hz, validada manualmente por el usuario (hasta 101 muestras).
- S02-B: clon holográfico y reproducción con E implementados; 12 pruebas EditMode pasaron y el usuario confirmó validación en Play Mode.
- S02-C: placa, puerta y rompecabezas de Echo incorporados a `Prototype`; 21/21 pruebas EditMode del proyecto pasaron y el usuario confirmó S02 en Play Mode.
- S03-A: núcleo de historial y Rewind cinemático implementado; 30/30 pruebas EditMode pasaron, integrado y validado posteriormente en S03.
- S03-B: selección por cámara y RMB para un único mecanismo durante cada pulsación, con rotor cinemático provisional en `Prototype`; 41/41 pruebas EditMode pasaron y el usuario confirmó 10/10 validaciones manuales.
- S03-C: viaje único al destino, con regreso solo mediante Rewind; 48/48 EditMode pasaron y el usuario confirmó S03 completo y validado en `fc73688`.
- S04-A: checkpoints, R, zona de caída, limpieza básica del jugador y contrato por sala; 61/61 EditMode pasaron y el usuario confirmó validación manual.
- S04-B: reinicio coordinado de Echo global y mecanismos inscritos en la sala activa; 74/74 EditMode pasaron. El usuario confirmó 11/11 comprobaciones manuales.
- S04-C: sala combinada añadida en Prototype, con un checkpoint de entrada y ámbito propio; 82/82 EditMode y 3/3 PlayMode automatizados pasaron; el usuario confirmó 11/11 comprobaciones manuales. El trabajo S04 está registrado en el checkout actual en `2cd3002`, verificado al comenzar S05-A1.
- Corrección final S04: checkpoint y reaparición B trasladados al inicio del acceso para impedir embarcar en el viaje inicial después de R/caída; plataforma y núcleos temporales sin cambios. 83/83 EditMode y 5/5 PlayMode pasaron en Unity batchmode; comprobación visual manual del ajuste pendiente.
- Dirección visual S05 confirmada: ciencia ficción temporal estilizada, 3D de detalle medio; grafito/titanio, cian Echo y violeta Rewind.
- S05-A1: ZIP Standard gratuito de Quaternius localizado; subconjunto de 10 FBX y 8 texturas CC0, materiales URP y VisualAssetLab independiente preparados. Importación validada en Unity 6000.3.22f1; captura final URP renderizada e inspeccionada, con presentación corregida. Revisión artística/manual pendiente; S05-A2 abordará montaje modular, iluminación y colliders. No se modifican mecánicas ni Prototype.
- HUD y niveles definitivos siguen sin implementarse.

Consulta `docs/` para especificaciones completas, dependencias y criterios; evita duplicar aquí detalles técnicos.
