> [!NOTE]
Game version 1.1.0 has rewritten the land value mechanism. Now the land value is determined only by city services. Since the very machanism that this mod aims to improve no longer exists, this mod will not be updated.

> [!NOTE]
游戏版本1.1.0大改了地价机制。现在地价只和城市服务有关。因为本mod致力于优化的底层地价更新机制已经不再存在，本mod将不会更新。

## 中文用户注意
- 中文版的介绍位于Readme.pdf中，可以直接查看。

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
- More details about the modifications made by this mod is provided below.

## Requirements

- Game version 1.0.19f1.
- BepInEx 5

## Compatibility

- Modifies:
  - LandValueSystem
  - BuildingInitializeSystem
  - RentAdjustSystem
  - PropertyRenterSystem
  - BuildingUpkeepSystem
- **NOT COMPATIBLE** with RentControl, Renter & LandValue Policy.

## Changelog
- 1.4.3
  - Fix a vanilla flaw that generates a wave of delivery trucks when the building upkeep is near zero.
  - Set lower bound for the building upkeep, so that per-area, per-renter and per-game-update upkeep values are larger than zero.
  
- 1.4.2
  - Fix a bug that causes the "road required" icon to be missing when placing parks.

- 1.4.1
  - Fix a bug that causes the "road required" icon to be missing when placing service buildings.

- 1.4.0
  - Game version 1.0.19f1 compatibility.

- 1.3.0
  - Land value of road edges with no properties is fixed to zero.

- 1.2.0
  - Land value will not spread to road edges with no properties.

- 1.1.0
  - Road edges with zero land value will not affect nearby road edges.
  
## Details

### The Game's Land Value(LV), Rent & Building Upgrade Mechanisms

- The LV update mechanism: the land value is actually LV of **road edges**. The LV of a road edge will be updated according to the *maximum willing-to-pay rent (denoted as maxRent.x)* of all renters of all properties on the road edge. The game's original update rules are as follows:
    - For an individual renter, if $maxRent.x< 3 * SharedLV + SharedBuildingUpkeep$ , the renter feels the rent is too high.
    - If 50% of all renters feel the rent is too high, the LV will fall and vice versa.
- The LV spreading mechanism: both the land value and the land value weight of the two ends of a road edge will fade with distance. A fade factor controls its speed, with a default setting of 2000m.
- The game's original rent mechanism：
    - Rent of a property renter equals $SharedLV + SharedBuildingUpkeep$,
    - We denote the *maximum willing-to-pay rent as maxRent.x*. If $rent > maxRent.x$, rent will slowly move towards maxRent.x,
    - We denote the *maximum affordable rent as maxRent.y*. If $rent > maxRent.y$, **the renter will try to find other properties, preparing to move away**,
    - For residential property renters, maxRent.x is determined by numerous factors, such as their life quality, household income and life expenses. maxRent.x of residential renters will not exceed 45% of their household income. **Meanwhile, their maxRent.y equals maxRent.x**,
    - For company renters, their maxRent.x equals their after-tax profit and maxRent.y equals their employee-maximized after-tax profit,
    - This mechanism is neither reasonable nor realistic. **The renter's maximum affordable rent do not and should not equals the maximum rent that he is willing to pay.** The following case shows how explosive profit/income leads to explosive LV, where a 4\*4 office building is *willing to pay a 3.39million rent* and pushes the LV to 10.08 million ($InGameLV = 240 * LV$)：
<img src="img/Untitled.png" alt="Untitled" style="zoom: 100%;" />

- Meanwhile, the area of low density residential buildings are similar to medium and high density ones, while they only contain one household. Using ExtendedTooltips or the DeveloperMode, it can be clearly observed that **per-household rent** **of low densities are much higher** than medium and high densities on the same road edge.
- The building upgrade and abandon mechanism：
    - **Different from the game's description**, *all* rent paid by property renters contribute to the building upgrade progress, while **the building upkeep and the garbage fee is actually paid from the building upgrade progress**.
    - When $BuildingUpgradeProgress > BuildingUpgradeCost$, the building will upgrade.
    - When $BuildingUpgradeProgress < -BuildingUpgradeCost$, the building will be instantly abandoned, which means the **only cause of abandoned buildings is not enough rent is paid**.
    - The building upkeep cost is calculated as: $Upkeep=BaseUpkeep * AreaSize * BuildingLevel^k$, where k equals 1.3 for residential, 2.0 for industrial and 2.1 for office and commercial. For residential properties, the upkeep of level 5 buildings is 8.1 times of level 1 ones. And for office and commercial properties, the figure is 29.3.
    - The building upgrade cost is calculated as: $UpgradeCost=MaxRenterCount * BaseCost * 2^{BuildingLevel*2}$. Leveling from 4 to 5 cost 64 times more than leveling from 1 to 2.

### Basic Modifications

- Building upkeep mechanism is changed to: $Upkeep=\frac{BaseUpkeep * AreaSize * \sqrt{BuildingLevel+3}}{2}$. Now the upkeep of level 5 buildings is 1.414 times of level 1 ones.
- Building upgrade cost is changed to: $UpgradeCost=MaxRenterCount * BaseCost * 2^{BuildingLevel + 1}$。This modification only affects residential buildings, and for them, leveling from 4 to 5 cost only 8 times more than leveling from 1 to 2 now.

