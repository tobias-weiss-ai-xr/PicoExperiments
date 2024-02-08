using System;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;
using UnityEngine.XR;
using Normal.Utility;
// using ViveSR.anipal.Lip;

namespace Custom.Normal
{
    [ExecutionOrder(-95)]
    public class RealtimeCustomAvatar : RealtimeComponent<RealtimeCustomAvatarModel>
    {
        // Local Player
        [Serializable]
        public class LocalPlayer
        {
            public Transform root;
            public Transform pelvis;
            public Transform spine;
            public Transform leftThigh;
            public Transform leftCalf;
            public Transform leftFoot;
            public Transform rightThigh;
            public Transform rightCalf;
            public Transform rightFoot;
            public Transform spine1;
            public Transform spine2;
            public Transform neck;
            public Transform head;
            public Transform leftUpperArm;
            public Transform leftForeArm;
            public Transform leftHand;
            public Transform rightUpperArm;
            public Transform rightForeArm;
            public Transform rightHand;
        }
        public LocalPlayer localPlayer { get { return _localPlayer; } set { SetLocalPlayer(value); } }
#pragma warning disable 0649 // Disable variable is never assigned to warning.
        private LocalPlayer _localPlayer;
#pragma warning restore 0649

        // Device Type
        public enum DeviceType : uint
        {
            Unknown = 0,
            OpenVR = 1,
            Oculus = 2,
        }

        /// <summary>
        /// The XR device type of the client that owns this avatar. See RealtimeAvatar#DeviceType for values.
        /// </summary>
        public DeviceType deviceType
        {
            get => model.deviceType;
            set => model.deviceType = value;
        }

        /// <summary>
        /// The XRDevice.model of the client that owns this avatar.
        /// </summary>
        public string deviceModel
        {
            get => model.deviceModel;
            set => model.deviceModel = value;
        }

        // Prefab
        public Transform pelvisActive { get { return _pelvis; } }
        public Transform spineActive { get { return _spine; } }
        public Transform leftThighActive { get { return _leftThigh; } }
        public Transform leftCalfActive { get { return _leftCalf; } }
        public Transform leftFootActive { get { return _leftFoot; } }
        public Transform rightThighActive { get { return _rightThigh; } }
        public Transform rightCalfActive { get { return _rightCalf; } }
        public Transform rightFootActive { get { return _rightFoot; } }
        public Transform spine1Active { get { return _spine1; } }
        public Transform spine2Active { get { return _spine2; } }
        public Transform neckActive { get { return _neck; } }
        public Transform headActive { get { return _head; } }
        public Transform leftUpperArmActive { get { return _leftUpperArm; } }
        public Transform leftForeArmActive { get { return _leftForeArm; } }
        public Transform leftHandActive { get { return _leftHand; } }
        public Transform rightUpperArmActive { get { return _rightUpperArm; } }
        public Transform rightForeArmActive { get { return _rightForeArm; } }
        public Transform rightHandActive { get { return _rightHand; } }

#pragma warning disable 0649 // Disable variable is never assigned to warning.
        [SerializeField] private Transform _pelvis;
        [SerializeField] private Transform _spine;
        [SerializeField] private Transform _leftThigh;
        [SerializeField] private Transform _leftCalf;
        [SerializeField] private Transform _leftFoot;
        [SerializeField] private Transform _rightThigh;
        [SerializeField] private Transform _rightCalf;
        [SerializeField] private Transform _rightFoot;
        [SerializeField] private Transform _spine1;
        [SerializeField] private Transform _spine2;
        [SerializeField] private Transform _neck;
        [SerializeField] private Transform _head;
        [SerializeField] private Transform _leftUpperArm;
        [SerializeField] private Transform _leftForeArm;
        [SerializeField] private Transform _leftHand;
        [SerializeField] private Transform _rightUpperArm;
        [SerializeField] private Transform _rightForeArm;
        [SerializeField] private Transform _rightHand;
#pragma warning restore 0649

