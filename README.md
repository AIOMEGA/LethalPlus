# LethalPlus

LethalPlus enhances Lethal Company by introducing numerous new features (currently in early development). As a hobbyist developer, I work on this project in my free time, so updates are expected to be slow but steady. You can download the mod from [Thunderstore](https://thunderstore.io/c/lethal-company/p/AIOMEGA/LethalPlus/) or [GitHub](https://github.com/AIOMEGA/LethalPlus). For any questions, concerns, or suggestions, visit my mods Discord threads [here](https://discord.com/channels/1168655651455639582/1187132207953870848) and [here](https://discord.com/channels/1169792572382773318/1187262152369770496).

## Features

### BeeHive Discombobulation

- **Functionality**: Using Spray Paint on a BeeHive triggers a unique effect. The bees become discombobulated for a brief period, allowing you to run off with the hive.
  - **Update v0.4.0**: 
    - Significantly reduced discombobulation time.
    - Rendered the bees docile (they can't attack while sprayed).
    - Fixed a bug where the timer was halved with each bee in-game.
    - Ensured that mistakenly spraying another object after the hive doesn't disable the effect.
    - Made it so spraying another hive while one is discombobulated disables the effect on the former and transfers it to the latter for balance.
    - Optimized the code substantially and reduced lag.

#### How It Works
- Distract the Bees by running past a Hive, then quickly return and spray it.
- If you aim correctly, the Bees will become docile for a short time.
- Note: Only one Hive can be sprayed at a time; spraying another transfers the effect from the last to the newest.
- Note: I test this mod solo, I'm unsure how it deals with multiple people using Spray Paint, my guess however is that for proper use only 1 should be used per group.

### Pepper Spray **v0.4.0** (Suggested by DrivingCrooner)
- **Functionality**: Using Spray Paint on an Enemy stuns them like the Bees, which monsters can be stunned and which will you die trying to stun?

#### How It Works
- Aim at the central part of the enemy's body. The Spray Paint can pass through most parts, potentially hitting a wall or floor instead. Therefore, attempting to spray an enemy involves a significant risk. (Improvements planned in a future update)
- A successful spray on a recognized part of the Enemy will freeze them like the Bees, but be cautious as they can still attack!

#### Future Improvement Plans for the Above Features
- Improve Enemy Colliders for easier stunning.
- Introduce random recovery times to increase unpredictability.
- Implement 3 different Enemy sprayed behaviors:
  - 1. Fight: The Enemy is blinded but attacks wildly based on sound and the player's last known location.
  - 2. Flight: The Enemy becomes startled and flees to avoid danger.
  - 3. Deer in Headlights (stun): Retains the current behavior.
- Increase price of Spray Paint for game balance.
- Ensure compatibility with other mods, such as Better Spray Paint.

*Stay tuned for more features and updates as LethalPlus evolves!*

## Future Features

### New Monsters
- **CopyCat**
- **Wraith**
- **Trade Reaper**
- **Stalker**
- **Fido**
- **Siren**
- **Endread**
### New Items
- **Silver Bullet Revolver**
- **Hacking Kit**
- **Monster Detector**
- **Monster Repellant**
- **Abort Button**
### Terminal Commands
### Vanilla Patches
### Additional Features akin to BeeHive Discombobulation and Pepper Spray
