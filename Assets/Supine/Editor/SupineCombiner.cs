using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using static VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

using ExpressionsMenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;
using ExpressionsMenuControl = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using ExpressionParameter = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter;


/// <summary>
/// avatarにLocomotion, Menu, Parametersを組み込むクラス
/// </summary>

public class SupineCombiner
{
    private GameObject _avatar;
    private string _avatar_name;
    private VRCAvatarDescriptor _avatarDescriptor;
    public bool canCombine = true;
    public bool alreadyCombined = false;

    private string _templatesPath = "Assets\\Supine\\Templates\\";
    private string _generatedPath = "Assets\\Supine\\Generated\\";

    private ExpressionParameter[] OldSupineParameters = new ExpressionParameter[2]
        {
            new ExpressionParameter { name = "VRCLockPose", valueType = ExpressionParameters.ValueType.Int },
            new ExpressionParameter { name = "VRCFootAnchor", valueType = ExpressionParameters.ValueType.Int }
        };
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="avatar">アバターのGameObject</param>
    public SupineCombiner(GameObject avatar)
    {
        _avatar = avatar;
        _avatar_name = avatar.name;
        _avatarDescriptor = avatar.GetComponent<VRCAvatarDescriptor>();

        if (_avatarDescriptor == null)
        {
            // avatar descriptorがなければエラー
            Debug.LogError("[VRCSupine] Could not find VRCAvatarDescriptor.");
            canCombine = false;
        }
        else if (hasGeneratedFiles())
        {
            //  すでに組込済みの場合、(アバター名)_(数字)で作れるようになるまでループ回す
            alreadyCombined = true;
            Debug.Log("[VRCSupine] Directory already exists.");
            int suffix;
            for (suffix=1; hasGeneratedFiles(suffix); suffix++);
            _avatar_name = _avatar_name + "_" + suffix.ToString();
        }
    }

    /// <summary>
    /// avatarにLocomotion, Menu, Parametersの組込実行
    /// </summary>
    public void CombineWithAvatar()
    {
        if (canCombine)
        {
            // SerializedObjectで操作する
            var descriptorObj = new SerializedObject(_avatarDescriptor);
            descriptorObj.FindProperty("customizeAnimationLayers").boolValue = true;
            descriptorObj.FindProperty("customExpressions").boolValue = true;

            // Locomotionを組む
            var locomotionController = CreateAssetFromTemplate<AnimatorController>("SupineLocomotion.controller");
            var layersProp = descriptorObj.FindProperty("baseAnimationLayers.Array");
            var layerProp = layersProp.GetArrayElementAtIndex(0);
            layerProp.FindPropertyRelative("isDefault").boolValue = false;
            var controllerProp = layerProp.FindPropertyRelative("animatorController");
            controllerProp.objectReferenceValue = locomotionController;

            // ExMenuを組む
            var exMenu = CreateAssetFromTemplate<ExpressionsMenu>("MainMenu.asset");
            var descriptorMenuProp = descriptorObj.FindProperty("expressionsMenu");

            ExpressionsMenu descriptorMenu = _avatarDescriptor.expressionsMenu;
            if (descriptorMenu == null) descriptorMenu = new ExpressionsMenu();
            var descriptorControls = descriptorMenu.controls;
            if (descriptorControls == null) descriptorControls = new List<ExpressionsMenuControl>();

            exMenu.controls = AssembleExMenuControls(descriptorControls, exMenu.controls);

            descriptorMenuProp.objectReferenceValue = exMenu;

            // ExParametersを組む
            var exParameters = CreateAssetFromTemplate<ExpressionParameters>("SupineParameters.asset");
            var descriptorParamsProp = descriptorObj.FindProperty("expressionParameters");

            var descriptorParams = _avatarDescriptor.expressionParameters;
            if (descriptorParams == null) descriptorParams = new ExpressionParameters();
            var descriptorParamsArray = descriptorParams.parameters;
            if (descriptorParamsArray == null) descriptorParamsArray = new ExpressionParameter[0];

            exParameters.parameters = AssembleExParameters(descriptorParamsArray, exParameters.parameters);

            descriptorParamsProp.objectReferenceValue = exParameters;
            
            // 変更を適用
            descriptorObj.ApplyModifiedProperties();

            Debug.Log("[VRCSupine] Created the directory '" + generatedDirectory() + "'.");
            Debug.Log("[VRCSupine] Combination is done.");

        } else {
            Debug.LogError("[VRCSupine] Could not combine with this avatar.");
        }
    }

    private T CreateAssetFromTemplate<T>(string name) where T : Object
    {
        string generatedPath = generatedDirectory() + "\\" + name;
        string templatePath = _templatesPath + name;
        
        if (!Directory.Exists(generatedDirectory()))
        {
            Directory.CreateDirectory(generatedDirectory());
        }

        if (!AssetDatabase.CopyAsset(templatePath, generatedPath))
        {
            Debug.LogError("[VRCSupine] Could not create asset: (" + generatedPath + ") from: (" + templatePath + ")");
            throw new IOException();
        }

        return AssetDatabase.LoadAssetAtPath<T>(generatedPath);
    }

    private string generatedDirectory(int suffix = 0)
    {
        if (suffix > 0) {
            return _generatedPath + _avatar_name + "_" + suffix.ToString();
        }
        else
        {
            return _generatedPath + _avatar_name;
        }
    }

    private bool hasGeneratedFiles(int suffix = 0)
    {
        return AssetDatabase.IsValidFolder(generatedDirectory(suffix));
    }

    private List<ExpressionsMenuControl> AssembleExMenuControls(List<ExpressionsMenuControl> baseControls, List<ExpressionsMenuControl> templateControls)
    {
        // 結合して重複要素を削除
        var concatinated = baseControls.Concat(templateControls);
        var uniqued = concatinated.Distinct(new ExMenuControlComparer()).ToList<ExpressionsMenuControl>();
        return uniqued;
    }

    private ExpressionParameter[] AssembleExParameters(ExpressionParameter[] baseParams, ExpressionParameter[] templateParams)
    {
        // 結合して重複要素を削除＆古いごろ寝パラメータがあれば削除
        var baseParamsList = new List<ExpressionParameter>(baseParams);
        var concatinated = baseParamsList.Concat(templateParams);
        var uniqued = concatinated.Distinct(new ExParameterComparer()).ToList<ExpressionParameter>();
        uniqued.RemoveAll(IsOldSupineParameter);
        return uniqued.ToArray<ExpressionParameter>();
    }

    private bool IsOldSupineParameter(ExpressionParameter parameter)
    {
        // 古いごろ寝パラメータと一致するか
        return OldSupineParameters.Contains(parameter, new ExParameterComparer());
    }
}