        private CustomRealtimeAvatarManager _realtimeAvatarManager;

        private static List<XRNodeState> _nodeStates = new List<XRNodeState>();

        private Savefile _savefile;
        void Awake()
        {
            _savefile = GameObject.Find("SavefileManager").GetComponent<Savefile>();
        }

        void Start()
        {


            // Register with RealtimeAvatarManager
            try
            {
                _realtimeAvatarManager = realtime.GetComponent<CustomRealtimeAvatarManager>();
                _realtimeAvatarManager._RegisterAvatar(realtimeView.ownerIDSelf, this);

            }
            catch
            {
                Debug.LogError("RealtimeAvatar failed to register with RealtimeAvatarManager component. Was this avatar prefab instantiated by RealtimeAvatarManager?");
            }
        }

        void OnDestroy()
        {
            // Unregister with RealtimeAvatarManager
            if (_realtimeAvatarManager != null)
                _realtimeAvatarManager._UnregisterAvatar(this);

            // Unregister for events
            localPlayer = null;
        }

        void FixedUpdate()
        {
            UpdateAvatarTransformsForLocalPlayer();
        }

        void Update()
        {
            UpdateAvatarTransformsForLocalPlayer();
        }

        void LateUpdate()
        {
            UpdateAvatarTransformsForLocalPlayer();
        }

        protected override void OnRealtimeModelReplaced(RealtimeCustomAvatarModel previousModel, RealtimeCustomAvatarModel currentModel)
        {
            if (previousModel != null)
            {
                previousModel.pelvisActiveDidChange -= ActiveStateChanged;
                previousModel.spineActiveDidChange -= ActiveStateChanged;
                previousModel.leftThighActiveDidChange -= ActiveStateChanged;
                previousModel.leftCalfActiveDidChange -= ActiveStateChanged;
                previousModel.leftFootActiveDidChange -= ActiveStateChanged;
                previousModel.rightThighActiveDidChange -= ActiveStateChanged;
                previousModel.rightCalfActiveDidChange -= ActiveStateChanged;
                previousModel.rightFootActiveDidChange -= ActiveStateChanged;
                previousModel.spine1ActiveDidChange -= ActiveStateChanged;
                previousModel.spine2ActiveDidChange -= ActiveStateChanged;
                previousModel.neckActiveDidChange -= ActiveStateChanged;
                previousModel.headActiveDidChange -= ActiveStateChanged;
                previousModel.leftUpperArmActiveDidChange -= ActiveStateChanged;
                previousModel.leftForeArmActiveDidChange -= ActiveStateChanged;
                previousModel.leftHandActiveDidChange -= ActiveStateChanged;
                previousModel.rightUpperArmActiveDidChange -= ActiveStateChanged;
                previousModel.rightForeArmActiveDidChange -= ActiveStateChanged;
                previousModel.rightHandActiveDidChange -= ActiveStateChanged;
            }

            if (currentModel != null)
            {
                currentModel.pelvisActiveDidChange += ActiveStateChanged;
                currentModel.spineActiveDidChange += ActiveStateChanged;
                currentModel.leftThighActiveDidChange += ActiveStateChanged;
                currentModel.leftCalfActiveDidChange += ActiveStateChanged;
                currentModel.leftFootActiveDidChange += ActiveStateChanged;
                currentModel.rightThighActiveDidChange += ActiveStateChanged;
                currentModel.rightCalfActiveDidChange += ActiveStateChanged;
                currentModel.rightFootActiveDidChange += ActiveStateChanged;
                currentModel.spine1ActiveDidChange += ActiveStateChanged;
                currentModel.spine2ActiveDidChange += ActiveStateChanged;
                currentModel.neckActiveDidChange += ActiveStateChanged;
                currentModel.headActiveDidChange += ActiveStateChanged;
                currentModel.leftUpperArmActiveDidChange += ActiveStateChanged;
                currentModel.leftForeArmActiveDidChange += ActiveStateChanged;
                currentModel.leftHandActiveDidChange += ActiveStateChanged;
                currentModel.rightUpperArmActiveDidChange += ActiveStateChanged;
                currentModel.rightForeArmActiveDidChange += ActiveStateChanged;
                currentModel.rightHandActiveDidChange += ActiveStateChanged;
            }
        }

