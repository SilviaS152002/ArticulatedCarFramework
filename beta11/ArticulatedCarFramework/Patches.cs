using Audio;
using Character;
using Game;
using Game.Messages;
using Game.State;
using HarmonyLib;
using Helpers;
using JsonSubTypes;
using KeyValue.Runtime;
using KinematicCharacterController;
using Model;
using Model.Definition;
using Model.Definition.Data;
using NS15.ArticulatedCarFramework;
using RollingStock;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Track;
using UI;
using UI.CarEditor;
using UI.Common;
using UI.Tags;
using UnityEngine;
using static Model.Car;
using static Track.TrackSegment;

namespace NS15
{
    namespace ArticulatedCarFramework
    {
        [HarmonyPatch(typeof(Car))]
        public static class CarPatches
        {
            [HarmonyPatch("LoadModelsAsync"), HarmonyPrefix]
            public static bool LoadModelsAsyncPrefix(Car __instance)
            {
                // Debug.Log($"{__instance.GetType()} {__instance} called Car.LoadModelsAsync");
                if (__instance is ArticulatedCar)
                {
                    ((ArticulatedCar)__instance).LoadModelsAsync();
                    return false;
                }
                return true;
            }

            [HarmonyPatch("WorldDidMove"), HarmonyPostfix]
            public static void WorldDidMovePostfix(Car __instance, Vector3 offset)
            {
                if (__instance is ArticulatedCar)
                {
                    ((ArticulatedCar)__instance).Pivots.ForEach(p => p.mover.WorldDidMove(offset));
                }
            }

            [HarmonyPatch("SetPlayerNearby"), HarmonyPostfix]
            public static void SetPlayerNearbyPostfix(bool nearby, Car __instance)
            {
                if (__instance is ArticulatedCar)
                {
                    nearby |= !Preferences.EnableCarUpdateOptimization;
                    ((ArticulatedCar)__instance).Pivots.ForEach(p => p.mover.SetPlayerNearby(nearby));
                }
            }


            [HarmonyPatch("SetupComponent"), HarmonyPrefix]
            public static bool SetupComponentPrefix(Car __instance, Model.Definition.Component component, ComponentSetup.Context setupContext, ComponentLifetime lifetime)
            {
                if (__instance is ArticulatedCar)
                {
                    ((ArticulatedCar)__instance).SetupComponent(component, setupContext, lifetime);
                    return false;
                }
                return true;
            }

            [HarmonyPatch("PositionCoupler"), HarmonyPrefix]
            public static bool PositionCouplerPrefix(Car __instance, Car.LogicalEnd logicalEnd)
            {
                if (__instance is ArticulatedCar)
                {
                    ((ArticulatedCar)__instance).PositionCoupler(logicalEnd);
                    return false;
                }
                return true;
            }

            [HarmonyPatch("CouplerPivot"), HarmonyPrefix]
            public static bool CouplerPivotPrefix(Car __instance, Car.End end, float extra, ref Vector3 __result)
            {
                if (__instance is ArticulatedCar)
                {
                    __result = ((ArticulatedCar)__instance).ReparentedCouplerPivot(end, extra);
                    return false;
                }
                return true;
            }


            [HarmonyPatch("GetMoverTargetPositionRotation"), HarmonyPrefix]
            public static bool GetMoverTargetPositionRotation(Car __instance, ref (Vector3 position, Quaternion rotation) __result)
            {
                if (__instance is ArticulatedCar)
                {
                    __result = ((ArticulatedCar)__instance).GetMoverTargetPositionRotation();
                    return false;
                }
                return true;

            }


