# Exact army reinforcement quantities

The Settlement HEROES dossier now uses a whole-number slider instead of fixed +5 buttons.

1. Select your hero, then Infantry, Sharpshooter or Cavalry.
2. Drag the slider like a volume control. Its number updates immediately and previews the army total after adding. Dragging does not spend fighters.
3. Use **−1 / +1** for precise adjustments, then **ADD N** to confirm.
4. Or press **FILL +N**, at the right end of the slider, to add the maximum legal quantity of the selected fighter type immediately.

For an army at 48/50 with at least two matching reserve fighters, the slider allows 0–2 and Fill adds exactly two. If only one fighter remains in reserve, Fill adds one. It never generates free fighters or exceeds the hero's total capacity. Changing fighter type resets the slider to zero. Hero changes, battle casualties, XP/capacity changes and loaded saves refresh the bounds. Full armies and empty reserves disable assignment.

This updates the main Settlement hero panel. The separate Hero Army Test scene already supports exact numeric input; the Battle Scene keeps its debug army presets. Army rules, independent hero armies, reserves, casualty settlement and save formats are unchanged.

## Verify in Unity

Outside Play, run **Ashlands → Validate Army Reinforcement Controls**. It builds the actual slider/buttons in a disposable preview and exercises numeric changes, adding one fighter, Fill from 48 to 50, disabling a full army and reserve-limited Fill. It does not open player saves.

Then open Settlement Scene, press Play in 9:16, and check the HEROES panel on Unity and Android. Drag the slider, try −1/+1, ADD and FILL, switch all three fighter types and heroes, and replace a non-multiple-of-five raid loss. Check the number, army total, reserve consumption, touch targets and portrait layout. Save/restart after adding to confirm the existing persistence path.

The cloud can run the standalone rules suite, C# syntax and asset metadata checks. Native Unity compilation, the new editor validator and phone interactions cannot run here.