        void SetLocalPlayer(LocalPlayer localPlayer)
        {
            if (localPlayer == _localPlayer)
                return;

            _localPlayer = localPlayer;

            if (_localPlayer != null)
            {
                // // Avatar automation try TW:
                //Transform _armature = transform.FindRecursive("Avatar");
                //// Automatic avatar mapping for readyplayer me avatars
                //if (_savefile.avatar == "Asmus" | _savefile.avatar == "Tobias")
                //{
                //    _localPlayer.root = _armature;
                //    _pelvis = _armature.FindRecursive("Hips");
                //    _spine = _armature.FindRecursive("Spine");
                //    _leftThigh = _armature.FindRecursive("LeftUpLeg");
                //    _leftCalf = _armature.FindRecursive("LeftLeg");
                //    _leftFoot = _armature.FindRecursive("LeftFoot");
                //    _rightThigh = _armature.FindRecursive("RightUpLeg");
                //    _rightCalf = _armature.FindRecursive("RightLeg");
                //    _rightFoot = _armature.FindRecursive("RightFoot");
                //    _spine1 = _armature.FindRecursive("Spine1");
                //    _spine2 = _armature.FindRecursive("Spine2");
                //    _neck = _armature.FindRecursive("Neck");
                //    _head = _armature.FindRecursive("Head");
                //    _leftUpperArm = _armature.FindRecursive("LeftUpArm");
                //    _leftForeArm = _armature.FindRecursive("LeftArm");
                //    _leftHand = _armature.FindRecursive("LeftHand");
                //    _rightUpperArm = _armature.FindRecursive("RightUpArm");
                //    _rightForeArm = _armature.FindRecursive("RightArm");
                //    _rightHand = _armature.FindRecursive("Right");
                //}

                RealtimeTransform rootRealtimeTransform = GetComponent<RealtimeTransform>();
                RealtimeTransform pelvisRealtimeTransform = _pelvis != null ? _pelvis.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform spineRealtimeTransform = _spine != null ? _spine.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform leftThighRealtimeTransform = _leftThigh != null ? _leftThigh.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform leftCalfRealtimeTransform = _leftCalf != null ? _leftCalf.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform leftFootRealtimeTransform = _leftFoot != null ? _leftFoot.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform rightThighRealtimeTransform = _rightThigh != null ? _rightThigh.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform rightCalfRealtimeTransform = _rightCalf != null ? _rightCalf.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform rightFootRealtimeTransform = _rightFoot != null ? _rightFoot.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform spine1RealtimeTransform = _spine1 != null ? _spine1.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform spine2RealtimeTransform = _spine2 != null ? _spine2.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform neckRealtimeTransform = _neck != null ? _neck.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform headRealtimeTransform = _head != null ? _head.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform leftUpperArmRealtimeTransform = _leftUpperArm != null ? _leftUpperArm.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform leftForeArmRealtimeTransform = _leftForeArm != null ? _leftForeArm.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform leftHandRealtimeTransform = _leftHand != null ? _leftHand.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform rightUpperArmRealtimeTransform = _rightUpperArm != null ? _rightUpperArm.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform rightForeArmRealtimeTransform = _rightForeArm != null ? _rightForeArm.GetComponent<RealtimeTransform>() : null;
                RealtimeTransform rightHandRealtimeTransform = _rightHand != null ? _rightHand.GetComponent<RealtimeTransform>() : null;

                if (rootRealtimeTransform != null) rootRealtimeTransform.RequestOwnership();
                if (headRealtimeTransform != null) headRealtimeTransform.RequestOwnership();
                if (leftHandRealtimeTransform != null) leftHandRealtimeTransform.RequestOwnership();
                if (rightHandRealtimeTransform != null) rightHandRealtimeTransform.RequestOwnership();
                if (leftUpperArmRealtimeTransform != null) leftUpperArmRealtimeTransform.RequestOwnership();
                if (rightUpperArmRealtimeTransform != null) rightUpperArmRealtimeTransform.RequestOwnership();
                if (leftForeArmRealtimeTransform != null) leftForeArmRealtimeTransform.RequestOwnership();
                if (rightForeArmRealtimeTransform != null) rightForeArmRealtimeTransform.RequestOwnership();
                if (leftThighRealtimeTransform != null) leftThighRealtimeTransform.RequestOwnership();
                if (rightThighRealtimeTransform != null) rightThighRealtimeTransform.RequestOwnership();
                if (leftCalfRealtimeTransform != null) leftCalfRealtimeTransform.RequestOwnership();
                if (rightCalfRealtimeTransform != null) rightCalfRealtimeTransform.RequestOwnership();
                if (leftFootRealtimeTransform != null) leftFootRealtimeTransform.RequestOwnership();
                if (rightFootRealtimeTransform != null) rightFootRealtimeTransform.RequestOwnership();

                if (rootRealtimeTransform != null) rootRealtimeTransform.RequestOwnership();
                if (pelvisRealtimeTransform != null) pelvisRealtimeTransform.RequestOwnership();
                if (spineRealtimeTransform != null) spineRealtimeTransform.RequestOwnership();
                if (leftThighRealtimeTransform != null) leftThighRealtimeTransform.RequestOwnership();
                if (leftCalfRealtimeTransform != null) leftCalfRealtimeTransform.RequestOwnership();
                if (leftFootRealtimeTransform != null) leftFootRealtimeTransform.RequestOwnership();
                if (rightThighRealtimeTransform != null) rightThighRealtimeTransform.RequestOwnership();
                if (rightCalfRealtimeTransform != null) rightCalfRealtimeTransform.RequestOwnership();
                if (rightFootRealtimeTransform != null) rightFootRealtimeTransform.RequestOwnership();
                if (spine1RealtimeTransform != null) spine1RealtimeTransform.RequestOwnership();
                if (spine2RealtimeTransform != null) spine2RealtimeTransform.RequestOwnership();
                if (neckRealtimeTransform != null) neckRealtimeTransform.RequestOwnership();
                if (headRealtimeTransform != null) headRealtimeTransform.RequestOwnership();
                if (leftUpperArmRealtimeTransform != null) leftUpperArmRealtimeTransform.RequestOwnership();
                if (leftForeArmRealtimeTransform != null) leftForeArmRealtimeTransform.RequestOwnership();
                if (leftHandRealtimeTransform != null) leftHandRealtimeTransform.RequestOwnership();
                if (rightUpperArmRealtimeTransform != null) rightUpperArmRealtimeTransform.RequestOwnership();
                if (rightForeArmRealtimeTransform != null) rightForeArmRealtimeTransform.RequestOwnership();
                if (rightHandRealtimeTransform != null) rightHandRealtimeTransform.RequestOwnership();
            }
        }