            /*
            [HarmonyPatch("TransformForDerailment"), HarmonyPrefix]
            public static void TransformForDerailmentPrefix(Graph.PositionRotation pr, Vector3 center0, Location location, Car __instance)
			{
                Debug.Log($"{__instance.GetType()} {__instance} called Car.TransformForDerailment with Graph.PositionRotation {pr}, Vector3 {center0}, Location {location}");
            }
            
			[HarmonyPatch("PositionA"), HarmonyPrefix]
			public static void PositionAPrefix(Car __instance, Location a, Graph graph, MovementInfo movementInfo, bool update)
			{
				Debug.Log($"{__instance.GetType()} {__instance} called Car.PositionA with Location {a}, Graph {graph}, MovementInfo {movementInfo}, bool {update}");
			}

			[HarmonyPatch("PositionFront"), HarmonyPrefix]
			public static void PositionFrontPrefix(Car __instance, Location front, Graph graph, MovementInfo info, bool update)
			{
				Debug.Log($"{__instance.GetType()} {__instance} called Car.PositionFront with Location {front}, Graph {graph}, MovementInfo {info}, bool {update}");
			}
			
			[HarmonyPatch("PositionWheelBoundsFront"), HarmonyPrefix]
			public static void PositionWheelBoundsFrontPrefix(Car __instance, Location wheelBoundsF, Graph graph, MovementInfo info, bool update)
			{
				Debug.Log($"{__instance.GetType()} {__instance} called Car.PositionWheelBoundsFront with Location {wheelBoundsF}, Graph {graph}, MovementInfo {info}, bool {update}");
			}

			[HarmonyPatch("Setup"), HarmonyPrefix]
			public static void SetupPrefix(Car __instance)
			{
				Debug.Log($"Entering Setup with {__instance.GetType()} {__instance}");
			}

			[HarmonyPatch("Setup"), HarmonyPostfix]
			public static void SetupPostfix(Car __instance)
			{
				Debug.Log($"Setup returning {__instance.GetType()} {__instance}");
			}

			[HarmonyPatch("ValidateDefinition"), HarmonyPrefix]
			public static void ValidateDefinitionPrefix(Car __instance)
			{
				Debug.Log($"Entering ValidateDefinition with {__instance.GetType()} {__instance}");
			}

			[HarmonyPatch("ValidateDefinition"), HarmonyPostfix]
			public static void ValidateDefinitionPostfix(Car __instance)
			{
				Debug.Log($"ValidateDefinition returning with {__instance.GetType()} {__instance}");
			}

			[HarmonyPatch("SetupComponents"), HarmonyPrefix]
			public static void SetupComponentsPrefix(Car __instance, ComponentSetup.Context ctx, ComponentLifetime lifetime)
			{
				Debug.Log($"Entering SetupComponents with {__instance.GetType()} {__instance}");
			}

			[HarmonyPatch("SetupComponents"), HarmonyPostfix]
			public static void SetupComponentsPostfix(Car __instance, ComponentSetup.Context ctx, ComponentLifetime lifetime)
			{
				Debug.Log($"Leaving SetupComponents with {__instance.GetType()} {__instance}");
			}

			[HarmonyPatch("PostSetupComponents"), HarmonyPrefix]
			public static void PostSetupComponentsPrefix(Car __instance, ComponentLifetime lifetime)
			{
				Debug.Log($"Entering PostSetupComponents with {__instance.GetType()} {__instance}");
			}

			[HarmonyPatch("PostSetupComponents"), HarmonyPostfix]
			public static void PostSetupComponentsPostfix(Car __instance, ComponentLifetime lifetime)
			{
				Debug.Log($"Leaving PostSetupComponents with {__instance.GetType()} {__instance}");
			}

			[HarmonyPatch("UnloadModels"), HarmonyPrefix]
			public static void UnloadModelsPrefix(Car __instance)
			{
				string test = string.Join("\nby ", new System.Diagnostics.StackTrace().GetFrames().ToList().Select(frame => frame.GetMethod().Name).ToArray());

				Debug.Log($"Entering UnloadModels with {__instance.GetType()} {__instance} Called by: {test}");
			}

			[HarmonyPatch("UnloadModels"), HarmonyPostfix]
			public static void UnloadModelsPostfix(Car __instance)
			{
				Debug.Log($"Leaving UnloadModels with {__instance.GetType()} {__instance}");
			}*/

        }

        [HarmonyPatch(typeof(JsonSubtypes))]
        public static class JsonSubtypesPatches
        {

