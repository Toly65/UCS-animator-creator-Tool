# UCS Animator Creator Tool

Generates the Animator layers a UdonCombatSystem gun needs (slide tracking, fire cycle, charging handle, bullet visibility) so you don't have to wire up states and transitions by hand.

**Read the [UCS wiki: Gun Setup](https://github.com/Toly65/UdonCombatSystem/wiki/Gun-Setup) page first** — it covers what the clips need to contain, the physbone setup that drives `SlideStretch`, and how the generated parameters hook into the gun scripts. This README only covers the tool window itself.

## Dependency

[Animator As Code v1 (hai-vr/av3-animator-as-code)](https://github.com/hai-vr/av3-animator-as-code) must be installed, or the editor script won't compile. Install it by adding Hai's VPM listing to VCC/ALCOM — see [docs.hai-vr.dev/docs/products/listing](https://docs.hai-vr.dev/docs/products/listing) for the add-repo link — then add the Animator As Code package to this project.

## Usage

1. `Tools > Gun Slide > Animator Generator`
2. Select the gun GameObject and hit **Populate From Selected GameObject** to fill the Animator and root (or assign them manually).
3. Assign an **Asset Container** — the asset the generated clips/state machines get written into. Leave empty to let AAC pick.
4. Assign the animation clips for each section.
5. **Generate**. Re-running overwrites the generated layers; hand edits to them are lost.

## Options

| Option | Use when |
|---|---|
| Include Charge Handle Layer | The charging handle moves only when pulled, not when firing (e.g. M4). |
| Manual Lock Only | The slide latches only when the player racks it, never on the last round (e.g. G3). Hides the Fire Cycle Lock clip. |
| Include Charge Handle Grab Layer | The handle pivots out when grabbed (e.g. G3). |

## Generated layers

- **SlideLayer** — slide position driven by `SlideStretch`, with locked/returning states.
- **ChargeHandleLayer** / **ChargeHandleGrabLayer** — optional, per the toggles above.
- **FireCycleLayer** — overrides the slide during auto-cycling.
- **BulletLayer** — bullet mesh visibility.

## Animator parameters

| Parameter | Type | Written by |
|---|---|---|
| `SlideStretch` | Float | Udon reading PhysBone stretch |
| `SlideLocked` | Bool | Udon |
| `SlideGrabbed` | Bool | Udon (`UCS_SliderHandler`) |
| `IsFiring` | Trigger | Udon, per shot |
| `IsFiringLock` | Bool | Udon (last round) |
| `BulletVisible` | Bool | Udon |

## Notes

- All assigned clips have looping **disabled** on generation — the tool edits the clip assets themselves.

