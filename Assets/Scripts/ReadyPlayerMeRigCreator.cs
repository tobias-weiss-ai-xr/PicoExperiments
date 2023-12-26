using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ReadyPlayerMeRigCreator : MonoBehaviour
{

    public RigBuilder rigBuilder;
    #region UpperRig
    [Header("UpperRig")]
    public Rig upperBodyRig;
    public TwoBoneIKConstraint leftArmConstraint;
    public TwoBoneIKConstraint rightArmConstraint;
    public Transform leftArm;
    public Transform leftHand;
    public Transform leftForeArm;
    public Transform rightArm;
    public Transform rightHand;
    public Transform rightForeArm;
    #endregion

    #region LowerRig
    [Header("LowerRig")]
    public Rig lowerBodyRig;
    public TwoBoneIKConstraint leftLegConstraint;
    public TwoBoneIKConstraint rightLegConstraint;
    public Transform leftUpLeg;
    public Transform leftLeg;
    public Transform leftFoot;
    public Transform rightUpLeg;
    public Transform rightLeg;
    public Transform rightFoot;
    #endregion


    #region Arms
    public string FindArmParts()
    {
        string result = "";
        result += FindBodyPart(ref leftArm, "LeftArm");
        result += FindBodyPart(ref leftHand, "LeftHand");
        result += FindBodyPart(ref leftForeArm, "LeftForeArm");
        result += FindBodyPart(ref rightArm, "RightArm");
        result += FindBodyPart(ref rightHand, "RightHand");
        result += FindBodyPart(ref rightForeArm, "RightForeArm");
        if(result == "")
        {
            result = "All arm-parts in armature found.";
        }
        return result;
    }

    public List<MissingBodyRigObjects> CheckForValidArmRig()
    {
        if (!rigBuilder)
        {
            rigBuilder = GetComponent<RigBuilder>();
            if (!rigBuilder)
            {
                return new List<MissingBodyRigObjects>() { MissingBodyRigObjects.CORERIG };
            }
        }
        if (!upperBodyRig)
        {
            upperBodyRig = GetComponentInChildren<Rig>();
            if (!upperBodyRig)
            {
                return new List<MissingBodyRigObjects>() { MissingBodyRigObjects.BODYRIG };
            }
        }
        List<MissingBodyRigObjects> missingConstraints = new List<MissingBodyRigObjects>();
        if (!leftArmConstraint)
        {
            TwoBoneIKConstraint[] boneConstraints = upperBodyRig.GetComponentsInChildren<TwoBoneIKConstraint>();
            IEnumerable<TwoBoneIKConstraint> lhConstraints = boneConstraints.Where(x => x.gameObject.name.Contains("_L") && x.gameObject.name.Contains("Hand"));
            if (lhConstraints.Count() == 0)
            {
                missingConstraints.Add(MissingBodyRigObjects.LEFTARM);
            }
            else
            {
                leftArmConstraint = lhConstraints.First();
            }

        }
        if (!rightArmConstraint)
        {
            TwoBoneIKConstraint[] boneConstraints = upperBodyRig.GetComponentsInChildren<TwoBoneIKConstraint>();
            IEnumerable<TwoBoneIKConstraint> rhConstraints = boneConstraints.Where(x => x.gameObject.name.Contains("_R") && x.gameObject.name.Contains("Hand"));
            if (rhConstraints.Count() == 0)
            {
                missingConstraints.Add(MissingBodyRigObjects.RIGHTARM);
            }
            else
            {
                rightArmConstraint = rhConstraints.First();
            }
        }

        return missingConstraints;
    }

    public List<MissingBodyRigReferences> CheckForValidArmReferences()
    {
        List<MissingBodyRigReferences> missingReferences = new List<MissingBodyRigReferences>();
        if (rigBuilder.layers.Count == 0 || !rigBuilder.layers.Select(x => x.rig).Contains(upperBodyRig))
        {
            missingReferences.Add(MissingBodyRigReferences.BUILDERTORIG);
        }
        if (!ValidateTwoBoneIKConstraint(leftArmConstraint.data))
        {
            missingReferences.Add(MissingBodyRigReferences.L_HAND);
        }
        if (!ValidateTwoBoneIKConstraint(rightArmConstraint.data))
        {
            missingReferences.Add(MissingBodyRigReferences.R_HAND);
        }
        return missingReferences;
    }

    public void CreateMissingArmGameobjectsAndComponents(IEnumerable<MissingBodyRigObjects> missingRigs)
    {
        foreach (MissingBodyRigObjects mbro in missingRigs)
        {
            switch (mbro)
            {
                case MissingBodyRigObjects.CORERIG: gameObject.AddComponent<RigBuilder>(); break;
                case MissingBodyRigObjects.BODYRIG:
                    GameObject rigHolder = new GameObject("BodyRig");
                    rigHolder.transform.SetParent(transform);
                    rigHolder.AddComponent<Rig>();
                    upperBodyRig = rigHolder.GetComponent<Rig>();
                    break;
                case MissingBodyRigObjects.LEFTARM:
                    GameObject leftArmHolder = new GameObject("Rig_Hand_L");
                    leftArmHolder.transform.SetParent(upperBodyRig.transform);
                    leftArmHolder.AddComponent<TwoBoneIKConstraint>();
                    leftArmConstraint = leftArmHolder.GetComponent<TwoBoneIKConstraint>();
                    GameObject leftArmHint = new GameObject("Rig_Elbow_L_Hint");
                    leftArmHint.transform.SetParent(upperBodyRig.transform);
                    break;
                case MissingBodyRigObjects.RIGHTARM:
                    GameObject rightArmHolder = new GameObject("Rig_Hand_R");
                    rightArmHolder.transform.SetParent(upperBodyRig.transform);
                    rightArmHolder.AddComponent<TwoBoneIKConstraint>();
                    GameObject rightArmHint = new GameObject("Rig_Elbow_R_Hint");
                    rightArmHint.transform.SetParent(upperBodyRig.transform);
                    break;
            }
        }
    }

    public void FixMissingArmRefs(IEnumerable<MissingBodyRigReferences> missingRigs)
    {
        foreach (MissingBodyRigReferences mbrr in missingRigs)
        {
            switch (mbrr)
            {
                case MissingBodyRigReferences.BUILDERTORIG:
                    rigBuilder.layers.Add(new RigLayer(upperBodyRig, true));
                    break;
                case MissingBodyRigReferences.L_HAND:
                    leftArmConstraint.data.root = leftArm;
                    leftArmConstraint.data.mid = leftForeArm;
                    leftArmConstraint.data.tip = leftHand;
                    leftArmConstraint.data.target = upperBodyRig.transform.FindRecursive("Rig_Hand_L");
                    leftArmConstraint.data.hint = upperBodyRig.transform.FindRecursive("Rig_Elbow_L_Hint");
                    break;
                case MissingBodyRigReferences.R_HAND:
                    rightArmConstraint.data.root = rightArm;
                    rightArmConstraint.data.mid = rightForeArm;
                    rightArmConstraint.data.tip = rightHand;
                    rightArmConstraint.data.target = upperBodyRig.transform.FindRecursive("Rig_Hand_R");
                    rightArmConstraint.data.hint = upperBodyRig.transform.FindRecursive("Rig_Elbow_R_Hint");
                    break;
            }
        }
    }
    public void AlignArmObjects()
    {
        leftArmConstraint.data.target.transform.position = leftHand.position;
        rightArmConstraint.data.target.transform.position = rightHand.position;
        Vector3 lhintPosition = leftForeArm.position + (leftArm.position - leftForeArm.position) * 0.2f;
        leftArmConstraint.data.hint.transform.position = lhintPosition;
        Vector3 rhintPosition = rightForeArm.position + (rightArm.position - rightForeArm.position) * 0.2f;
        rightArmConstraint.data.hint.transform.position = rhintPosition;
    }

    #endregion
    #region Legs

    public string FindLegParts()
    {
        string result = "";
        result += FindBodyPart(ref leftLeg, "LeftLeg");
        result += FindBodyPart(ref leftUpLeg, "LeftUpLeg");
        result += FindBodyPart(ref leftFoot, "LeftFoot");
        result += FindBodyPart(ref rightLeg, "RightLeg");
        result += FindBodyPart(ref rightUpLeg, "RightUpLeg");
        result += FindBodyPart(ref rightFoot, "RightFoot");
        if (result == "")
        {
            result = "All leg-parts in armature found.";
        }
        return result;
    }

    public List<MissingBodyRigObjects> CheckForValidLegRig()
    {
        if (!rigBuilder)
        {
            rigBuilder = GetComponent<RigBuilder>();
            if (!rigBuilder)
            {
                return new List<MissingBodyRigObjects>() { MissingBodyRigObjects.CORERIG };
            }
        }
        if (!lowerBodyRig)
        {
            lowerBodyRig = GetComponentInChildren<Rig>();
            if (!lowerBodyRig)
            {
                return new List<MissingBodyRigObjects>() { MissingBodyRigObjects.BODYRIG };
            }
        }
        List<MissingBodyRigObjects> missingConstraints = new List<MissingBodyRigObjects>();
        if (!leftLegConstraint)
        {
            TwoBoneIKConstraint[] boneConstraints = lowerBodyRig.GetComponentsInChildren<TwoBoneIKConstraint>();
            IEnumerable<TwoBoneIKConstraint> lhConstraints = boneConstraints.Where(x => x.gameObject.name.Contains("_L") && x.gameObject.name.Contains("Leg"));
            if (lhConstraints.Count() == 0)
            {
                missingConstraints.Add(MissingBodyRigObjects.LEFTLEG);
            }
            else
            {
                leftLegConstraint = lhConstraints.First();
            }

        }
        if (!rightLegConstraint)
        {
            TwoBoneIKConstraint[] boneConstraints = lowerBodyRig.GetComponentsInChildren<TwoBoneIKConstraint>();
            IEnumerable<TwoBoneIKConstraint> rhConstraints = boneConstraints.Where(x => x.gameObject.name.Contains("_R") && x.gameObject.name.Contains("Leg"));
            if (rhConstraints.Count() == 0)
            {
                missingConstraints.Add(MissingBodyRigObjects.RIGHTLEG);
            }
            else
            {
                rightLegConstraint = rhConstraints.First();
            }
        }

        return missingConstraints;
    }

    public List<MissingBodyRigReferences> CheckForValidLegReferences()
    {
        List<MissingBodyRigReferences> missingReferences = new List<MissingBodyRigReferences>();
        if (rigBuilder.layers.Count == 0 || !rigBuilder.layers.Select(x => x.rig).Contains(lowerBodyRig))
        {
            missingReferences.Add(MissingBodyRigReferences.BUILDERTORIG);
        }
        if (!ValidateTwoBoneIKConstraint(leftLegConstraint.data))
        {
            missingReferences.Add(MissingBodyRigReferences.L_LEG);
        }
        if (!ValidateTwoBoneIKConstraint(rightLegConstraint.data))
        {
            missingReferences.Add(MissingBodyRigReferences.R_LEG);
        }
        return missingReferences;
    }

    public void CreateMissingLegGameobjectsAndComponents(IEnumerable<MissingBodyRigObjects> missingRigs)
    {
        foreach (MissingBodyRigObjects mbro in missingRigs)
        {
            switch (mbro)
            {
                case MissingBodyRigObjects.CORERIG: gameObject.AddComponent<RigBuilder>(); break;
                case MissingBodyRigObjects.RIGHTLEG:
                    GameObject rightLegHolder = new GameObject("Rig_Leg_R");
                    rightLegHolder.transform.SetParent(lowerBodyRig.transform);
                    rightLegHolder.AddComponent<TwoBoneIKConstraint>();
                    IK_Feet rightFoot = rightLegHolder.AddComponent<IK_Feet>();
                    rightFoot.groundLayer = LayerMask.GetMask(new string[] { "Ground Layer" });
                    GameObject rightLegHint = new GameObject("Rig_Knee_R_Hint");
                    rightLegHint.transform.SetParent(lowerBodyRig.transform);
                    if(leftLegConstraint != null)
                    {
                        IK_Feet other = leftLegConstraint.GetComponent<IK_Feet>();
                        rightFoot.otherFoot = other;
                        other.otherFoot = rightFoot;
                    }
                    break;
                case MissingBodyRigObjects.LEFTLEG:
                    GameObject leftLegHolder = new GameObject("Rig_Leg_L");
                    leftLegHolder.transform.SetParent(upperBodyRig.transform);
                    leftLegHolder.AddComponent<TwoBoneIKConstraint>();
                    IK_Feet leftFoot = leftLegHolder.AddComponent<IK_Feet>();
                    leftFoot.groundLayer = LayerMask.GetMask(new string[] { "Ground Layer" });
                    GameObject leftLegHint = new GameObject("Rig_Knee_L_Hint");
                    leftLegHint.transform.SetParent(lowerBodyRig.transform);
                    
                    break;
            }
        }
    }
    public void FixMissingLegRefs(IEnumerable<MissingBodyRigReferences> missingRigs)
    {
        foreach (MissingBodyRigReferences mbrr in missingRigs)
        {
            switch (mbrr)
            {
                case MissingBodyRigReferences.BUILDERTORIG:
                    rigBuilder.layers.Add(new RigLayer(lowerBodyRig, true));
                    break;
                case MissingBodyRigReferences.L_LEG:
                    leftLegConstraint.data.root = leftUpLeg;
                    leftLegConstraint.data.mid = leftLeg;
                    leftLegConstraint.data.tip = leftFoot;
                    leftLegConstraint.data.target = lowerBodyRig.transform.FindRecursive("Rig_Leg_L");
                    leftLegConstraint.data.hint = lowerBodyRig.transform.FindRecursive("Rig_Knee_L_Hint");
                    leftLegConstraint.GetComponent<IK_Feet>().body = this.transform;
                    if (rightLegConstraint != null)
                    {
                        IK_Feet other = rightLegConstraint.GetComponent<IK_Feet>();
                        IK_Feet here = leftLegConstraint.GetComponent<IK_Feet>();
                        here.otherFoot = other;
                        other.otherFoot = here;
                    }
                    break;
                case MissingBodyRigReferences.R_LEG:
                    rightLegConstraint.data.root = rightUpLeg;
                    rightLegConstraint.data.mid = rightLeg;
                    rightLegConstraint.data.tip = rightFoot;
                    rightLegConstraint.data.target = lowerBodyRig.transform.FindRecursive("Rig_Leg_R");
                    rightLegConstraint.data.hint = lowerBodyRig.transform.FindRecursive("Rig_Knee_R_Hint");
                    rightLegConstraint.GetComponent<IK_Feet>().body = this.transform;
                    if (leftLegConstraint != null)
                    {
                        IK_Feet other = rightLegConstraint.GetComponent<IK_Feet>();
                        IK_Feet here = leftLegConstraint.GetComponent<IK_Feet>();
                        here.otherFoot = other;
                        other.otherFoot = here;
                    }
                    break;
                    
            }
        }
    }

    public void AlignLegObjects()
    {
        Debug.LogError("NOT YET IMPLEMENTED");
    }

    #endregion

    #region Generic

    public string FindBodyPart(ref Transform t, string nameToFind)
    {
        if (t)
        {
            return "";
        } else
        {
            t = transform.FindRecursive(nameToFind);
        }
        if (!t)
        {
            return "Couldnt find object with name " + nameToFind + " in Armature";
        }
        return "";
    }

    public bool ValidateTwoBoneIKConstraint(TwoBoneIKConstraintData data)
    {
        return data.root != null && data.mid != null && data.target != null && data.hint != null && data.tip != null;
    }
    #endregion

}

public enum MissingBodyRigReferences
{
    BUILDERTORIG, L_HAND, R_HAND, L_LEG, R_LEG
}

public enum MissingBodyRigObjects
{
    CORERIG, BODYRIG, LEFTARM, RIGHTARM, RIGHTLEG, LEFTLEG
}

