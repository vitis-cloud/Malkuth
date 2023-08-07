using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using static VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
using VRC.SDK3.Avatars.Components;
using System.IO;
using VRC.SDK3.Avatars.ScriptableObjects;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using System.Reflection;

public class SupineCombinerEditor : EditorWindow
{
    private GameObject avatar;
    private SupineCombiner supineCombiner;
    private bool _canCombine = false;
    private string _retryMessage = "Cannot combine.";

    [MenuItem("MinMinMart/Supine Combiner")]
    private static void Create()
    {
        GetWindow<SupineCombinerEditor>("Supine Combiner");
    }

    private void OnGUI()
    {
        using (new GUILayout.HorizontalScope())
        {
            avatar = EditorGUILayout.ObjectField("Avatar", avatar, typeof(GameObject), true) as GameObject;
        }

        using (new GUILayout.VerticalScope())
        {
            using (new EditorGUI.DisabledGroupScope(!avatar))
            {
                if (GUILayout.Button("Check"))
                {
                    supineCombiner = new SupineCombiner(avatar);
                    if (supineCombiner.canCombine)
                    {
                        _canCombine = true;
                        Debug.Log("[VRCSupine] Check OK.");
                    }
                    else
                    {
                        _canCombine = false;
                        EditorUtility.DisplayDialog("Check failure", _retryMessage, "OK");
                        Debug.Log("[VRCSupine] Check failed. " + _retryMessage);
                    }

                    if (supineCombiner.alreadyCombined)
                    {
                        EditorUtility.DisplayDialog("Already combined", "Combined files are exists.\rTry to create other directory then combine again.", "OK");
                    }
                }
            }

            using (new EditorGUI.DisabledGroupScope(!_canCombine))
            {
                if (GUILayout.Button("Combine"))
                {
                    try
                    {
                        supineCombiner.CombineWithAvatar();
                    }
                    catch (IOException e)
                    {
                        EditorUtility.DisplayDialog("Combine failure", _retryMessage, "OK");
                        throw e;
                    }
                    EditorUtility.DisplayDialog("Combine successful", "Combined!", "OK");
                    _canCombine = false;
                }
            }
        }
    }
}