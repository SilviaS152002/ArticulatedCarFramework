using AssetPack.Common;
using AssetPack.Runtime;
using Audio;
using Game;
using HarmonyLib;
using Helpers;
using KeyValue.Runtime;
using Model;
using Model.Database;
using Model.Definition;
using Model.Definition.Data;
using RollingStock;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Track;
using UnityEngine;
using static Track.PrefabInstancer;

namespace NS15
{
    namespace ArticulatedCarFramework
    {
        public class ArticulatedPivot : MonoBehaviour
        {
            internal ArticulatedPivotDefinition Definition;

            internal float pivotRatio = 0.5f;

            internal bool IsWheelset => !string.IsNullOrEmpty(Definition.TruckIdentifier);

            internal ArticulatedPivot parentA;

            internal ArticulatedPivot parentB;


            internal CarMover mover = new CarMover();


            internal Graph.PositionRotation posRot;

            public Graph.PositionRotation GetPosRot()
            {
                if (!IsWheelset)
                {
                    Graph.PositionRotation posRotA = parentA.GetPosRot();
                    Graph.PositionRotation posRotB = parentB.GetPosRot();
                    if (posRotA.Position == posRotB.Position) { return posRotA; }
                    posRotA.Position += posRotA.Rotation * (Vector3.forward * Definition.OffsetA);
                    posRotB.Position += posRotB.Rotation * (Vector3.forward * Definition.OffsetB);
                    posRot = new Graph.PositionRotation
                    {
                        Position = Vector3.Lerp(posRotA.Position, posRotB.Position, pivotRatio),
                        Rotation = Quaternion.LookRotation(posRotA.Position - posRotB.Position, Vector3.Lerp(posRotA.Rotation * Vector3.up, posRotB.Rotation * Vector3.up, .5f))
                    };
                }
                return posRot;
            }
            public void Move(bool isFirstPosition)
            {
                mover.Move(WorldTransformer.GameToWorld(posRot.Position), posRot.Rotation, isFirstPosition);
            }
        }
        public class ArticulatedPivotDefinition
        {
            public string Name { get; set; }
            public string ModelIdentifier { get; set; }
            public string TruckIdentifier { get; set; }

            public string PivotA { get; set; }
            public float OffsetA { get; set; }

            public string PivotB { get; set; }
            public float OffsetB { get; set; }
            public float Position { get; set; }


        }
        public class ArticulatedCarDefinition : CarDefinition
        {
            public override string Kind { get; } = "ArticulatedCar";
            public new string TruckIdentifier { get; set; } = null;
            // public float LengthOffset;
            public List<ArticulatedPivotDefinition> Pivots { get; set; }
            public string EndGearPivotF { get; set; } = null;
            public string EndGearPivotR { get; set; } = null;
        }

        public partial class ArticulatedCar : Car
        {
            private TrainController TrainController // Harmony bridge
            {
                get => (TrainController)AccessTools.Property(typeof(Car), "TrainController").GetValue(this);
                set => AccessTools.Property(typeof(Car), "TrainController").SetValue(this, value);
            }
            private Dictionary<string, Task<LoadedAssetReference<GameObject>>> _modelLoadTasks // Harmony bridge
            {
                get => (Dictionary<string, Task<LoadedAssetReference<GameObject>>>)AccessTools.Field(typeof(Car), "_modelLoadTasks").GetValue(this);
                set => AccessTools.Field(typeof(Car), "_modelLoadTasks").SetValue(this, value);
            }

            private Dictionary<string, Task<Wheelset>> _truckPrefabLoadTasks = new Dictionary<string, Task<Wheelset>>();
            private List<Renderer> _truckRenderers // Harmony bridge
            {
                get => (List<Renderer>)AccessTools.Field(typeof(Car), "_truckRenderers").GetValue(this);
                set => AccessTools.Field(typeof(Car), "_truckRenderers").SetValue(this, value);
            }