### MaxRent Modifications
$$
\displaylines{
maxRent.x(Modified)= maxRent.x, maxRent.x<SharedUpkeep\\
maxRent.x(Modified)=(1+log_{10}{\frac{maxRent.x}{SharedUpkeep}})*SharedUpkeep, maxRent.x >= SharedUpkeep.
}
$$
- Explanation: The obligation of a renter includes paying his shared building upkeep. And a richer renter is willing to pay more, but only slightly more (using log transformation to simulate). In this case, a company making 2 million profit every day is willing to pay a rent of ~50000 (instead of ~1.9 million), as much as 1.5 times the combined rent of all renters in a high density level 5 residential building.

### LV Mechanism Modification

- Distance fade factor is now 200m instead of 2000m.

- LV spreading will favor renters that feel the rent is too high. This mod implements a score-based update mechanism. It first calculates the rent payment willingness of individual renters according to *LV and the maximum willing-to-pay rent (maxRent.x)*:

$$
\displaylines{Rent =SharedLV+	SharedUpkeep\\
willingness=log_{10}{\frac{maxRent.x}{Rent}}.}
$$

- Then for every individual renter, a LV score is calculated. If the summed scores of all renters > 0, the LV falls and vice versa：

$$
\displaylines{
score = Sigmoid(willingness) - \frac{1}{2}, willingness>0\\
score = (1-willingness)*(Sigmoid(willingness) - \frac{1}{2}), willingness\leq 0.}
$$

### Other Modification

- Garbage fee is set to 0.
- When calculating shared LV, shared rent and shared Upkeep, $AreaSize=min(MaxRenterCount，AreaSize)$.

## Disclaimer

- This mod is experimental. It alters core game mechanisms, affects game data and has a short-term impact on saves. It may not be compatible with other mods. Please use at your own risk.
- This mod can be quite balance-breaking if not properly configurated.

## Credits

- [Captain-Of-Coit](https://github.com/Captain-Of-Coit/cities-skylines-2-mod-template): A Cities: Skylines 2 mod template.
- [BepInEx](https://github.com/BepInEx/BepInEx): Unity / XNA game patcher and plugin framework.
- [Harmony](https://github.com/pardeike/Harmony): A library for patching, replacing and decorating .NET and Mono methods during runtime.
- [CSLBBS](https://www.cslbbs.net): A chinese Cities: Skylines 2 community, for extensive test and feedback efforts.

## Test (In Chinese)

测试城市由四个主要测试片区组成，每个片区，之间都由公路隔开，防止地价蔓延(**公共交通不蔓延地价**)。

一区由高密度住宅、高密度商业、中密度混合住宅组成，已经全部都是五级：

<img src="img/Untitled%201.png" alt="Untitled" style="zoom: 33%;" />

二区完全由高密度办公组成，已经全部升到五级：

<img src="img/Untitled%202.png" alt="Untitled" style="zoom: 50%;" />

三区是彻底大杂烩，除了工业区什么都有：

<img src="img/Untitled%203.png" alt="Untitled" style="zoom:50%;" />

四区是包含有工业标志性建筑的工业区：

<img src="img/Untitled%204.png" alt="Untitled" style="zoom: 33%;" />

所有区域服务齐全，税率全城1%，**挂机数小时等城市完全稳定下来后**，结果如下：

<img src="img/Untitled%205.png" alt="Untitled" style="zoom: 33%;" />

<img src="img/Untitled%206.png" alt="Untitled" style="zoom: 50%;" />

<img src="img/Untitled%207.png" alt="Untitled" style="zoom:50%;" />

一区地价24万左右，高密度住宅平均租金180左右，中密度混合住宅300左右，没有鬼城现象

<img src="img/Untitled%208.png" alt="Untitled" style="zoom:33%;" />

<img src="img/Untitled%209.png" alt="Untitled" style="zoom:50%;" />

二区地价48万左右，全办公楼确实应该高，没毛病，4*4的办公楼租金在48000左右

<img src="img/Untitled%2010.png" alt="Untitled" style="zoom:50%;" />

<img src="img/Untitled%2011.png" alt="Untitled" style="zoom:50%;" />

<img src="img/Untitled%2012.png" alt="Untitled" style="zoom:67%;" />

<img src="img/Untitled%2013.png" alt="Untitled" style="zoom:67%;" />

<img src="img/Untitled%2014.png" alt="Untitled" style="zoom: 50%;" />

三区显示出了明显的地价分区现象，显示出此Mod有效遏制了地价蔓延。其中高密度住宅区和中央商务区地价约在13万，中密度区地价约在7万，低密度区地价约在6000-3万之间，混合住宅区地价在24000-90000之间。中密度住宅平均租金最高，约为250(不房屋逼仄是有代价的)，高密度约为150，而低密度约为50。

<img src="img/Untitled%2015.png" alt="Untitled" style="zoom: 33%;" />

<img src="img/Untitled%2016.png" alt="Untitled" style="zoom:33%;" />

<img src="img/Untitled%2017.png" alt="Untitled" style="zoom: 50%;" />

四区工业区地价较低，在最为赚钱(利润200万+)的燃料厂附近仅有4万多，工业适应性仍然全绿。

<img src="img/Untitled%2018.png" alt="Untitled" style="zoom:50%;" />

全城平均地价仅为18.5万，而如果免水电费，等到城市稳定后，地价也仅为19.9万，并不会像游戏原版中上涨到高密度住宅144万，办公区>1000万的恐怖状况。

改动后的建筑维护费约为高密度住宅=办公楼(五级1000)>中密度混合(五级640)>高密度商业(五级550)>中密度住宅(~300)>低密度商业>工业(~60)>低密度住宅(~1)。
