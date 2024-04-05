> [!NOTE]
> **LandValueOverhaul v1.5.0 is available via PDXMods.** The Bepinex version will be released very soon. Meanwhile, **feedbacks** are still needed and most welcomed.

> [!NOTE]
> **地价机制大修 v1.5.0 已经在PMOD上线。**BepInEx版本将会很快发布。同时，关于本MOD的游戏体验，仍然需要更多玩家**反馈**。

Overhaul on the game's land value, rent and building upgrade mechanisms.

## Why needed?

The vanilla land value system, along with the rent and building upgrade system, are designed without careful thoughts and lack sufficient testing and polishing, no matter before or after the game update 1.1.0. Before 1.1.0, the market-based land value system has led to:

* High-density residents, even rich and educated, are struggling to pay their rents. Land value of these estates could climb to 1,200,000$.
* Commercial zones are constantly yielding "Not enough customer" warnings, even when they are built in reasonable locations.
* Low-density residential buildings are dying simply because they share the same road with office/high-density residential buildings.
* The land value is instantly spreading from city centers to remote and less-urbanized regions along major roads.
* The city's average land value is unusually high and still climbing above, let's say, 2,000,000$.

The 1.1.0 update, however, brutely binds the land value with merely five factors: transportation, healthcare, education, police, and commercial coverages. it is still reported that:

* The low-density high-rent issue persists.
* Even higher rent for commercial zones, causing more "Not enough customer" warnings.
* Land value and rent for high-density residential areas are too low to complement their upkeep.
* The land value tooltip system is lying on the real land value, causing confusion.

This mod makes a **thorough overhaul** on the game's **land value update mechanism**, the associated **building upgrade mechanism** along with some extra small fix. Please see `README_1.4.3.pdf` for details and full descriptions of the modifications this mod made to vanilla systems. The below list is only a short overview.

## Feature

* Implemented a soft market-based land value(LV) update mechanism, which is the combination of the LV system in LandValueOverhaul 1.4.3(See `README_1.4.3.pdf` for details), and the vanilla 1.1.0 LV system. This system aims to:
	* Make the LV reasonable, realistic and dynamic.
	* Make the corresponding rent comfortable for most renters(instead of 50%).
	* Make building upgrade more smooth and reduce abandoned buildings.
* The LV will stablizes at a level that is determined by the market(most acceptable by nearby property renters). But property renters that are not willing to pay high rent are now **favored** in the land value update mechanism, ensuring the **majority** of renters along a road edge, instead of 50% of them, can afford their rents.
* Land value will fade in a **shorter** distance(200 instead of 2000, configurable), also in a **slower** speed(0.001 instead of 0.01, configurable), resulting in a more cliffy land value distribution, which is more realistic.
* Building upkeep will increase **linearly** with building level, instead of exponentially. This effectively reduces the rent, causing less high-rent warnings.
* Building upgrade cost will increase **slower** with building level, but still exponentially. This effectively speeds up the building upgrade, making it easier for high-density residentials getting to level 5.
* Area size of low density residential properties will be considered **square root of the area size** in land-value-related computations, to avoid extremely high per-family rent for low-density residentials.
* Garbage fee will **no longer decrease** the building upgrade progress.
* The land value tooltip system now **correctly displays the real land value**.

## Important Notes

* Bankrupting stores and companies are still most likely to yield high-rent warnings, since they cannot pay anything.
* The overall system is BETA and still need more testing. FEEDBACKS are most welcomed(via Github issues).
* The land-value-related effect of this mod is **not instantly observable**. It takes time (sometimes quite a lot) to adapt everything to the new mechanisms. The land value will require multiple in-game days to re-stablize.
* The building-related effect of this mod is **instantly observable**. Massive floods of building upgrade are possible when loading existing save games played without this mod.
* The configurable options are applied only once, during the mod initialization. For new setting to take effect, you need to **restart the game**.
* This mod affects save games, as it modifies the land value. **Backup your save before installing!**
* This mod affects the game's pop simulation, which would possibly result in unexpected and unrelated behaviors (e.g. Suddenly everyone wants to move into the city).
* This mod conflicts with any mod that makes changes to land-value-related systems.

## Technical

* Modified systems: BuildingInitializeSystem, BuildingUpkeepSystem, LandValueSystem, PropertyRenterSystem, RentAdjustSystem, LandValueDebugSystem, LandValueTooltipSystem, OverlayInfomodeSystem.

## Changelog
- v1.5.4

  * Rewrite the building upkeep modification, so that it is guaranteed to be correctly loaded.

- v1.5.2 and v1.5.3

  * Fix a bug that causes LV to endlessly increase where there is no healthcare coverage.

- v1.5.1

  * Fix a bug that led to colorless info view and the land value node-level debug view not showing up.

- v1.5.0

  * Mod migrated to PDXMods platform and updated for v1.1.0 of the game.

  * Combined the new vanilla land value system to the modded system.

  * Adjusted size computation for low-density residentials from 1x1 to 1xN.

  * Corrected the vanilla land value tooltip system.

  * Rebalance the building upgrade costs to be higher than previous versions but still lower than vanilla.


- v1.4.3

  * Fix a vanilla flaw that generates a wave of delivery trucks when the building upkeep is near zero.

  * Set lower bound for the building upkeep, so that per-area, per-renter and per-game-update upkeep values are larger than zero.


- v1.4.2
  * Fix a bug that causes the "road required" icon to be missing when placing parks.


- v1.4.1
  * Fix a bug that causes the "road required" icon to be missing when placing service buildings.


- v1.4.0
  * Game version 1.0.19f1 compatibility.


- v1.3.0
  * Land value of road edges with no properties is fixed to zero.


- v1.2.0
  * Land value will not spread to road edges with no properties.


- v1.1.0
  * Road edges with zero land value will not affect nearby road edges.


- v1.0.0
  * Mod exists.