            public List<Graph.PositionRotation> _truckGizmoPosRots = new List<Graph.PositionRotation>();
            private bool _modelLoadPending // Harmony bridge
            {
                get => (bool)AccessTools.Field(typeof(Car), "_modelLoadPending").GetValue(this);
                set => AccessTools.Field(typeof(Car), "_modelLoadPending").SetValue(this, value);
            }
            private float swayPosition
            {
                get => (float)AccessTools.Field(typeof(Car), "swayPosition").GetValue(this);
                set => AccessTools.Field(typeof(Car), "swayPosition").SetValue(this, value);
            }
            protected override void OnDrawGizmosSelected()
            {
                DrawLine(WheelBoundsF, Color.Lerp(Color.green, Color.red, 0.25f));
                DrawLine(WheelBoundsR, Color.Lerp(Color.green, Color.red, 0.75f));
                DrawLine(LocationA, Color.green);
                DrawLine(LocationB, Color.red);
                _truckGizmoPosRots.ForEach(DrawTruckLine);
                static void DrawLine(Location loc, Color color)
                {
                    if (loc.IsValid)
                    {
                        Vector3 vector = Vector3.up * 0f;
                        Vector3 vector2 = Graph.Shared.GetPosition(loc).GameToWorld() + vector;
                        Gizmos.color = color;
                        Gizmos.DrawRay(vector2, Vector3.up);
                    }
                }
                static void DrawTruckLine(Graph.PositionRotation pr)
                {
                    Vector3 vector = Vector3.right * 1.435f / 2f;
                    Gizmos.color = Color.yellow;
                    Vector3 vector2 = WorldTransformer.GameToWorld(pr.Position);
                    Vector3 vector3 = vector2 + pr.Rotation * vector;
                    Vector3 to = vector2 + pr.Rotation * -vector;
                    Gizmos.DrawLine(vector3, to);
                }
            }
            internal Renderer[] GetRenderers(GameObject obj) => (Renderer[])AccessTools.Method(typeof(Car), "GetRenderers").Invoke(this, [obj]);
            internal void MakeMaterialsUnique(GameObject obj, IReadOnlyCollection<Renderer> renderers) => _ = AccessTools.Method(typeof(Car), "MakeMaterialsUnique").Invoke(this, [obj, renderers]);
            private AudioReparenter _audioReparenter // Harmony bridge
            {
                get => (AudioReparenter)AccessTools.Field(typeof(Car), "_audioReparenter").GetValue(this);
                set => AccessTools.Field(typeof(Car), "_audioReparenter").SetValue(this, value);
            }
            private Renderer[] _bodyRenderers // Harmony bridge
            {
                get => (Renderer[])AccessTools.Field(typeof(Car), "_bodyRenderers").GetValue(this);
                set => AccessTools.Field(typeof(Car), "_bodyRenderers").SetValue(this, value);
            }
            private CarMover _mover // Harmony bridge
            {
                get => (CarMover)AccessTools.Field(typeof(Car), "_mover").GetValue(this);
                set => AccessTools.Field(typeof(Car), "_mover").SetValue(this, value);
            }
            private new bool IsInDidLoadModels // Harmony bridge
            {
                get => (bool)AccessTools.Property(typeof(Car), "IsInDidLoadModels").GetValue(this);
                set => AccessTools.Property(typeof(Car), "IsInDidLoadModels").SetValue(this, value);
            }
            private bool _isVisible // Harmony bridge
            {
                get => (bool)AccessTools.Field(typeof(Car), "_isVisible").GetValue(this);
                set => AccessTools.Field(typeof(Car), "_isVisible").SetValue(this, value);
            }
            private Graph Graph // Harmony bridge
            {
                get => (Graph)AccessTools.Property(typeof(Car), "Graph").GetValue(this);
                set => AccessTools.Property(typeof(Car), "Graph").SetValue(this, value);
            }
            internal void CancelDelayedUnload() => _ = AccessTools.Method(typeof(Car), "CancelDelayedUnload").Invoke(this, null);
            internal void RemoveAllOilPointPickables() => _ = AccessTools.Method(typeof(Car), "RemoveAllOilPointPickables").Invoke(this, null);
            private List<Material> _ownedMaterials // Harmony bridge
            {
                get => (List<Material>)AccessTools.Field(typeof(Car), "_ownedMaterials").GetValue(this);
            }
            private EndGear EndGearF // Harmony bridge
            {
                get => (EndGear)AccessTools.Field(typeof(Car), "EndGearF").GetValue(this);
                set => AccessTools.Field(typeof(Car), "EndGearF").SetValue(this, value);
            }
            private EndGear EndGearR // Harmony bridge
            {
                get => (EndGear)AccessTools.Field(typeof(Car), "EndGearR").GetValue(this);
                set => AccessTools.Field(typeof(Car), "EndGearR").SetValue(this, value);
            }
            private List<ICarMovementListener> _movementListeners
            {
                get => (List<ICarMovementListener>)AccessTools.Field(typeof(Car), "_movementListeners").GetValue(this);
            }

            const string mainPivotString = "mainPivot";

