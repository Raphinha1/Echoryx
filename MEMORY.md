# Memoria del proyecto

## Decisiones confirmadas

- Protagonista humano con traje tecnológico; cámara en tercera persona.
- Echo conserva hasta cinco segundos y su reproducción se inicia manualmente.
- Rewind afecta objetos controlados, no implica rebobinar todo el mundo.
- La experiencia tendrá checkpoints, tres niveles cortos y un desafío final.

## Estado

- Base: Unity 6000.3.22f1, URP 17.3.0, Input System 1.20.1 y Cinemachine 3.1.7 declarados.
- S00: cerrado en `aa2f2c5`; `Prototype` y `Laboratory` existen.
- S01: cerrado según el estado informado por el usuario; controlador, cámara y circuito de `Prototype` presentes.
- S02-A: captura con Q hasta cinco segundos a 20 Hz, validada manualmente por el usuario (hasta 101 muestras).
- S02-B: clon holográfico y reproducción con E implementados; 12 pruebas EditMode pasaron y el usuario confirmó validación en Play Mode.
- S02-C: placa, puerta y rompecabezas de Echo incorporados a `Prototype`; 21/21 pruebas EditMode del proyecto pasan; validación jugable de este slice pendiente.
- Rewind, checkpoints, HUD y niveles definitivos siguen sin implementarse.

Consulta `docs/` para especificaciones completas, dependencias y criterios; evita duplicar aquí detalles técnicos.
