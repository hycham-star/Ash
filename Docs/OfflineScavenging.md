# Offline scavenging

The current behavior is documented in [Scavenger teams](ScavengerTrips.md).

Assigned teams gather while away, up to the existing eight-hour catch-up limit. Each worker carries 100 resources. Full teams, or teams exhausting their site, return and deliver automatically. They remain idle until explicitly sent again. There is no visible travel and no automatic repeat.

Partial cargo remains with an active team. The offline summary reports resources carried, resources delivered and workers returned. Repeated resumes or loads cannot replay a committed interval. Resource overflow protects cargo rather than discarding it.

`OfflineScavenging` supplies UTC elapsed time to the same `ScavengingService.Advance` used for foreground play. The high-water mark prevents backwards-clock rewards and consumes excess time beyond the catch-up limit. The local-device clock is not a trusted server clock. Settlement Garden/Water production remains foreground-only.

Save version 5 accepts versions 1–4 and preserves existing progression. GameManager checkpoints resources, cargo, assignments and the UTC watermark together using the existing atomic save and backup path.

Run **Ashlands → Validation → World Map → Offline Scavenging** outside Play for controlled-clock Unity lifecycle, native JSON/disk restart, partial recall, full-load return and no-repeat validation. It uses unique temporary saves. The cloud rules suite also exercises clock rollback, interval caps, partial work, save failures and recovery. Native Unity and Android checks remain necessary.