            internal float positionHead;
            internal float positionTail;
            // protected override float CalculateCarLength() => positionHead - positionTail;
            protected override float OffsetToEnd(End end, float extra = 0f) => (end == End.F) ? (positionHead + extra) : (positionTail - extra);
            private string AnglecockTooltipText(Anglecock anglecock) => (string)AccessTools.Method(typeof(Car), "AnglecockTooltipText").Invoke(this, [anglecock]);
            private (Animator, AnimationMap) SetupForAnimation() => ((Animator, AnimationMap))AccessTools.Method(typeof(Car), "SetupForAnimation").Invoke(this, null);
            private void SetupBrakeAnimations() => _ = AccessTools.Method(typeof(Car), "SetupBrakeAnimations").Invoke(this, null);
            private void ResetEndGearPositions() => _ = AccessTools.Method(typeof(Car), "ResetEndGearPositions").Invoke(this, null);
            private static void UpdateAnglecockControl(Anglecock anglecock, float value, float flow, bool force) => _ = AccessTools.Method(typeof(Car), "UpdateAnglecockControl").Invoke(null, [anglecock, value, flow, force]);
            private void SetupComponents(ComponentSetup.Context ctx, ComponentLifetime lifetime) => _ = AccessTools.Method(typeof(Car), "SetupComponents").Invoke(this, [ctx, lifetime]);
            private Vector3 CarCouplerPivot(Car car, End end, float extra) => (Vector3)AccessTools.Method(typeof(Car), "CouplerPivot").Invoke(car, [end, extra]);
            private List<GameObject> _oilPointPickables  => (List<GameObject>)AccessTools.Field(typeof(Car), "_oilPointPickables").GetValue(this);






        }
        public partial class ArticulatedCar : Car
        {
            public new ArticulatedCarDefinition Definition => (ArticulatedCarDefinition)DefinitionInfo.Definition;

            public List<ArticulatedPivot> Pivots = new List<ArticulatedPivot>();

            public ArticulatedPivot MainPivot;
            public ArticulatedPivot EndGearParentF;
            public ArticulatedPivot EndGearParentR;

            public GameObject Spacer;
            protected override void FinishSetup()
            {
                base.FinishSetup();

                Spacer = new GameObject("spacer");
                Spacer.transform.parent = this.transform;

                foreach (ArticulatedPivotDefinition pivotDef in Definition.Pivots)
                {
                    GameObject pivotObject = new GameObject(pivotDef.Name);
                    pivotObject.transform.SetParent(Spacer.transform);
                    ArticulatedPivot pivot = pivotObject.AddComponent<ArticulatedPivot>();
                    pivot.Definition = pivotDef;
                    pivot.mover.DebugId = $"Pivot {this}:{pivotDef.Name}";
                    // Debug.Log($"Configured mover for body {pivot}");
                    pivot.transform.localPosition = Vector3.forward * pivot.Definition.Position;
                    pivot.transform.localRotation = Quaternion.identity;
                    Pivots.Add(pivot);
                    pivot.mover.ConfigureForBody(pivot.gameObject);
                }


                foreach (ArticulatedPivot pivot in Pivots)
                {
                    if (pivot.IsWheelset) { continue; }
                    pivot.parentA = Pivots.First(p => p.name == pivot.Definition.PivotA) ?? throw new ArgumentNullException($"PivotA of pivot {name} does not exist.");
                    pivot.parentB = Pivots.First(p => p.name == pivot.Definition.PivotB) ?? throw new ArgumentNullException($"PivotB of pivot {name} does not exist.");
                    pivot.pivotRatio = (pivot.Definition.Position - (pivot.parentA.Definition.Position + pivot.Definition.OffsetA)) / ((pivot.parentB.Definition.Position + pivot.Definition.OffsetB) - (pivot.parentA.Definition.Position + pivot.Definition.OffsetA));
                }

                MainPivot = Pivots.Find(p => p.name == mainPivotString) ?? throw new ArgumentNullException(nameof(MainPivot), $"\"{mainPivotString}\" not loaded");

                _mover = MainPivot.mover;

                // Debug.Log($"Found mainPivot for {this}");

                EndGearParentF = Pivots.Find(p => p.name == Definition.EndGearPivotF) ?? MainPivot;
                EndGearParentR = Pivots.Find(p => p.name == Definition.EndGearPivotR) ?? MainPivot;

                positionHead =  carLength / 2f - EndGearParentF.Definition.Position;
                positionTail = -carLength / 2f - EndGearParentR.Definition.Position;

                List<ArticulatedPivot> wheelsetPivots = Pivots.Where(p => p.IsWheelset).OrderByDescending(pivot => pivot.Definition.Position).ToList();

                wheelInsetF = Mathf.Max(0.3f, carLength / 2f - wheelsetPivots.First().Definition.Position - 1f);
                wheelInsetR = Mathf.Max(0.3f, carLength / 2f + wheelsetPivots.Last().Definition.Position - 1f);
                // Debug.Log($"wheelInsetF: {wheelInsetF}, wheelInsetR: {wheelInsetR}");

            }

