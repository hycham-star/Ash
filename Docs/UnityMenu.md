# Simplified Unity Editor menu

The Ashlands menu belongs to the Unity Editor project, which you open from Unity Hub. After this update it has only four main entries:

- **Open Settlement Scene** — opens the normal game scene and prepares the project automatically.
- **Prepare Project** — manually refreshes project configuration when needed.
- **Test Scenes** — optional PvE, hero/army and foundation debug scenes.
- **Validation** — optional checks organized into Gameplay, Settlement, World Map, Save and Load, Graphics, Architecture and Workflows.

For everyday use choose **Ashlands > Open Settlement Scene**, then press **Play**. You do not need to run validation before each launch. After an update, run only the check relevant to that update, or use a grouped check under Workflows when troubleshooting several related features. Some graphics checks require a graphics device and all checks should run outside Play mode.

For the latest art sample, use **Ashlands > Validation > Graphics > Polished Art Sample**. For the recent production/sliders/travel update, use **Ashlands > Validation > Workflows > Production and Army Travel**.

Duplicate shortcuts (including Hero Illustrations, World Map, 2.5D Isometric View and Reference Wasteland Art) are removed from the visible menu. Their checks remain covered by the grouped commands. Existing validation methods and gameplay code are unchanged; no scripts or save files should be deleted.

To organize this menu yourself, change only the path string in a C# `[MenuItem("Ashlands/Validation/Graphics/Polished Art Sample")]` attribute under `Assets/Ash/Editor`. Each slash creates a submenu. Removing an attribute hides that shortcut while keeping its method available to other code. Save the script and let Unity recompile; the menu refreshes automatically. This update already makes those changes.
