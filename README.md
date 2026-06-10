# QoL Combo Fix

A BepInEx plugin for Nuclear Option that combines several quality-of-life fixes and compatibility patches for the P2082 QoL mod and Aryx aircraft.

---

## Features

### Kestrel FBW Fix
Tunable fly-by-wire PID parameters for the FQ-106 Kestrel, replacing its stock oscillation-prone values. All gains are exposed in BepInEx config.
### F-16M FBW Fix
- **Center of Mass correction** — The F-16M's CoM is rear-heavy (Z = −0.06), causing persistent pitch-up and tail/engine damage when spawning. This fix shifts the CoM forward to a configurable Z position (default 0.5).
- Tunable FBW PID parameters (pFactorFast, dFactorFast, yaw/roll tightness, angular velocity limits).
### F-99 FBW Fix
Tunable FBW PID parameters for the F-99 Shrike, correcting its stock oscillation behaviour.
### Chimera Tail Weld Fix
Welds the MC-260 Chimera's top-rear fuselage panel into the airframe to stop it visibly twisting under V-tail load, with mass/CoM reconciliation and an impact-breaker for hard crashes. Also spawns the Chimera with Flight Assist off.
### Navex Ship Fixes
Recenters the wake VFX origin onto the hull centerline for big ships, fixing both a starboard-offset wake and a heading-independent (compass-direction) drift on the Atlas Class Supply Ship, Devotion Class Light Carrier, and Ironside Class Frigate. Also tunes the Andromeda class Cruiser's propulsion thrust and hull drag to reach its intended ~60 km/h top speed.

### QoL Integration
- **Suppress QoL debug logs** — Optional filter for the verbose `[Debug : qol]` log spam.
- **Hide P2082ModTag** — Hides the QoL version banner on the main menu.
- **Fix Drop Tank Pierce Damage** — Zeroes the pierce damage on `P_DropTank1_spent` so jettisoned tanks no longer destroy your aircraft.
- **F-16M Drop Tank** — Adds the DT-1600 drop tank as a loadout option on the F-16M's inner wing pylons.
- **Revert F-16M Gun** — Restores the F-16M's internal gun to the vanilla 20mm Rotary Cannon, undoing QoL's 25mm Autocannon rename.
- **F-16M / F-99 Access QoL 20mm** — Injects the Revoker's 20mm Rotary DP (and AP/HE/Stealth variants) as gun hardpoint options on the F-16M and F-99.

## Optional
- P2082 QoL mod (1.1.8.0)
- Aryx F-16 &
- Aryx Navex