        void ActiveStateChanged(RealtimeCustomAvatarModel model, bool nodeIsActive)
        {
            // Leave the head active so RealtimeAvatarVoice runs even when the head isn't tracking.
            // if (_head != null) _head.gameObject.SetActive(model.headActive);

            if (_pelvis != null) _pelvis.gameObject.SetActive(model.pelvisActive);
            if (_spine != null) _spine.gameObject.SetActive(model.spineActive);
            if (_leftThigh != null) _leftThigh.gameObject.SetActive(model.leftThighActive);
            if (_leftCalf != null) _leftCalf.gameObject.SetActive(model.leftCalfActive);
            if (_leftFoot != null) _leftFoot.gameObject.SetActive(model.leftFootActive);
            if (_rightThigh != null) _rightThigh.gameObject.SetActive(model.rightThighActive);
            if (_rightCalf != null) _rightCalf.gameObject.SetActive(model.rightCalfActive);
            if (_rightFoot != null) _rightFoot.gameObject.SetActive(model.rightFootActive);
            if (_spine1 != null) _spine1.gameObject.SetActive(model.spine1Active);
            if (_spine2 != null) _spine2.gameObject.SetActive(model.spine2Active);
            if (_neck != null) _neck.gameObject.SetActive(model.neckActive);
            if (_leftUpperArm != null) _leftUpperArm.gameObject.SetActive(model.leftUpperArmActive);
            if (_leftForeArm != null) _leftForeArm.gameObject.SetActive(model.leftForeArmActive);
            if (_leftHand != null) _leftHand.gameObject.SetActive(model.leftHandActive);
            if (_rightUpperArm != null) _rightUpperArm.gameObject.SetActive(model.rightUpperArmActive);
            if (_rightForeArm != null) _rightForeArm.gameObject.SetActive(model.rightForeArmActive);
            if (_rightHand != null) _rightHand.gameObject.SetActive(model.rightHandActive);
        }

