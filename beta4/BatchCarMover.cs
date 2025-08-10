using HarmonyLib;
using KinematicCharacterController;
using Model;
using System.Collections.Generic;
using System.Linq;
using Track;
using UI.Console.Commands;
using UnityEngine;

namespace NS15
{
    namespace ArticulatedCarFramework
    {
        public partial class BatchCarMover : CarMover
        {
            private float _timeLastMoved // Harmony
            {
                get => (float)AccessTools.Field(typeof(CarMover), "_timeLastMoved").GetValue(this);
                set => AccessTools.Field(typeof(CarMover), "_timeLastMoved").SetValue(this, value);
            }
            private bool _physicsMoverEnabled // Harmony
            {
                get => (bool)AccessTools.Field(typeof(CarMover), "_physicsMoverEnabled").GetValue(this);
                set => AccessTools.Field(typeof(CarMover), "_physicsMoverEnabled").SetValue(this, value);
            }
            private bool _movedRecently // Harmony
            {
                get => (bool)AccessTools.Field(typeof(CarMover), "_movedRecently").GetValue(this);
                set => AccessTools.Field(typeof(CarMover), "_movedRecently").SetValue(this, value);
            }
            private bool _playerNearby // Harmony
            {
                get => (bool)AccessTools.Field(typeof(CarMover), "_playerNearby").GetValue(this);
                set => AccessTools.Field(typeof(CarMover), "_playerNearby").SetValue(this, value);
            }
            private Transform _bodyTransform // Harmony
            {
                get => (Transform)AccessTools.Field(typeof(CarMover), "_bodyTransform").GetValue(this);
                set => AccessTools.Field(typeof(CarMover), "_bodyTransform").SetValue(this, value);
            }
            private PhysicsMover _physicsMover // Harmony
            {
                get => (PhysicsMover)AccessTools.Field(typeof(CarMover), "_physicsMover").GetValue(this);
                set => AccessTools.Field(typeof(CarMover), "_physicsMover").SetValue(this, value);
            }
            private Rigidbody _rigidbody // Harmony
            {
                get => (Rigidbody)AccessTools.Field(typeof(CarMover), "_rigidbody").GetValue(this);
                set => AccessTools.Field(typeof(CarMover), "_rigidbody").SetValue(this, value);
            }
            private Vector3 _moverPosition // Harmony
            {
                get => (Vector3)AccessTools.Field(typeof(CarMover), "_moverPosition").GetValue(this);
                set => AccessTools.Field(typeof(CarMover), "_moverPosition").SetValue(this, value);
            }
            private Vector3 _velocity // Harmony
            {
                get => (Vector3)AccessTools.Field(typeof(CarMover), "_velocity").GetValue(this);
                set => AccessTools.Field(typeof(CarMover), "_velocity").SetValue(this, value);
            }
            private Quaternion _moverRotation // Harmony
            {
                get => (Quaternion)AccessTools.Field(typeof(CarMover), "_moverRotation").GetValue(this);
                set => AccessTools.Field(typeof(CarMover), "_moverRotation").SetValue(this, value);
            }
        }

        public partial class BatchCarMover : CarMover
		{
			internal float speed;

            internal float deltaTime;

            public SubMoverData GetMainMoverData() => new SubMoverData()
            {
                moverPosRot = new Graph.PositionRotation() { Rotation = _moverRotation },
                oldPosition = _moverPosition,
                velocity = _velocity,
                rigidbody = _rigidbody,
                physicsMover = _physicsMover,
                bodyTransform = _bodyTransform
            };



            public struct SubMoverData
			{
				public Graph.PositionRotation moverPosRot;
                public Vector3 oldPosition;
                public Vector3 velocity;
                public Rigidbody rigidbody;
				public PhysicsMover physicsMover;
				public Transform bodyTransform;
			}

            Dictionary<ArticulatedPivot, SubMoverData> moverData = new Dictionary<ArticulatedPivot, SubMoverData>();


            private void ApplyMoverPosition(SubMoverData data, bool immediate)
            {
                if (immediate)
                {
                    if (_physicsMoverEnabled)
                    {
                        data.physicsMover.SetPositionAndRotation(data.oldPosition, data.moverPosRot.Rotation);
                    }
                    else
                    {
                        data.bodyTransform?.SetPositionAndRotation(data.oldPosition, data.moverPosRot.Rotation);
                    }
                }
                else if (!_physicsMoverEnabled)
                {
                    if (data.rigidbody != null)
                    {
                        data.rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                        data.rigidbody.Move(data.velocity * deltaTime, data.moverPosRot.Rotation);
                    }
                    else
                    {
                        data.bodyTransform?.SetPositionAndRotation(data.oldPosition, data.moverPosRot.Rotation);
                    }
                }
            }