            internal async void LoadModelsAsync()
            {
                try
                {
                    IPrefabStore prefabStore = TrainController.PrefabStore;

                    foreach (ArticulatedPivot pivot in Pivots)
                    {
                        if (!string.IsNullOrEmpty(pivot.Definition.ModelIdentifier) && !_modelLoadTasks.TryGetValue(pivot.Definition.ModelIdentifier, out _))
                        {
                            string modelIdentifier = pivot.Definition.ModelIdentifier;
                            string assetPackIdentifier = prefabStore.AssetPackIdentifierContainingDefinition(DefinitionInfo.Identifier);
                            _modelLoadTasks[modelIdentifier] = prefabStore.LoadAssetAsync<GameObject>(assetPackIdentifier, modelIdentifier, CancellationToken.None);
                        }
                        if (pivot.IsWheelset && !_truckPrefabLoadTasks.TryGetValue(pivot.Definition.TruckIdentifier, out _))
                        {
                            _truckPrefabLoadTasks[pivot.Definition.TruckIdentifier] = prefabStore.TruckPrefabForId(pivot.Definition.TruckIdentifier);
                        }
                    }

                    if (!string.IsNullOrEmpty(Definition.ModelIdentifier) && !_modelLoadTasks.TryGetValue(Definition.ModelIdentifier, out _))
                    {
                        string modelIdentifier = Definition.ModelIdentifier;
                        string assetPackIdentifier = prefabStore.AssetPackIdentifierContainingDefinition(DefinitionInfo.Identifier);
                        _modelLoadTasks[modelIdentifier] = prefabStore.LoadAssetAsync<GameObject>(assetPackIdentifier, modelIdentifier, CancellationToken.None);
                    }

                    await Task.WhenAll(_modelLoadTasks.Values);
                    await Task.WhenAll(_truckPrefabLoadTasks.Values);
                }
                catch (Exception exception)
                {
                    Log.Error(exception, $"Error loading car model or trucks for {DefinitionInfo.Identifier}");
                    return;
                }
                HandleModelsLoaded();
            }

            internal void HandleModelsLoaded()
            {
                // Debug.Log($"{this} called HandleModelsLoaded");
                if (!_modelLoadPending)
                {
                    Log.Debug("{car} Car unloaded before HandleModelsLoaded.", DisplayName);
                    return;
                }
                if (this == null)
                {
                    Log.Debug("{car} Car deallocated before HandleModelsLoaded.", DisplayName);
                    return;
                }

                List<Renderer> bodyRenderers = new List<Renderer>();


                foreach (ArticulatedPivot pivot in Pivots)
                {

                    if (!string.IsNullOrEmpty(pivot.Definition.ModelIdentifier))
                    {
                        GameObject modelObject = UnityEngine.Object.Instantiate(_modelLoadTasks[pivot.Definition.ModelIdentifier]?.Result.Asset, pivot.transform) ?? throw new ArgumentNullException(nameof(pivot.name), $"Failed to load model \"{pivot.Definition.ModelIdentifier}\"");
                        modelObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                        bodyRenderers.AddRange(GetRenderers(modelObject));
                    }
                }

                if (!string.IsNullOrEmpty(Definition.ModelIdentifier))
                {
                    GameObject modelObject = UnityEngine.Object.Instantiate(_modelLoadTasks[Definition.ModelIdentifier]?.Result.Asset, MainPivot.transform) ?? throw new ArgumentNullException(this.name, $"Failed to load model \"{Definition.ModelIdentifier}\"");
                    modelObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    // bodyRenderers.AddRange(GetRenderers(modelObject));
                }

                Pivots.Remove(MainPivot);
                BodyTransform = MainPivot.transform;
                _audioReparenter.BodyTransform = BodyTransform;

                _bodyRenderers = bodyRenderers.ToArray();

                MakeMaterialsUnique(Spacer.gameObject, _bodyRenderers);

                Spacer.SetActive(value: false);

                try
                {
                    IsInDidLoadModels = true;
                    DidLoadModels();
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Exception from DidLoadModels {car}", this);
                }
                finally
                {
                    IsInDidLoadModels = false;
                }
                Spacer.SetActive(value: true);
                SetVisible(_isVisible);
                DidSetBodyActive();

                if (WheelBoundsF.IsValid)
                {
                    PositionWheelBoundsFront(WheelBoundsF, Graph, MovementInfo.Zero, update: true);
                }

                ((Delegate)AccessTools.Field(typeof(Car), "OnDidLoadModels").GetValue(this))?.DynamicInvoke();

                // Debug.Log($"{this} exiting HandleModelsLoaded");
            }

