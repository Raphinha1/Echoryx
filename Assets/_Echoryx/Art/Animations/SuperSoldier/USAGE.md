# Mixamo locomotion sources

Five original FBX files downloaded by the user from Adobe Mixamo for X Bot, without meshes/textures. Source files are unchanged; hashes, original names and source inspection are recorded in provenance.json. Exact download options and timestamps were not supplied. Each source has its own Humanoid Avatar; no SuperSoldier Avatar is copied onto the X Bot source skeleton.

Adobe Mixamo FAQ permits royalty-free characters and animations in personal, commercial and nonprofit projects, including video games: https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html . These assets are not CC BY or CC0; use is governed by Adobe General Terms https://www.adobe.com/go/terms and Mixamo Additional Terms https://wwwimages2.adobe.com/content/dam/cc/en/legal/servicetou/Mixamo-Addl-Terms-en_US-20210623.pdf . The latter document, effective June 23, 2021, restricts using service content/output to create, train, test or improve AI/ML systems; it does not itself specify a Creative Commons license. Do not describe these clips as freely relicensed assets. Keep them within the Echoryx project; separate asset-library redistribution is not part of this work.

No credit/license text was included in the downloaded FBX files. The target SuperSoldier and its helmet retain their separate SihaGames/Udon-San CC BY credits in the model's ATTRIBUTION.md.

Unity import settings extract source XZ/rotation and airborne Y trajectories; Apply Root Motion is disabled on the lab Animator. IdleLoop.anim is an Editor-extracted copy of the existing SuperSoldier Idle with loop settings enabled, preserving the earlier FBX importer and laboratory. No keyframes were authored or source FBX edited.

The former `X Bot@Jumping.fbx` remains in `Mixamo/` unchanged for reference, but the controller's Jump state uses the user-provided `MixamoReplacement/Jumping Up.fbx`. Its source hash, 30 fps range (frames 0–25), Humanoid import, 0.833333 s duration, root policy, and Mixamo origin are recorded in `provenance.json`. The same Adobe/Mixamo terms above apply; the source file itself contains no separate license or attribution notice.
