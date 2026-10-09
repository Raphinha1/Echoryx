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
- S03-A: núcleo de historial y Rewind cinemático implementado sin selección ni escena; 30/30 pruebas EditMode del proyecto pasan; validación integrada en Play Mode pendiente.
- S03-B: selección por cámara y RMB para un único mecanismo durante cada pulsación, con rotor cinemático provisional en `Prototype`; 41/41 pruebas EditMode pasaron y el usuario confirmó 10/10 validaciones manuales.
- S03-C: el usuario confirmó 11/11 pruebas manuales de la primera versión. Ajuste final: viaje único al destino, con regreso solo mediante Rewind; 48/48 EditMode pasaron. La nueva solución obligatoria requiere validación en Play Mode.
- Checkpoints, HUD y niveles definitivos siguen sin implementarse.

Consulta `docs/` para especificaciones completas, dependencias y criterios; evita duplicar aquí detalles técnicos.