            protected override void DidLoadModels()
            {

                SetupPivotWheelsets();

                SetupPrefabs setupPrefabs = _setupPrefabs;
                GameObject bodyObject = BodyTransform.gameObject;

                // _mover.ConfigureForBody(bodyObject);
                // SetupTrucks();

                SetupBrakeAnimations();
                SetupEndGear(_setupPrefabs);

                _rollingPlayer = Spacer.AddComponent<RollingPlayer>();
                _rollingPlayer.profile = _setupPrefabs.RollingProfile;

                Spacer.gameObject.AddComponent<CarPickable>().car = this;

                CarColorController carColorController = Spacer.gameObject.AddComponent<CarColorController>();

                _isFirstPosition = true;
                ResetEndGearPositions();
                UpdateAnglecockControl(EndGearA.Anglecock, EndGearA.AnglecockSetting, air.anglecockFlowA, force: true);
                UpdateAnglecockControl(EndGearB.Anglecock, EndGearB.AnglecockSetting, air.anglecockFlowB, force: true);

                AnimationMap animationMap = SetupForAnimation().Item2;
                ComponentSetup.Context ctx = new ComponentSetup.Context
                {
                    AnimationMap = animationMap,
                    MaterialMap = Spacer.GetComponentInChildren<MaterialMap>(),
                    CarColorController = carColorController
                };
                SetupComponents(ctx, ComponentLifetime.Model);

                MainPivot.transform.SetLocalPositionAndRotation(WorldTransformer.GameToWorld(MainPivot.posRot.Position), MainPivot.posRot.Rotation);

                // Debug.Log($"MainPivot at {MainPivot.transform.position}, {MainPivot.transform.rotation}");

                if (ghost) { GetComponentsInChildren<Collider>().ToList().ForEach(c => { if (c.isTrigger) { c.enabled = false; } }); }
            }


            private void SetupPivotWheelsets()
            {
                // Debug.Log($"{this} in SetupPivots");
                WheelClackProfile wheelClackProfile = TrainController.Shared.wheelClackProfile;
                int truck_idx = 65;

                List<ArticulatedPivot> wheelsetPivots = Pivots.Where(p => p.IsWheelset).ToList();

                if (MainPivot.IsWheelset) { wheelsetPivots.Add(MainPivot); }

                foreach (ArticulatedPivot pivot in wheelsetPivots)
                {
                    if (pivot.IsWheelset)
                    {
                        if (!_truckPrefabLoadTasks.TryGetValue(pivot.Definition.TruckIdentifier, out Task<Wheelset> truckPrefab))
                        {
                            throw new ArgumentNullException($"Pivot {pivot} in {this} has defined truck {pivot.Definition.TruckIdentifier} with no result");
                        }

                        Wheelset truck = UnityEngine.Object.Instantiate(truckPrefab.Result, pivot.transform, worldPositionStays: false);
                        truck.name = $"Truck {(char)truck_idx}";
                        truck_idx++;
                        truck.Configure(wheelClackProfile, this);
                        truck.SetLinearOffset(_linearOffset - pivot.Definition.Position);
                        BrakeAnimators.Add(truck);
                        Renderer[] renderers = GetRenderers(truck.gameObject);
                        MakeMaterialsUnique(truck.gameObject, (IReadOnlyCollection<Renderer>)(object)renderers);
                        _truckRenderers.AddRange(renderers);
                        if (EnableOiling)
                        {
                            float diameter = (float)AccessTools.Field(typeof(Wheelset), "diameterInInches").GetValue(truck) / 39.37008f;
                            float axleSeparation = truck.CalculateAxleSpread();
                            AddOilPointPickable(pivot.Definition.Position, axleSeparation, diameter);
                            _oilPointPickables.Last().transform.SetParent(truck.transform);
                            _oilPointPickables.Last().transform.localPosition = Vector3.zero;
                        }
                    }
                }
            }