            public new void CheckForSleepyMover()
            {
                if (Time.time - _timeLastMoved > 1f && _physicsMoverEnabled)
                {
                    _movedRecently = false;
                    UpdatePhysicsMoverEnabled();
                }
            }
            private void CheckToAwakenMover(float distanceMoved)
            {
                if (!(distanceMoved < 0.001f))
                {
                    _timeLastMoved = Time.time;
                    _movedRecently = true;
                    UpdatePhysicsMoverEnabled();
                }
            }
            public new void ClearBody()
            {
                ClearBodyFromData(GetMainMoverData());
                moverData.Values.ToList().ForEach(ClearBodyFromData);
                _physicsMoverEnabled = false;
            }
            public void ClearBodyFromData(SubMoverData data)
            {
                data.bodyTransform = null;
                if (data.physicsMover != null)
                {
                    data.physicsMover.MoverController = null;
                    Object.Destroy(data.physicsMover);
                    data.physicsMover = null;
                }
                if (data.rigidbody != null)
                {
                    Object.Destroy(data.rigidbody);
                    data.rigidbody = null;
                }
            }
            public void ConfigureForMainBody(GameObject body)
            {
                _bodyTransform = body.transform;
                _rigidbody = body.AddComponent<Rigidbody>();
                _physicsMover = body.AddComponent<PhysicsMover>();
                _physicsMover.ForceAwake();
                _physicsMoverEnabled = true;
                _physicsMover.MoverController = this;
                _timeLastMoved = Time.time;
                UpdatePhysicsMoverEnabled();
                ApplyMoverPosition(GetMainMoverData(), immediate: true);
            }
            public void ConfigureForSubBody(GameObject body, ArticulatedPivot pivot)
            {
                SubMoverData data = new SubMoverData()
                {
                    rigidbody = body.AddComponent<Rigidbody>(),
                    physicsMover = body.AddComponent<PhysicsMover>(),
                    bodyTransform = body.transform
                };
                data.physicsMover.ForceAwake();
                data.physicsMover.MoverController = this;
                moverData[pivot] = data;
                ApplyMoverPosition(data, immediate: true);
            }
            // No need to change GetMotionSnapshot()
            public new void Move(Vector3 worldPosition, Quaternion rotation, bool immediate)
            {
                CheckToAwakenMover(Vector3.Distance(worldPosition, (_physicsMover == null) ? _moverPosition : _physicsMover.TransientPosition));
                if (immediate)
                {
                    _velocity = Vector3.zero;
                    speed = 0f;
                }
                else
                {
                    deltaTime = Time.fixedDeltaTime;
                    _velocity = (worldPosition - _moverPosition) / deltaTime;
                }
                _moverPosition = worldPosition;
                _moverRotation = rotation;
                ApplyMoverPosition(GetMainMoverData(), immediate);
                foreach (SubMoverData data in moverData.Values)
                {
                    ApplyMoverPosition(data, immediate);
                }
            }
            // SetPhysicsMoverEnabled integrated into UpdatePhysicsMoverEnabled()
            private void SetPhysicsMoverPositionSeamless(SubMoverData data)
            {
                Vector3 initialTickPosition = data.moverPosRot.Position - (data.velocity * deltaTime);
                data.physicsMover.InitialTickPosition = initialTickPosition;
                data.physicsMover.SetPosition(data.moverPosRot.Position);
                data.physicsMover.VelocityUpdate(Time.fixedDeltaTime);
                data.physicsMover.SetPosition(data.moverPosRot.Position);
            }
            public new void SetPlayerNearby(bool playerNearby)
            {
                _playerNearby = playerNearby;
                UpdatePhysicsMoverEnabled();
            }
            // No need to change UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
            private void UpdatePhysicsMoverEnabled()
            {
                bool physicsMoverEnable = _movedRecently && _playerNearby;
                if (_physicsMoverEnabled == physicsMoverEnable) { return; }

                foreach (SubMoverData data in moverData.Values)
                {
                    if (data.physicsMover == null) { continue; }

                    if (physicsMoverEnable)
                    {
                        data.physicsMover.enabled = true;
                        data.physicsMover.Rigidbody = data.rigidbody;
                        data.physicsMover.InitialTickRotation = data.moverPosRot.Rotation;
                        data.physicsMover.SetRotation(data.moverPosRot.Rotation);
                        SetPhysicsMoverPositionSeamless(data);
                    }
                    else
                    {
                        data.physicsMover.enabled = false;
                        if (data.rigidbody != null)
                        {
                            data.rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                            data.rigidbody.MovePosition(data.oldPosition - data.velocity * deltaTime);
                            data.rigidbody.MoveRotation(data.moverPosRot.Rotation);
                        }
                    }

                }
                _physicsMoverEnabled = physicsMoverEnable;
            }
            public new void WorldDidMove(Vector3 offset)
            {
                if (_physicsMoverEnabled)
                {
                    _physicsMover.OffsetSeamless(offset);
                    foreach (SubMoverData data in moverData.Values)
                    {
                        data.physicsMover.OffsetSeamless(offset);
                    }
                }
                else if (_bodyTransform != null)
                {
                    _moverPosition += offset;
                    _bodyTransform.position = _moverPosition;
                    moverData.Values.ToList().ForEach(data =>
                    {
                        data.oldPosition += offset;
                        data.bodyTransform.position = data.oldPosition;
                    });

                }
            }



        }
    }
}