        void UpdateAvatarTransformsForLocalPlayer()
        {
            // Make sure this avatar is a local player
            if (_localPlayer == null)
                return;

            // Flags to fetch XRNode position/rotation state
            // bool updatePelvisWithXRNode = false;
            // bool updateSpineWithXRNode = false;
            // bool updateLeftThighWithXRNode = false;
            // bool updateLeftCalfWithXRNode = false;
            // bool updateLeftFootWithXRNode = false;
            // bool updateRightThighWithXRNode = false;
            // bool updateRightCalfWithXRNode = false;
            // bool updateRightFootWithXRNode = false;
            // bool updateSpine1WithXRNode = false;
            // bool updateSpine2WithXRNode = false;
            // bool updateNeckWithXRNode = false;
            bool updateHeadWithXRNode = false;
            // bool updateLeftUpperArmWithXRNode = false;
            // bool updateLeftForeArmWithXRNode = false;
            bool updateLeftHandWithXRNode = false;
            // bool updateRightUpperArmWithXRNode = false;
            // bool updateRightForeArmWithXRNode = false;
            bool updateRightHandWithXRNode = false;

            // Root
            if (_localPlayer.root != null)
            {
                transform.position = _localPlayer.root.position;
                transform.rotation = _localPlayer.root.rotation;
                transform.localScale = _localPlayer.root.localScale;
            }

            // Pelvis
            if (_localPlayer.pelvis != null)
            {
                model.pelvisActive = _localPlayer.pelvis.gameObject.activeSelf;

                _pelvis.position = _localPlayer.pelvis.position;
                _pelvis.rotation = _localPlayer.pelvis.rotation;
            }
            // else
            // {
            //     updatePelvisWithXRNode = true;
            // }

            // Spine
            if (_localPlayer.spine != null)
            {
                model.spineActive = _localPlayer.spine.gameObject.activeSelf;

                _spine.position = _localPlayer.spine.position;
                _spine.rotation = _localPlayer.spine.rotation;
            }
            // else
            // {
            //     updateSpineWithXRNode = true;
            // }

            // Left Thigh
            if (_leftThigh != null)
            {
                if (_localPlayer.leftThigh != null)
                {
                    model.leftThighActive = _localPlayer.leftThigh.gameObject.activeSelf;

                    _leftThigh.position = _localPlayer.leftThigh.position;
                    _leftThigh.rotation = _localPlayer.leftThigh.rotation;
                }
                // else
                // {
                //     updateLeftThighWithXRNode = true;
                // }
            }

            // Left Calf
            if (_leftCalf != null)
            {
                if (_localPlayer.leftCalf != null)
                {
                    model.leftCalfActive = _localPlayer.leftCalf.gameObject.activeSelf;

                    _leftCalf.position = _localPlayer.leftCalf.position;
                    _leftCalf.rotation = _localPlayer.leftCalf.rotation;
                }
                // else
                // {
                //     updateLeftCalfWithXRNode = true;
                // }
            }

            // Left Foot 
            if (_leftFoot != null)
            {
                if (_localPlayer.leftFoot != null)
                {
                    model.leftFootActive = _localPlayer.leftFoot.gameObject.activeSelf;

                    _leftFoot.position = _localPlayer.leftFoot.position;
                    _leftFoot.rotation = _localPlayer.leftFoot.rotation;
                }
                // else
                // {
                //     updateLeftFootWithXRNode = true;
                // }
            }

            // Right Thigh
            if (_rightThigh != null)
            {
                if (_localPlayer.rightThigh != null)
                {
                    model.rightThighActive = _localPlayer.rightThigh.gameObject.activeSelf;

                    _rightThigh.position = _localPlayer.rightThigh.position;
                    _rightThigh.rotation = _localPlayer.rightThigh.rotation;
                }
                // else
                // {
                //     updateRightThighWithXRNode = true;
                // }
            }

            // Right Calf
            if (_rightCalf != null)
            {
                if (_localPlayer.rightCalf != null)
                {
                    model.rightCalfActive = _localPlayer.rightCalf.gameObject.activeSelf;

                    _rightCalf.position = _localPlayer.rightCalf.position;
                    _rightCalf.rotation = _localPlayer.rightCalf.rotation;
                }
                // else
                // {
                //     updateRightCalfWithXRNode = true;
                // }
            }

            // Right Foot
            if (_rightFoot != null)
            {
                if (_localPlayer.rightFoot != null)
                {
                    model.rightFootActive = _localPlayer.rightFoot.gameObject.activeSelf;

                    _rightFoot.position = _localPlayer.rightFoot.position;
                    _rightFoot.rotation = _localPlayer.rightFoot.rotation;
                }
                // else
                // {
                //     updateRightFootWithXRNode = true;
                // }
            }

            // Spine 1
            if (_localPlayer.spine1 != null)
            {
                model.spine1Active = _localPlayer.spine1.gameObject.activeSelf;

                _spine1.position = _localPlayer.spine1.position;
                _spine1.rotation = _localPlayer.spine1.rotation;
            }
            // else
            // {
            //     updateSpine1WithXRNode = true;
            // }

            // Spine 2
            if (_localPlayer.spine2 != null)
            {
                model.spine2Active = _localPlayer.spine2.gameObject.activeSelf;

                _spine2.position = _localPlayer.spine2.position;
                _spine2.rotation = _localPlayer.spine2.rotation;
            }
            // else
            // {
            //     updateSpine2WithXRNode = true;
            // }

            // Neck
            if (_localPlayer.neck != null)
            {
                model.neckActive = _localPlayer.neck.gameObject.activeSelf;

                _neck.position = _localPlayer.neck.position;
                _neck.rotation = _localPlayer.neck.rotation;
            }
            // else
            // {
            //     updateNeckWithXRNode = true;
            // }

            // Head
            if (_localPlayer.head != null)
            {
                model.headActive = _localPlayer.head.gameObject.activeSelf;

                _head.position = _localPlayer.head.position;
                _head.rotation = _localPlayer.head.rotation;
            }
            else
            {
                updateHeadWithXRNode = true;
            }

            // Left UpperArm
            if (_leftUpperArm != null)
            {
                if (_localPlayer.leftUpperArm != null)
                {
                    model.leftUpperArmActive = _localPlayer.leftUpperArm.gameObject.activeSelf;

                    _leftUpperArm.position = _localPlayer.leftUpperArm.position;
                    _leftUpperArm.rotation = _localPlayer.leftUpperArm.rotation;
                }
                // else
                // {
                //     updateLeftUpperArmWithXRNode = true;
                // }
            }

            // Left ForeArm
            if (_leftForeArm != null)
            {
                if (_localPlayer.leftForeArm != null)
                {
                    model.leftForeArmActive = _localPlayer.leftForeArm.gameObject.activeSelf;

                    _leftForeArm.position = _localPlayer.leftForeArm.position;
                    _leftForeArm.rotation = _localPlayer.leftForeArm.rotation;
                }
                // else
                // {
                //     updateLeftForeArmWithXRNode = true;
                // }
            }

            // Left Hand
            if (_leftHand != null)
            {
                if (_localPlayer.leftHand != null)
                {
                    model.leftHandActive = _localPlayer.leftHand.gameObject.activeSelf;

                    _leftHand.position = _localPlayer.leftHand.position;
                    _leftHand.rotation = _localPlayer.leftHand.rotation;
                }
                else
                {
                    updateLeftHandWithXRNode = true;
                }
            }


            // Right UpperArm
            if (_rightUpperArm != null)
            {
                if (_localPlayer.rightUpperArm != null)
                {
                    model.rightUpperArmActive = _localPlayer.rightUpperArm.gameObject.activeSelf;

                    _rightUpperArm.position = _localPlayer.rightUpperArm.position;
                    _rightUpperArm.rotation = _localPlayer.rightUpperArm.rotation;
                }
                // else
                // {
                //     updateRightUpperArmWithXRNode = true;
                // }
            }

            // Right ForeArm
            if (_rightForeArm != null)
            {
                if (_localPlayer.rightForeArm != null)
                {
                    model.rightForeArmActive = _localPlayer.rightForeArm.gameObject.activeSelf;

                    _rightForeArm.position = _localPlayer.rightForeArm.position;
                    _rightForeArm.rotation = _localPlayer.rightForeArm.rotation;
                }
                // else
                // {
                //     updateRightForeArmWithXRNode = true;
                // }
            }

            // Right Hand
            if (_rightHand != null)
            {
                if (_localPlayer.rightHand != null)
                {
                    model.rightHandActive = _localPlayer.rightHand.gameObject.activeSelf;

                    _rightHand.position = _localPlayer.rightHand.position;
                    _rightHand.rotation = _localPlayer.rightHand.rotation;
                }
                else
                {
                    updateRightHandWithXRNode = true;
                }
            }

            // Update head/hands using XRNode APIs if needed
            if (updateHeadWithXRNode || updateLeftHandWithXRNode || updateRightHandWithXRNode)
            {
                InputTracking.GetNodeStates(_nodeStates); // the list is cleared by GetNodeStates

                bool headActive = false;
                bool leftHandActive = false;
                bool rightHandActive = false;

                foreach (XRNodeState nodeState in _nodeStates)
                {
                    if (nodeState.nodeType == XRNode.Head && updateHeadWithXRNode)
                    {
                        headActive = nodeState.tracked;
                        UpdateTransformWithNodeState(_head, nodeState);
                    }
                    else if (nodeState.nodeType == XRNode.LeftHand && updateLeftHandWithXRNode)
                    {
                        leftHandActive = nodeState.tracked;
                        UpdateTransformWithNodeState(_leftHand, nodeState);
                    }
                    else if (nodeState.nodeType == XRNode.RightHand && updateRightHandWithXRNode)
                    {
                        rightHandActive = nodeState.tracked;
                        UpdateTransformWithNodeState(_rightHand, nodeState);
                    }
                }

                if (updateHeadWithXRNode) model.headActive = headActive;
                if (updateLeftHandWithXRNode) model.leftHandActive = leftHandActive;
                if (updateRightHandWithXRNode) model.rightHandActive = rightHandActive;
            }
        }

        private static void UpdateTransformWithNodeState(Transform transform, XRNodeState state)
        {
            if (state.TryGetPosition(out Vector3 position))
            {
                transform.localPosition = position;
            }

            if (state.TryGetRotation(out Quaternion rotation))
            {
                transform.localRotation = rotation;
            }
        }
    }
}