            private void SetupEndGear(SetupPrefabs setupPrefabs)
            {
                // Couplers
                if (WantsEndGear(End.F))
                {
                    EndGearF.Coupler = UnityEngine.Object.Instantiate(setupPrefabs.CouplerPrefab, EndGearParentF.transform, worldPositionStays: false);
                    EndGearF.Coupler.car = this;
                    EndGearF.Coupler.end = End.F;
                    EndGearF.Coupler.gameObject.hideFlags = HideFlags.DontSave;
                }
                if (WantsEndGear(End.R))
                {
                    EndGearR.Coupler = UnityEngine.Object.Instantiate(setupPrefabs.CouplerPrefab, EndGearParentR.transform, worldPositionStays: false);
                    EndGearR.Coupler.car = this;
                    EndGearR.Coupler.end = End.R;
                    EndGearR.Coupler.gameObject.hideFlags = HideFlags.DontSave;
                }

                // Cut levers
                EndGearF.CutLever = UnityEngine.Object.Instantiate(setupPrefabs.CutLeverPrefab, EndGearParentF.transform, worldPositionStays: false);
                EndGearR.CutLever = UnityEngine.Object.Instantiate(setupPrefabs.CutLeverPrefab, EndGearParentR.transform, worldPositionStays: false);
                EndGearF.CutLever.transform.localPosition = OffsetToEnd(End.F) * Vector3.forward;
                EndGearR.CutLever.transform.localPosition = OffsetToEnd(End.R) * Vector3.forward;
                EndGearR.CutLever.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                EndGearF.CutLever.OnActivate += () => HandleCouplerClick(EndGearF.Coupler);
                EndGearR.CutLever.OnActivate += () => HandleCouplerClick(EndGearR.Coupler);
                EndGearF.CutLever.gameObject.SetActive(value: false);
                EndGearR.CutLever.gameObject.SetActive(value: false);

                // Anglecocks
                EndGearF.Populate(setupPrefabs.AnglecockPrefab, EndGearParentF.transform, airHosePosition);
                EndGearR.Populate(setupPrefabs.AnglecockPrefab, EndGearParentR.transform, airHosePosition);
                EndGearF.Anglecock.transform.localPosition = OffsetToEnd(End.F) * Vector3.forward;
                EndGearR.Anglecock.transform.localPosition = OffsetToEnd(End.R) * Vector3.forward;
                EndGearR.Anglecock.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                EndGearF.Anglecock.Setup(End.F, id);
                EndGearR.Anglecock.Setup(End.R, id);
                EndGearF.Anglecock.control.tooltipText = () => AnglecockTooltipText(EndGearF.Anglecock);
                EndGearR.Anglecock.control.tooltipText = () => AnglecockTooltipText(EndGearR.Anglecock);
                EndGearF.Anglecock.gameObject.SetActive(WantsEndGear(End.F));
                EndGearR.Anglecock.gameObject.SetActive(WantsEndGear(End.R));
                EndGearF.DidPopulate();
                EndGearR.DidPopulate();
            }

            internal void SetupComponent(Model.Definition.Component component, ComponentSetup.Context setupContext, ComponentLifetime lifetime)
            {
                Transform parent = lifetime switch
                {
                    ComponentLifetime.Static => base.transform,
                    ComponentLifetime.Model => Spacer.transform.ResolveTransform(component.Parent, defaultReturnsReceiver: true),
                    _ => throw new ArgumentOutOfRangeException("lifetime", lifetime, null),
                };
                Action<string, Action<Value>> observeProperty = lifetime switch
                {
                    ComponentLifetime.Static => delegate (string key, Action<Value> action)
                    {
                        Observers.Add(KeyValueObject.Observe(key, action));
                    }
                    ,
                    ComponentLifetime.Model => delegate (string key, Action<Value> action)
                    {
                        _controlObservers.Add(KeyValueObject.Observe(key, action));
                    }
                    ,
                    _ => throw new ArgumentOutOfRangeException("lifetime", lifetime, null),
                };
                ComponentSetup.Setup(DefinitionIdentifier, component, setupContext, parent, observeProperty, TrainController.Shared.PrefabInstantiator);
            }
























            public override Location PositionWheelBoundsFront(Location wheelBoundsF, Graph graph, MovementInfo info, bool update)
            {
                // Debug.Log($"{this} called PositionWheelBoundsFront");

                List<ArticulatedPivot> wheelsetPivots = Pivots.Where(p => p.IsWheelset).OrderByDescending(pivot => pivot.Definition.Position).ToList();

                if (MainPivot.IsWheelset) { wheelsetPivots.Add(MainPivot); }

                float halfCarLength = carLength / 2f;

                List<Location> locations = [graph.LocationByMoving(wheelBoundsF, wheelInsetF - halfCarLength + wheelsetPivots.First().Definition.Position)];

                for (int i = 1; i < wheelsetPivots.Count; i++)
                {
                    locations.Add(graph.LocationByMoving(locations[i - 1], wheelsetPivots[i].Definition.Position - wheelsetPivots[i - 1].Definition.Position));
                }

                Location wheelBoundsR = graph.LocationByMoving(locations.Last(), wheelInsetR - halfCarLength - wheelsetPivots.Last().Definition.Position);

                if (!update)
                {
                    // Debug.Log($"{this} shortcutting PositionWheelBoundsFront");
                    return wheelBoundsR;
                }

                bool isFirstPosition = _isFirstPosition;
                _isFirstPosition = false;

                UpdateBaseLocations(wheelBoundsF, wheelBoundsR, graph, isFirstPosition);
                UpdateCurvatureForLocation(locations.Random());

                PositionAccuracy accuracy = (IsVisible ? PositionAccuracy.High : PositionAccuracy.Standard);

                _truckGizmoPosRots = locations.Select(loc => graph.GetPositionRotation(loc, accuracy)).ToList();

                for (int i = 0; i < wheelsetPivots.Count; i++)
                {
                    wheelsetPivots[i].posRot = _truckGizmoPosRots[i];
                }

                foreach (ArticulatedPivot pivot in Pivots)
                {
                    pivot.GetPosRot();
                }

                Vector3 mainPivotForward = MainPivot.GetPosRot().Rotation * Vector3.forward;

                Graph.PositionRotation posRotA = MainPivot.posRot;
                posRotA.Position += mainPivotForward;
                Graph.PositionRotation posRotB = MainPivot.posRot;
                posRotB.Position -= mainPivotForward;

                SetBodyPosition(wheelBoundsF, posRotA, posRotB, 0, isFirstPosition);

                // Debug.Log($"MainPivot in loop at {MainPivot.transform.position}, {MainPivot.transform.rotation}"); // Debug.Log prints correct value but actually positioned at (0, 0, 0)

                float distance = info.DeltaTime * velocity;

                foreach (ArticulatedPivot pivot in Pivots)
                {
                    if (pivot.IsWheelset)
                    {
                        pivot.GetComponentInChildren<Wheelset>()?.Roll(distance, velocity);
                    }
                    else
                    {
                        pivot.posRot = Sway(pivot.posRot);
                    }
                    TransformForDerailment(pivot.posRot, pivot.posRot.Position, wheelBoundsF);
                    pivot.Move(isFirstPosition);
                }

                // Debug.Log($"Car {this} Positions:\n" + string.Join("\n", Pivots.ToList().Select(namedPivot => namedPivot.Key + ": mps=" + ((Vector3)AccessTools.Field(typeof(CarMover), "_velocity").GetValue(namedPivot.Value.mover)).magnitude + ", pos=" + WorldTransformer.GameToWorld(namedPivot.Value.posRot.Position).ToString() + ", rot=" + namedPivot.Value.posRot.Rotation.ToString())));

                if (_rollingPlayer != null)
                {
                    _rollingPlayer.SetVelocity(velocity);
                }

                FireOnMovement(info);
                // Debug.Log($"{this} leaving PositionWheelBoundsFront");
                return wheelBoundsR;

                Graph.PositionRotation Sway(Graph.PositionRotation pr)
                {
                    return new Graph.PositionRotation(pr.Position, pr.Rotation * Quaternion.Euler(0f, 0f, Preferences.CameraSwayIntensity * Config.swayScaleRoll * swayPosition));
                }
            }

