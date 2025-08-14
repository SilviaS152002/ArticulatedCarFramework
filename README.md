For users: Go to [Releases](https://github.com/SilviaS152002/ArticulatedCarFramework/releases).

For modders:

Defining the car:
Rather than the original "modelIdentifier" under "definition", it uses each individual "modelIdentifier" under each pivot in "pivots".

The original "modelIdentifier" string is instead reused for loading a (preferably empty) gameObject that has nothing more than the AnimationMap and MaterialMap of the model.

The bundle structure should look something like the steam locomotive ones:
```
bundle
├ carBodyF
├ carBodyM
├ carBodyR
├ truckPivotBodyAddonBits
└ carMaps // modelIdentifier should point to this 
```

Basically, add this in "definition":

```C#
"pivots":
{
  [
    "name":              // At least one pivot must have the name "mainPivot", preferably in the center
    "modelIdentifier":   // put your model identifier here, do not define / leave blank / null for no model
    "truckIdentifier":   // same as above, if this is defined then this pivot will be treated as a truck
    "pivotA":            // name of the front pivot inside this list of pivots that the current pivot should base their position on. Do not define / leave blank / null if you have defined a truck
    "offsetA":           // amount of offset that should be applied forwards / backwards with respect to pivotA
                         // Imagine a beam attached lengthwise to pivotA and this is how far forward/backward from the center of pivotA you attach the pivot point onto this beam
    "pivotB":            // same, but for the back pivot
    "offsetB":           // same, but for the back pivot
    "position":          // how far forward / backwards this pivot is with respect to the "center" of the car
  ],
  [
    // other pivots
  ]
},
"endGearParentF": // name of pivot in Pivots that the coupler should base its movement around
"endGearParentR": // same as above
```

Adjust "length" of car to be the length of *the entire car*.

Known issues:
1. if mainPivot has a model, it may not load in at first
   - [x] does not affect gameplay
   - [x] Should be fixed, but may reappear
2. Camera will follow mainPivot making it difficult to do stuff at the ends of the car (esp. with longer articulated cars)
   - [x] ~Working on it~ Done, will be in beta10
3. Editor saving will cause a "identifier not found" error
   - [x] Does not appear to affect saving the definition files, probably will not fix
4. Component parents cannot be set in editor
   - [ ] Due to how model hierarchy is loaded, probably can't fix
   - [ ] Create the components, save, exit, manually set component parents, then go back into editor
   - [ ] Note that first line in the component parent path should be name of a pivot in "pivots" list

Includes a teeny tiny little patch such that first-person car-pushing is based on your facing direction instead of where you are relative to the center of the car.
