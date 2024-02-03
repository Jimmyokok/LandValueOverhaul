## Introduction

- In the original game, your city will experience the following **death spiral**(s) once you run your city static (i.e. without building anything new) for enough time.
  - Your renters become rich→Land value explodes→Rent explodes→Some renters cannot afford
  - Either: Some renters cannot afford→Building not getting enough fund to pay upkeep→Building not upgrading/abandoned
  - Or: Some renters cannot afford→High rent warnings→Renters move away.
- The problem here is, once you stopped building new areas/building/zones, at least somewhere in your city will be experiencing this disgusting death spiral. The underlying core problem in the land value & rent mechanism, is **the renter's maximum affordable rent do not and should not equals the maximum rent that he is willing to pay, while the later one dominates the land value calculation**. Imagine an office company that makes 4 million profit every day, do you believe that it would even think about paying 4 million rent each day? 
- This mod makes a thorough overhaul on the game's land value update mechanism and the associated building upgrade mechanism. After installing this mod:
  - Extremely high profit/income from property renters will **no longer cause extremely high land value** (e.g. 10 million).
  - Land value will fade in **shorter distance** (200 instead of 2000).
  - Property renters that are **not willing to pay high rent** are now favored in the land value spreading process.
  - Building upkeep will increase **linearly with building level**, instead of exponentially.
  - Building upgrade cost will increase **slower with building level**, but still exponentially.
  - Area size of low density residential properties will be considered **1** in land value-related calculations.
  - Garbage fee is set to **0**.
- The effect of this mod is not instantly observable. **It takes time** (sometimes quite a lot) to adapt everything to the new mechanisms. 
- When you installed this mod and load a save previously played without this mod, massive instant building upgrading may happen. This is normal as this mod alters the building upgrade mechanism.
- For more details about the modifications made by this mod, please check the github repository (https://github.com/Jimmyokok/LandValueOverhaul).

## Requirements

- Game version 1.0.19f1.
- BepInEx 5

## Compatibility

- Modifies:
  - LandValueSystem
  - BuildingInitializeSystem
  - RentAdjustSystem
  - PropertyRenterSystem
- **NOT COMPATIBLE** with RentControl, Renter & LandValue Policy.

## Disclaimer

- This mod is experimental. It alters core game mechanisms, affects game data and has a short-term impact on saves. It may not be compatible with other mods. Please use at your own risk.
- This mod can be quite balance-breaking if not properly configurated.

## Credits

- [Captain-Of-Coit](https://github.com/Captain-Of-Coit/cities-skylines-2-mod-template): A Cities: Skylines 2 mod template.
- [BepInEx](https://github.com/BepInEx/BepInEx): Unity / XNA game patcher and plugin framework.
- [Harmony](https://github.com/pardeike/Harmony): A library for patching, replacing and decorating .NET and Mono methods during runtime.
- [CSLBBS](https://www.cslbbs.net): A chinese Cities: Skylines 2 community, for extensive test and feedback efforts.