            [HarmonyPatch("GetSubTypeMapping"), HarmonyPostfix]
            public static void GetSubTypeMappingPostfix(Type type, JsonSubtypes __instance, ref NullableDictionary<object, Type> __result)
            {
                if (!__result.TryGetValue("ArticulatedCar", out _))
                {
                    __result.Add("ArticulatedCar", typeof(ArticulatedCarDefinition));
                }

            }

        }
        /*
        [HarmonyPatch("DefinitionChecker", "CheckCar"), HarmonyDebug]
        public static class DefinitionCheckerPatches_CheckCar
        {
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> CheckCarTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> codes = instructions.ToList();
                MethodInfo DefCheckerWarn = AccessTools.Method(AccessTools.TypeByName("Model.Database.DefinitionChecker"), "Warning");

                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldarg_0 &&
                        codes[i + 1].opcode == OpCodes.Ldstr && codes[i + 1].operand is string str && str == "ModelIdentifier '")
                    {
                        yield return codes[i];
                        yield return codes[i + 1];
                        yield return codes[i + 2];
                        yield return codes[i + 3];
                        yield return codes[i + 4];
                        yield return codes[i + 5];
                        yield return new CodeInstruction(OpCodes.Call, DefCheckerWarn);
                        i += 6;
                        Debug.Log($"CheckCarTranspiler patched");
                        continue;
                    }
                    yield return codes[i];
                }

            }
        }*/

        [HarmonyPatch(typeof(TrainController))]
        public static class TrainControllerPatches
        {
            [HarmonyPatch("CreateCarRaw"), HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> CreateCarRawTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> codes = instructions.ToList();

                MethodInfo archetypeGetter = AccessTools.Property(typeof(CarDefinition), "Archetype").GetGetMethod();
                MethodInfo kindGetter = AccessTools.Property(typeof(CarDefinition), "Kind").GetGetMethod();
                MethodInfo addCarKind = AccessTools.Method(typeof(TrainControllerPatches), nameof(AddCarKind));
                var kindLocal = generator.DeclareLocal(typeof(string));

                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand is MethodInfo methodInfo && methodInfo == archetypeGetter)
                    {
                        yield return new CodeInstruction(OpCodes.Callvirt, kindGetter);
                        yield return new CodeInstruction(OpCodes.Stloc_S, kindLocal.LocalIndex);
                        yield return new CodeInstruction(OpCodes.Ldloc_2);
                        yield return new CodeInstruction(OpCodes.Ldloc_S, kindLocal.LocalIndex);
                        yield return new CodeInstruction(OpCodes.Call, addCarKind);
                        yield return new CodeInstruction(OpCodes.Stloc_S, 5);
                        i += 18;
                        continue;
                    }
                    yield return codes[i];
                }

            }

            public static Car AddCarKind(GameObject gameObject, string definitionKind)
            {
                Type type = Main.CarJsonSubTypes[definitionKind] ?? throw new ArgumentException("Kind \"" + definitionKind + "\" does not match.");
                Debug.Log($"Trying to get car typed {type} with definitionKind {definitionKind}");
                Car addedCar = (Car)gameObject.AddComponent(type);
                // Debug.Log($"Internal function AddCarKind returning {addedCar.GetType()} {addedCar}");
                return addedCar;
            }

