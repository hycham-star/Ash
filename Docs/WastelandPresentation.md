# Gathering indicators and wasteland building props

The world map displays the bus, bike and hero label during outbound/return travel. During resource gathering, the entire convoy disappears and the occupied resource landmark turns green (tinted art and green backing). Selection retains its gold outline. The normal landmark colour returns as soon as the last gathering army leaves. Multiple heroes gathering at a site keep it green until all leave. Raid dust is unchanged. Returning vehicles appear at their actual position, including after reloading a save.

Existing textured 3D buildings now receive original combined low-poly environment meshes: staggered sandbags, eight-sided drums, worn paving, roof repair sheets and corrugations, supported canvas shade and guy stays. Water stations gain wind-pump sails; workshops gain sheet-metal stock. Farms, housing, hospitals and player-built production plots reuse the appropriate kit. Models retain the fixed orthographic isometric view and existing textures. Props are children of the building, move with it and use no independent selection colliders. Each kit owns and disposes its generated meshes/materials. No external film assets are used.

No save version change, balance changes or new resource generation. The gathering mission remains active while the convoy is hidden.

## Verification

- Pure gameplay suite: 670 checks passed.
- C# syntax and asset/GUID checks passed. These do not compile against Unity APIs.
- Extended **Ashlands → Validate World Map** checks actual outbound/gathering/return visual states and site tint reset.
- Extended **Ashlands → Validate Settlement Presentation** checks all five building kits, fewer than 3,500 added triangles per kit, collider isolation and relocation.
- Unity Editor/Android unavailable in the cloud; the two native validators and visual/touch checks must run locally outside Play mode.

## Manual check

Open Settlement Scene and press Play. Inspect the buildings closely, then hold a building for 1.5 seconds and move it: all added props should follow. On the world map, dispatch an army to gather. Verify the bus/bike disappear when gathering starts and the resource site turns green. Recall or wait until its cargo is full: site colouring clears and the returning convoy appears at that site. Try two heroes at the same site, and save/restart during gathering to check restoration.