            protected override void FixedUpdate()
            {
                base.FixedUpdate();
                Pivots.ForEach(p => p.mover.CheckForSleepyMover());
            }
            protected override void UnloadModels()
            {

                CancelDelayedUnload();
                try
                {
                    _modelLoadPending = false;
                    if (BodyTransform == null)
                    {
                        return;
                    }

                    foreach (IDisposable controlObserver in _controlObservers)
                    {
                        controlObserver.Dispose();
                    }

                    _controlObservers.Clear();
                    RemoveAllOilPointPickables();

                    Wheelset[] trucks = GetComponentsInChildren<Wheelset>();
                    for (int i = trucks.Length - 1; i >= 0; i--)
                    {
                        Wheelset truck = trucks[i];
                        BrakeAnimators.Remove(truck);
                        truck = null;
                    }

                    _truckPrefabLoadTasks.Clear();

                    BrakeAnimator[] componentsInChildren = BodyTransform.GetComponentsInChildren<BrakeAnimator>();
                    foreach (BrakeAnimator item in componentsInChildren)
                    {
                        BrakeAnimators.Remove(item);
                    }
                    int childCount = 0;
                    foreach (ArticulatedPivot pivot in Pivots)
                    {
                        // pivot.mover?.ClearBody();
                        // pivot.transform.DestroyAllChildren();
                        
                        childCount = pivot.transform.childCount;
                        for (int i = childCount - 1; i >= 0; i--)
                        {
                            DestroyImmediate(pivot.transform.GetChild(i).gameObject);
                        }
                    }

                    // MainPivot.mover?.ClearBody();
                    // MainPivot.transform.DestroyAllChildren();
                    // Extention method Helpers.TransformExtensions.DestroyAllChildren exists, unfortunately it is an extension function and calling it too fast fucks things up

                    
                    childCount = MainPivot.transform.childCount;
                    for (int i = childCount - 1; i >= 0; i--)
                    {
                        DestroyImmediate(MainPivot.transform.GetChild(i).gameObject);
                    }

                    UnityEngine.Object.Destroy(Spacer.GetComponent<CarPickable>());
                    UnityEngine.Object.Destroy(Spacer.GetComponent<RollingPlayer>());
                    UnityEngine.Object.Destroy(Spacer.GetComponent<CarColorController>());
                    // UnityEngine.Object.Destroy(BodyTransform.gameObject);

                    Pivots.Add(MainPivot);
                    BodyTransform = null;
                    _audioReparenter.BodyTransform = null;
                    _bodyRenderers = Array.Empty<Renderer>();
                    _truckRenderers.Clear();
                    foreach (Material ownedMaterial in _ownedMaterials)
                    {
                        UnityEngine.Object.DestroyImmediate(ownedMaterial);
                    }

                    _ownedMaterials.Clear();
                    // _mover.ClearBody();
                    EndGearF.Depopulate();
                    EndGearR.Depopulate();
                    _movementListeners.Clear();
                    foreach (Task<LoadedAssetReference<GameObject>> value in _modelLoadTasks.Values)
                    {
                        value.Result?.Dispose();
                    }

                    _modelLoadTasks.Clear();
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Exception in UnloadModels for {car}", this);
                    Debug.LogException(exception);
                }
            }
            internal Vector3 ReparentedCouplerPivot(End end, float extra = 0f)
            {
                return (end == End.F)
                    ? EndGearParentF.transform.TransformPoint(new Vector3(0f, couplerHeight, OffsetToEnd(end, extra)))
                    : EndGearParentR.transform.TransformPoint(new Vector3(0f, couplerHeight, OffsetToEnd(end, extra)));
            }

