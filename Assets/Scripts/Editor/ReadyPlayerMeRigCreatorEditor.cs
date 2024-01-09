using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[CustomEditor(typeof(ReadyPlayerMeRigCreator))]
public class ReadyPlayerMeRigCreatorEditor : Editor
{
    bool showOriginalUI = false;
    private Dictionary<MissingBodyRigObjects, string> errorMessagesObjects = new Dictionary<MissingBodyRigObjects, string>()
    {
        {MissingBodyRigObjects.CORERIG, "Couldnt find RigBuilder on this Object" },
        {MissingBodyRigObjects.BODYRIG, "Couldnt find Rig on a child of this Object" },
        {MissingBodyRigObjects.LEFTARM, "Couldnt find left arm constraint, looking for a child of the Rig with name containing Hand and L" },
        {MissingBodyRigObjects.RIGHTARM, "Couldnt find left arm constraint, looking for a child of the Rig with name containing Hand and R" },
    };
    private Dictionary<MissingBodyRigReferences, string> errorMessagesReferences = new Dictionary<MissingBodyRigReferences, string>()
    {
        {MissingBodyRigReferences.BUILDERTORIG, "Builder Rig Reference Missing" },
        {MissingBodyRigReferences.L_HAND, "Some references missing on L_Hand object" },
        {MissingBodyRigReferences.R_HAND, "Some references missing on R_Hand object" },
    };

    public override void OnInspectorGUI()
    {
        showOriginalUI = EditorGUILayout.Toggle("Show base layout", showOriginalUI);
        if (showOriginalUI)
        {
            base.OnInspectorGUI();
            return;
        }
        ReadyPlayerMeRigCreator creator = (ReadyPlayerMeRigCreator)target;
        PaintHead(creator);
        PaintArms(creator);
        PaintLegs(creator);       
        if (GUILayout.Button("AttachToXR"))
        {
            creator.AttachToPico();
        }
    }

    public void PaintArms(ReadyPlayerMeRigCreator creator)
    {
        List<MissingBodyRigObjects> missingRigs = creator.CheckForValidArmRig();
        EditorGUILayout.LabelField(creator.FindArmParts());
        if (missingRigs.Count() == 0)
        {
            EditorGUILayout.LabelField("All arm components are in place.");
        }
        else
        {
            foreach (MissingBodyRigObjects mbrc in missingRigs)
            {
                errorMessagesObjects.TryGetValue(mbrc, out string errormsg);
                EditorGUILayout.LabelField(errormsg);
            }
            if (GUILayout.Button("Create Missing Arm Components"))
            {
                creator.CreateMissingArmGameobjectsAndComponents(missingRigs);
            }
            return;
        }
        List<MissingBodyRigReferences> missingRefs = creator.CheckForValidArmReferences();
        if (missingRefs.Count() == 0)
        {
            EditorGUILayout.LabelField("All references seem to be in place.");
        }
        else
        {
            foreach (MissingBodyRigReferences mbrr in missingRefs)
            {
                errorMessagesReferences.TryGetValue(mbrr, out string errormsg);
                EditorGUILayout.LabelField(errormsg);
            }
            if (GUILayout.Button("Set missing Arm Refs"))
            {
                creator.FixMissingArmRefs(missingRefs);
            }
        }
        if (GUILayout.Button("Align Arm Objects"))
        {
            creator.AlignArmObjects();
        }
    }
    public void PaintLegs(ReadyPlayerMeRigCreator creator)
    {
        EditorGUILayout.LabelField(creator.FindLegParts());
        List<MissingBodyRigObjects> missingRigs = creator.CheckForValidLegRig();
        if (missingRigs.Count() == 0)
        {
            EditorGUILayout.LabelField("All leg components are in place.");
        }
        else
        {
            foreach (MissingBodyRigObjects mbrc in missingRigs)
            {
                errorMessagesObjects.TryGetValue(mbrc, out string errormsg);
                EditorGUILayout.LabelField(errormsg);
            }
            if (GUILayout.Button("Create Missing leg Components"))
            {
                creator.CreateMissingLegGameobjectsAndComponents(missingRigs);
            }
            return;
        }
        List<MissingBodyRigReferences>  missingRefs = creator.CheckForValidLegReferences();
        if (missingRefs.Count() == 0)
        {
            EditorGUILayout.LabelField("All leg references seem to be in place.");
        }
        else
        {
            foreach (MissingBodyRigReferences mbrr in missingRefs)
            {
                errorMessagesReferences.TryGetValue(mbrr, out string errormsg);
                EditorGUILayout.LabelField(errormsg);
            }
            if (GUILayout.Button("Set missing Leg Refs"))
            {
                creator.FixMissingLegRefs(missingRefs);
            }
        }
        if (GUILayout.Button("Align Leg Objects"))
        {
            creator.AlignLegObjects();
        }
    }

    public void PaintHead(ReadyPlayerMeRigCreator creator)
    {
        EditorGUILayout.LabelField(creator.FindHead());
        List<MissingBodyRigObjects> missingRigs = creator.CheckForValidHeadRig();
        if (missingRigs.Count() == 0)
        {
            EditorGUILayout.LabelField("All head components are in place.");
        }
        else
        {
            foreach (MissingBodyRigObjects mbrc in missingRigs)
            {
                errorMessagesObjects.TryGetValue(mbrc, out string errormsg);
                EditorGUILayout.LabelField(errormsg);
            }
            if (GUILayout.Button("Create Missing head Components"))
            {
                creator.CreateMissingHeadGameobjectsAndComponents(missingRigs);
            }
            return;
        }        
        if (GUILayout.Button("Align Head Objects"))
        {
            creator.AlignHeadObjects();
        }
    }




}

