# Android settlement presentation fix

Reported: UI visible on Android, settlement pink; zoom-out insufficient.

The scene generates all world/portrait materials at runtime. Its old Shader.Find-only Standard lookup had no tracked shader/material reference guaranteeing inclusion in the player. Replaced the built-in pipeline path with a Resources-loaded, bundled lightweight shader. Missing/unsupported shader now fails explicitly instead of accepting an unusable material. This addresses a likely shader stripping cause; actual phone verification remains necessary. The optional URP path is unchanged; this project ships the built-in pipeline.

The bundled shader uses ambient/directional lighting and fog, without dynamic shadow/specular passes. Shared materials and mesh combining remain; GPU instancing is not enabled for this simple shader.

The old camera's fixed size 20 and maximum 23 did not account for phone aspect ratio or HUD coverage. HOME now fits the walls/towers between the HUD panels with padding, recenters there, and recalculates for screen/safe-area changes. Pinch uses a distance ratio, independent of pixel density; zoom-out allows a broad terrain overview. Camera pan/taps remain; pinch must start over the world rather than UI.

Created: Resources folder/meta, AshSettlement.shader/meta, this report.
Modified: SettlementArt.cs, SettlementCamera.cs, SettlementValidation.cs, Tests/verify_assets.py, README.md.

Verified in cloud: C# syntax parsing (33 files), asset metadata/reference checks (75 GUIDs, 81 references), whitespace checks. Domain code and save formats unchanged. No Unity Editor/Android build/GPU is available, so shader compilation, rendered output and touch behavior are unverified here. Existing Validate Settlement Presentation now checks shader import/support and actual camera-projected settlement corners outside the HUD.

Next: open updated project in Unity 6000.3.23f1; outside Play run Ashlands > Validate Settlement Presentation. Build And Run to the same phone using the same application identifier/signing identity. Do not uninstall, since Android uninstallation deletes local saves. Check settlement/hero colors, HOME overview, pinch-in/out over the world, UI taps and saved progress after restart. If pink persists, capture Android player shader errors rather than changing unrelated materials/packages.
