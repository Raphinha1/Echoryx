# Instrucciones para agentes

- Inspecciona el estado de Git y los archivos de Unity antes de editar; conserva cambios existentes.
- Usa Unity Editor 6000.3.22f1 y las versiones fijadas en `Packages/manifest.json`; no edites YAML de escenas a mano.
- Mantén los `.meta` que genera Unity junto con sus assets. No muevas ni dupliques contenido existente sin necesidad.
- Respeta el alcance del slice activo descrito en `docs/development-plan.md`; S00 no implementa mecánicas.
- Ejecuta solo validaciones pertinentes y reporta resultados observados, sin afirmar pruebas no ejecutadas.
- No hagas commits, pushes ni cambios remotos salvo petición explícita.