            internal void PositionCouplerOld(LogicalEnd logicalEnd)
            {
                Coupler coupler = this[logicalEnd].Coupler;
                if (!(coupler == null))
                {
                    float extra = -0.276f;
                    LogicalEnd logical = ((logicalEnd == LogicalEnd.A) ? LogicalEnd.B : LogicalEnd.A);
                    Car car = set?.GetCouplerConnection(this, logicalEnd);
                    Transform parentTransform = (LogicalToEnd(logicalEnd) == End.F) ? EndGearParentF.transform : EndGearParentR.transform;
                    Vector3? obj = ((car != null) ? new Vector3?(CarCouplerPivot(car, car.LogicalToEnd(logical), extra)) : ((Vector3?)null));
                    Vector3 vector = ReparentedCouplerPivot(LogicalToEnd(logicalEnd), extra);
                    Vector3 obj2 = obj ?? ReparentedCouplerPivot(LogicalToEnd(logicalEnd), 1f);
                    Vector3 vector2 = (LogicalToEnd(logicalEnd) == End.F) ? parentTransform.TransformPoint(Vector3.up) : parentTransform.TransformPoint(Vector3.up);
                    Vector3 vector3 = Vector3.ProjectOnPlane(Quaternion.LookRotation(obj2 - vector, vector2) * Vector3.forward, vector2);
                    Quaternion quaternion = Quaternion.Inverse(parentTransform.rotation);
                    coupler.transform.SetLocalPositionAndRotation(quaternion * (vector - parentTransform.position), Quaternion.Euler(0, (quaternion * Unity.Mathematics.quaternion.LookRotation(vector3, vector2)).eulerAngles.y, 0));
                }


            }
            internal void PositionCoupler(LogicalEnd logicalEnd)
            {
                Coupler coupler = this[logicalEnd].Coupler;
                if (!(coupler == null))
                {
                    float extra = -0.276f;
                    LogicalEnd otherLogicalEnd = ((logicalEnd == LogicalEnd.A) ? LogicalEnd.B : LogicalEnd.A);
                    End thisEnd = LogicalToEnd(logicalEnd);
                    Transform parentTransform = (thisEnd == End.F) ? EndGearParentF.transform : EndGearParentR.transform;
                    Car car = set?.GetCouplerConnection(this, logicalEnd);
                    if (car == null)
                    {
                        coupler.transform.SetLocalPositionAndRotation(new Vector3(0f, couplerHeight, OffsetToEnd(thisEnd, extra)), Quaternion.Euler(0f, (thisEnd == End.F) ? 0f : 180f, 0f));
                        return;
                    }
                    Vector3 lookDir = CarCouplerPivot(car, car.LogicalToEnd(otherLogicalEnd), extra) - ReparentedCouplerPivot(thisEnd, extra);

                    coupler.transform.SetLocalPositionAndRotation(new Vector3(0f, couplerHeight, OffsetToEnd(thisEnd, extra)), Quaternion.Inverse(parentTransform.rotation) * Quaternion.LookRotation(lookDir));
                }
            }


















        }
    }
}
//          Changing car identifier -> HandleSetIdent -> ReloadModel -> UnloadModels -┐
//                                                               (See below)          v                                           ┌-> BodyTransform
// AddCarInternal & HandleCreateCarsAsTrain -> PostAdd(Car) -> ModelLoadRetain -> LoadModels -> ASYNC LoadModelsAsync -> HandleModelsLoaded -> DidLoadModels -> (bunch of SetupXXXX funcs) SetupTrucks -> set _truckA and _truckB
//                │                                └-> GetSpherePosition -> GetCenterPosition -> LocationsAssertValid -> PositionWheelBoundsFront
//                └-> CreateCarIfNeeded -┬> CreateCarRaw -> Setup -> FinishSetup (type-specific defintion-to-class loading)
//    PlaceGhost -> CreateGhostIfNeeded -┘                                └-> ModelLoadRetain -> PositionA -> PositionFront -> PositionWheelBoundsFront