using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MVCTool
{
    [RequireComponent(typeof(Animator))]
    public class MVCAvatar : MonoBehaviour
    {
        [field: Header("References")]
        [field: SerializeField, ReadOnly] public Animator Animator { get; private set; }

        [Header("Hand Direction")]
        [SerializeField, ReadOnly] private Transform _rightHandReference;
        [SerializeField, ReadOnly] private Transform _leftHandReference;

        public Transform Head { get; private set; }
        public Transform RightArm { get; private set; }
        public Transform RightForearm { get; private set; }
        public Transform RightHand { get; private set; }
        public Transform LeftHand { get; private set; }
        public Transform LeftArm { get; private set; }
        public Transform LeftForearm { get; private set; }

        [Header("Rigging")]
        [SerializeField, ReadOnly] private RigBuilder _rigBuilder;
        [SerializeField, ReadOnly] private Rig _rig;
        [SerializeField, ReadOnly] private MultiParentConstraint _headIK;
        [SerializeField, ReadOnly] private TwoBoneIKConstraint _rightHandIK;
        [SerializeField, ReadOnly] private TwoBoneIKConstraint _leftHandIK;

        private void Awake()
        {
            Animator = GetComponent<Animator>();
            AssignBoneVariables();
        }

        private void OnValidate()
        {
            Animator = GetComponent<Animator>();
            AssignBoneVariables();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            float lineThickness = 10f;
            float lineLength = 0.5f;
            float sphereRadius = 0.1f;

            // Disable ZTest so it draws through everything
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            if (_rightHandReference != null)
            {
                Handles.color = Color.black;
                Handles.SphereHandleCap(0, _rightHandReference.position, Quaternion.identity, sphereRadius, EventType.Repaint);
                // Finger direction
                Handles.color = Color.blue;
                Handles.DrawAAPolyLine(lineThickness, _rightHandReference.position, _rightHandReference.position + lineLength * _rightHandReference.right);
                Handles.Label(_rightHandReference.position + lineLength / 2f * _rightHandReference.right, "Right Fingers");
                // Thumb direction
                Handles.color = Color.green;
                Handles.DrawAAPolyLine(lineThickness, _rightHandReference.position, _rightHandReference.position + lineLength * _rightHandReference.forward);
                Handles.Label(_rightHandReference.position + lineLength / 2f * _rightHandReference.forward, "Right Thumb");
                // Palm direction
                Handles.color = Color.red;
                Handles.DrawAAPolyLine(lineThickness, _rightHandReference.position, _rightHandReference.position + lineLength * -_rightHandReference.up);
                Handles.Label(_rightHandReference.position + lineLength / 2f * -_rightHandReference.up, "Right Palm");
            }

            if (_leftHandReference != null)
            {
                Handles.color = Color.black;
                Handles.SphereHandleCap(0, _leftHandReference.position, Quaternion.identity, sphereRadius, EventType.Repaint);
                // Finger direction
                Handles.color = Color.blue;
                Handles.DrawAAPolyLine(lineThickness, _leftHandReference.position, _leftHandReference.position + lineLength * -_leftHandReference.right);
                Handles.Label(_leftHandReference.position + lineLength / 2f * -_leftHandReference.right, "Left Fingers");
                // Thumb direction
                Handles.color = Color.green;
                Handles.DrawAAPolyLine(lineThickness, _leftHandReference.position, _leftHandReference.position + lineLength * _leftHandReference.forward);
                Handles.Label(_leftHandReference.position + lineLength / 2f * _leftHandReference.forward, "Left Thumb");
                // Palm direction
                Handles.color = Color.red;
                Handles.DrawAAPolyLine(lineThickness, _leftHandReference.position, _leftHandReference.position + lineLength * -_leftHandReference.up);
                Handles.Label(_leftHandReference.position + lineLength / 2f * -_leftHandReference.up, "Left Palm");
            }

            // Reset ZTest to default after drawing
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        }
#endif

        /// <summary>
        /// Gets the bone transforms for the head and hands from the Animator.
        /// Assigns them to the public properties for easy access.
        /// </summary>
        private void AssignBoneVariables()
        {
            Head = Animator.GetBoneTransform(HumanBodyBones.Head);

            RightArm = Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            RightForearm = Animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            RightHand = Animator.GetBoneTransform(HumanBodyBones.RightHand);

            LeftArm = Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            LeftForearm = Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            LeftHand = Animator.GetBoneTransform(HumanBodyBones.LeftHand);
        }

        public void CreateHandReferences()
        {
            AssignBoneVariables();

            if (_rightHandReference == null)
                _rightHandReference = new GameObject("RightHandReference").transform;

            _rightHandReference.SetParent(RightHand);
            _rightHandReference.localPosition = Vector3.zero;

            if (_leftHandReference == null)
                _leftHandReference = new GameObject("LeftHandReference").transform;

            _leftHandReference.SetParent(LeftHand);
            _leftHandReference.localPosition = Vector3.zero;
        }

        /// <summary>
        /// Automatically sets up the rig for the avatar. Missing the targets for the IK because VR avatar will be in MVC.
        /// </summary>
        /// <returns></returns>
        public void CreateRig()
        {
            // If RigBuilder already exists, remove it
            if (gameObject.TryGetComponent(out _rigBuilder))
            {
                // If the rigbuilder has rigs, destroy it
                foreach (var layer in _rigBuilder.layers)
                {
                    if (layer.rig != null)
                        DestroyImmediate(layer.rig.gameObject, true);
                }
                DestroyImmediate(_rigBuilder, true);
            }
            _rigBuilder = gameObject.AddComponent<RigBuilder>();

            // Create rig and connect to rig builder
            if (_rig != null)
                DestroyImmediate(_rig.gameObject, true);
            GameObject rigObject = new GameObject("Rig(AutoGenerated)");
            rigObject.transform.SetParent(transform);
            rigObject.transform.localPosition = Vector3.zero;
            _rig = rigObject.AddComponent<Rig>();
            _rigBuilder.layers.Add(new RigLayer(_rig));

            // Setup head IK
            if (_headIK != null)
                DestroyImmediate(_headIK.gameObject, true);
            MultiParentConstraint headIK = new GameObject("HeadIK").AddComponent<MultiParentConstraint>();
            headIK.transform.SetParent(_rig.transform);
            headIK.transform.localPosition = Vector3.zero;
            headIK.Reset();
            headIK.data.constrainedObject = Head;
            _headIK = headIK;

            // Setup right hand IK
            if (_rightHandIK != null)
                DestroyImmediate(_rightHandIK.gameObject, true);
            TwoBoneIKConstraint rightHandIK = new GameObject("RightHandIK").AddComponent<TwoBoneIKConstraint>();
            rightHandIK.transform.SetParent(_rig.transform);
            rightHandIK.transform.localPosition = Vector3.zero;
            rightHandIK.Reset();
            rightHandIK.data.root = RightArm;
            rightHandIK.data.mid = RightForearm;
            rightHandIK.data.tip = RightHand;
            Transform rightIKHint = new GameObject("RightHandIKHint").transform;
            rightIKHint.SetParent(rightHandIK.transform);
            rightIKHint.localPosition = -0.5f * Vector3.one;
            rightHandIK.data.hint = rightIKHint;
            _rightHandIK = rightHandIK;

            // Setup left hand IK
            if (_leftHandIK != null)
                DestroyImmediate(_leftHandIK.gameObject, true);
            TwoBoneIKConstraint leftHandIK = new GameObject("LeftHandIK").AddComponent<TwoBoneIKConstraint>();
            leftHandIK.transform.SetParent(_rig.transform);
            leftHandIK.transform.localPosition = Vector3.zero;
            leftHandIK.Reset();
            leftHandIK.data.root = LeftArm;
            leftHandIK.data.mid = LeftForearm;
            leftHandIK.data.tip = LeftHand;
            Transform leftIKHint = new GameObject("LeftHandIKHint").transform;
            leftIKHint.SetParent(leftHandIK.transform);
            leftIKHint.localPosition = -0.5f * Vector3.one;
            leftHandIK.data.hint = leftIKHint;
            _leftHandIK = leftHandIK;
        }

        /// <summary>
        /// Configures the rig with the provided head and hand transforms.
        /// </summary>
        public void PairRig(Transform head, Transform rightHand, Transform leftHand)
        {
            // Create the hand targets
            Transform rightHandTarget = new GameObject("RightHandTarget(AutoGenerated)").transform;
            rightHandTarget.SetParent(rightHand);
            rightHandTarget.localPosition = Vector3.zero;
            rightHandTarget.localRotation = Quaternion.identity;
            Transform leftHandTarget = new GameObject("LeftHandTarget(AutoGenerated)").transform;
            leftHandTarget.SetParent(leftHand);
            leftHandTarget.localPosition = Vector3.zero;
            leftHandTarget.localRotation = Quaternion.identity;

            // Setup IK
            WeightedTransformArray headIKSources = new WeightedTransformArray();
            headIKSources.Add(new WeightedTransform(head, 1.0f));
            _headIK.data.sourceObjects = headIKSources;
            _rightHandIK.data.target = rightHandTarget;
            _leftHandIK.data.target = leftHandTarget;

            _rigBuilder.Build();

            StartCoroutine(AlignPalmsAfterFrame(rightHand, leftHand, rightHandTarget, leftHandTarget));
        }

        private IEnumerator AlignPalmsAfterFrame(Transform rightHand, Transform leftHand, Transform rightHandTarget, Transform leftHandTarget)
        {
            yield return null;

            // RIGHT HAND
            Vector3 rightReferenceFingers = _rightHandReference.right;
            Vector3 rightReferenceThumb = _rightHandReference.forward;
            Vector3 rightReferencePalm = -_rightHandReference.up;

            Vector3 rightTargetFingers = rightHand.forward;
            Vector3 rightTargetThumb = rightHand.up;
            Vector3 rightTargetPalm = -rightHand.right;

            Quaternion rightAlignRotation = Quaternion.LookRotation(
                rightTargetPalm,  // forward
                rightTargetThumb  // up
            );

            Quaternion rightReferenceRotation = Quaternion.LookRotation(
                rightReferencePalm,
                rightReferenceThumb
            );

            rightHandTarget.rotation = rightAlignRotation * Quaternion.Inverse(rightReferenceRotation);


            // LEFT HAND
            Vector3 leftReferenceFingers = -_leftHandReference.right;
            Vector3 leftReferenceThumb = _leftHandReference.forward;
            Vector3 leftReferencePalm = -_leftHandReference.up;

            Vector3 leftTargetFingers = leftHand.forward;
            Vector3 leftTargetThumb = leftHand.up;
            Vector3 leftTargetPalm = leftHand.right;

            Quaternion leftAlignRotation = Quaternion.LookRotation(
                leftTargetPalm,
                leftTargetThumb
            );

            Quaternion leftReferenceRotation = Quaternion.LookRotation(
                leftReferencePalm,
                leftReferenceThumb
            );

            leftHandTarget.rotation = leftAlignRotation * Quaternion.Inverse(leftReferenceRotation);
        }

        /// <summary>
        /// Checks if the avatar is ready for upload by making sure all necessary references are set.
        /// </summary>
        public bool IsReadyForUpload()
        {
            if (_rightHandReference == null)
                return false;
            if (_leftHandReference == null)
                return false;

            if (_rigBuilder == null)
                return false;
            if (_rig == null)
                return false;

            if (_headIK == null)
                return false;
            if (_rightHandIK == null)
                return false;
            if (_leftHandIK == null)
                return false;

            return true;
        }

        /// <summary>
        /// Assigns the provided animator controller to the avatar's Animator component.
        /// </summary>
        public void AssignAnimatorController(RuntimeAnimatorController controller)
        {
            if(controller == null)
            {
                Debug.LogError($"Animator controller is missing, cannot assign null controller to animator.");
                return;
            }

            Animator.runtimeAnimatorController = controller;
        }
    }
}