            /*
            [HarmonyPatch("CreateCarRaw"), HarmonyPostfix]
            public static void CreateCarRawPostfix(ref Car __result)
            {
                Debug.Log($"CreateCarRaw returning {__result.GetType()} {__result}");
            }*/
        }


        [HarmonyPatch(typeof(CarMover))]
        public static class CarMoverPatches
        {
            [HarmonyPatch("ConfigureForBody"), HarmonyPrefix]
            public static void ConfigureForBodyPrefix(CarMover __instance)
            {
                if ((Transform)AccessTools.Field(typeof(CarMover), "_bodyTransform").GetValue(__instance) != null)
                {
                    Debug.Log($"{__instance.DebugId} is already configured:\n" + string.Join("\nby ", new System.Diagnostics.StackTrace().GetFrames().ToList().Select(frame => frame.GetMethod().Name)));
                    // throw new ArgumentNullException();
                }
            }

        }

        /*
        [HarmonyPatch(typeof(CarMover))]
        public static class CarMoverPatches
        {
            [HarmonyPatch("CheckForSleepyMover"), HarmonyPrefix]
            public static bool CheckForSleepyMoverPrefix(CarMover __instance)
            {
                if (__instance is BatchCarMover)
                {
                    ((BatchCarMover)__instance).CheckForSleepyMover();
                    return false;
                }
                return true;
            }
            [HarmonyPatch("ClearBody"), HarmonyPrefix]
            public static bool ClearBodyPrefix(CarMover __instance)
            {
                if (__instance is BatchCarMover)
                {
                    ((BatchCarMover)__instance).ClearBody();
                    return false;
                }
                return true;
            }
            [HarmonyPatch("ConfigureForBody"), HarmonyPrefix]
            public static bool ConfigureForBodyPrefix(GameObject body, CarMover __instance)
            {
                if (__instance is BatchCarMover)
                {
                    ((BatchCarMover)__instance).ConfigureForMainBody(body);
                    return false;
                }
                return true;
            }
            [HarmonyPatch("Move"), HarmonyPrefix]
            public static bool MovePrefix(Vector3 worldPosition, Quaternion rotation, bool immediate, CarMover __instance)
            {
                if (__instance is BatchCarMover)
                {
                    ((BatchCarMover)__instance).Move(worldPosition, rotation, immediate);
                    return false;
                }
                return true;
            }
            [HarmonyPatch("SetPlayerNearby"), HarmonyPrefix]
            public static bool SetPlayerNearbyPrefix(bool playerNearby, CarMover __instance)
            {
                if (__instance is BatchCarMover)
                {
                    ((BatchCarMover)__instance).SetPlayerNearby(playerNearby);
                    return false;
                }
                return true;
            }
            [HarmonyPatch("WorldDidMove"), HarmonyPrefix]
            public static bool WorldDidMovePrefix(Vector3 offset, CarMover __instance)
            {
                if (__instance is BatchCarMover)
                {
                    ((BatchCarMover)__instance).CheckForSleepyMover();
                    return false;
                }
                return true;
            }
        }*/


        /*
        [HarmonyPatch(typeof(KinematicCharacterMotor))]
        public static class KinematicCharacterMotorPatches
        {
            [HarmonyPatch("GetVelocityFromRigidbodyMovement"), HarmonyPostfix]
            public static void GetVelocityFromRigidbodyMovementPostfix(Rigidbody interactiveRigidbody, Vector3 atPoint, float deltaTime, ref Vector3 linearVelocity, ref Vector3 angularVelocity)
            {
                Debug.Log($"returning velocity {linearVelocity} and {angularVelocity} from {interactiveRigidbody.name} at a time of {deltaTime}");
            }


        }*/

        // _attachedCar

        
        [HarmonyPatch(typeof(DefinitionEditorModeController))]
        public static class DefinitionEditorModeControllerPatches
        {
            [HarmonyPatch("GetParentPositionRotation", [typeof(string), typeof(TransformReference)]), HarmonyPrefix]
            public static bool GetParentPositionRotation(string carId, TransformReference transformReference, DefinitionEditorModeController __instance, ref (Vector3, Quaternion) __result)
            {
                if (!TrainController.Shared.TryGetCarForId(carId, out var car))
                {
                    Debug.LogWarning("Couldn't find car " + carId);
                    __result = (Vector3.zero, Quaternion.identity);
                    return false;
                }
                
                Transform bodyTransform = car is ArticulatedCar ? ((ArticulatedCar)car).Spacer.transform : car.BodyTransform;

                if (bodyTransform == null)
                {
                    Car.MotionSnapshot motionSnapshot = car.GetMotionSnapshot();
                    Debug.LogWarning($"Can't get transform reference position rotation - BodyTransform is null. Snapshot: {motionSnapshot.Position}, {motionSnapshot.Rotation}");
                    __result = (motionSnapshot.Position, motionSnapshot.Rotation);
                    return false;
                }
                __result = ((Vector3, Quaternion))AccessTools.Method(typeof(DefinitionEditorModeController), "GetParentPositionRotation", [typeof(Transform), typeof(TransformReference)]).Invoke(null, [bodyTransform, transformReference]);
                
                return false;
            }

            /*
            [HarmonyPatch("GetParentPositionRotation", [typeof(Transform), typeof(TransformReference)]), HarmonyPostfix]
            public static void GetParentPositionRotationPostfix(DefinitionEditorModeController __instance, (Vector3, Quaternion) __result, Transform bodyTransform, TransformReference transformReference)
            {
                Debug.Log($"Returning {__result} from GetParentPositionRotation from Transform {bodyTransform.name}, TransformReference {string.Join("-", transformReference.Path)} with offset {WorldTransformer.GameToWorld(Vector3.zero)}");
            }*/


        }

        /*
        [HarmonyPatch(typeof(CarColorController))]
        public static class CarColorControllerPatches
        {
            [HarmonyPatch("OnEnable"), HarmonyPostfix]
            public static void OnEnablePostfix(CarColorController __instance)
            {
                Debug.Log($"CarColorController {__instance.gameObject.name} enabled");
            }

            [HarmonyPatch("ColorFromPalette", []), HarmonyPrefix]
            public static void ColorFromPalettePrefix(CarColorController __instance)
            {
                Debug.Log($"CarColorController {__instance.gameObject.name} getting color");
            }

            [HarmonyPatch("ColorFromPalette", []), HarmonyPostfix]
            public static void ColorFromPalettePostfix(CarColorController __instance, string __result)
            {
                Debug.Log($"CarColorController {__instance.gameObject.name} got color {__result}");
            }
            [HarmonyPatch("UpdateForColorScheme"), HarmonyPostfix]
            public static void UpdateForColorSchemePostfix(CarColorController __instance, Value value)
            {
                Debug.Log($"CarColorController {__instance.gameObject.name} updating color scheme {value}");
            }
            [HarmonyPatch("OnColorSchemeChanged"), HarmonyPostfix]
            public static void OnColorSchemeChangedPostfix(CarColorController __instance, CarColorScheme scheme)
            {
                Debug.Log($"CarColorController {__instance.gameObject.name} updating OnColorSchemeChanged {scheme}");
            }
        }*/

        [HarmonyPatch(typeof(GameInput))]
        public static class GameInputPatches
        {
            [HarmonyPatch("PushCar"), HarmonyPrefix]
            public static bool PushCarPrefix()
            {
                if (!Main.settings.AltCamera) { return true; }
                ObjectPicker objectPicker = ObjectPicker.Shared;
                if (objectPicker == null)
                {
                    Log.Warning("Missing ObjectPicker.");
                    return false;
                }
                if (!(objectPicker.HoveringOver is CarPickable carPickable))
                {
                    Log.Warning("Couldn't find car to push; hovering over: {hoveringOver}", objectPicker.HoveringOver);
                    return false;
                }
                CameraSelector cameraSelector = CameraSelector.shared;
                if (!cameraSelector.CurrentCameraIsFirstPerson)
                {
                    Toast.Present("Only available in first person.");
                    return false;
                }
                PlayerController character = cameraSelector.character;
                if (!character.IsOnGround)
                {
                    Debug.Log($"Can't push from here: IsStableOnGround = {character.character.motor.GroundingStatus.IsStableOnGround}, AttachedRigidbody = {character.character.motor.AttachedRigidbody}");
                    Toast.Present("Must be on ground.");
                    return false;
                }
                if (objectPicker.DistanceToTarget > 5f)
                {
                    Toast.Present("Too far away.");
                    return false;
                }
                Car car = carPickable.car;
                if (car.IsDerailed)
                {
                    Log.Debug("Push {car} (rerail)", car);
                    StateManager.ApplyLocal(new Rerail(new string[1] { car.id }, 0.26f));
                    return false;
                }
                Log.Debug("Push {car}", car);
                TrainController trainController = TrainController.Shared;
                Vector3 lookDir = Camera.main.transform.rotation * Vector3.forward;
                Vector3 carDir = trainController.graph.GetPosition(car.WheelBoundsF) - trainController.graph.GetPosition(car.WheelBoundsR);
                float num2 = Vector3.Dot(lookDir, carDir);
                int direction = (num2 > 0) ? -1 : 1;
                StateManager.ApplyLocal(new ManualMoveCar(car.id, direction));
                return false;
            }

        }
    }
    
}

