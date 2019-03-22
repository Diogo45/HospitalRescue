using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System;


namespace K_PathFinder {
    [CustomEditor(typeof(AgentProperties))]
    public class AgentPropertiesEditor : Editor {
        int setThisMuch = 2;
        public override void OnInspectorGUI() {
            AgentProperties myTarget = (AgentProperties)target;
            if (myTarget == null)
                return;

            float currentLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 160f;

            var serializedObject = new SerializedObject(myTarget);
            var tagsProperty = serializedObject.FindProperty("ignoredTags");
            var includedLayersProperty = serializedObject.FindProperty("includedLayers");
            serializedObject.Update();

            myTarget.voxelsPerChunk = EditorGUILayout.IntField(new GUIContent("Voxel per chunk side", "amount of voxel per chunk side. voxel size are chunk size / this number"), myTarget.voxelsPerChunk);
            if (myTarget.voxelsPerChunk < 10)
                myTarget.voxelsPerChunk = 10;

            float foxelSize = PathFinder.gridSize / myTarget.voxelsPerChunk;
            EditorGUILayout.LabelField("Voxel Size", (foxelSize).ToString());

            myTarget.radius = EditorGUILayout.FloatField(new GUIContent("Radius", "agent radius in world space"), myTarget.radius);

            EditorGUILayout.LabelField("Voxel per radius", ((int)(myTarget.radius / foxelSize)).ToString());

            GUILayout.BeginHorizontal();
            setThisMuch = EditorGUILayout.IntField(setThisMuch, GUILayout.MaxWidth(100));
            if (GUILayout.Button("Set this much")) {
                myTarget.voxelsPerChunk = Mathf.CeilToInt(PathFinder.gridSize / (myTarget.radius / setThisMuch));
            }
            GUILayout.EndHorizontal();

            GUILayout.Box(string.Empty, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            myTarget.height = EditorGUILayout.FloatField(new GUIContent("Height", "agent height in world space"), myTarget.height);
            myTarget.maxSlope = EditorGUILayout.FloatField(new GUIContent("Max slope", "maximum slope in degree"), myTarget.maxSlope);
            myTarget.maxSlope = Mathf.Clamp(myTarget.maxSlope, 0f, 89f);

            myTarget.maxStepHeight = EditorGUILayout.FloatField(new GUIContent("Step height", "maximum step peight in world space. describe how much height difference agent can handle while moving up and down") , myTarget.maxStepHeight);
            if (myTarget.maxStepHeight < 0f)
                myTarget.maxStepHeight = 0f;

            myTarget.offsetMultiplier = EditorGUILayout.Slider(new GUIContent("Offset multiplier", "In order chunk to be more precise pathfinder must  take into account nearby obstacles outside chunk. This value will tell how much area it should take into account. 1 = agent radius"), myTarget.offsetMultiplier, 0, 2);

            EditorGUILayout.PropertyField(tagsProperty, true);
            EditorGUILayout.PropertyField(includedLayersProperty, true);
            serializedObject.ApplyModifiedProperties();


            GUILayout.Box(string.Empty, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });

            myTarget.doNavMesh = EditorGUILayout.Toggle(new GUIContent("Do NavMesh", "do NavMesh at all. (maybe you just need grid or covers?)"), myTarget.doNavMesh);
            if (myTarget.doNavMesh) {
                myTarget.walkMod = EditorGUILayout.FloatField(new GUIContent("Walk mod", "generic move cost modifyer. (maybe you need one?) 1f == move cost equal to distance of movement"), myTarget.walkMod);
                myTarget.canCrouch = EditorGUILayout.Toggle(new GUIContent("Can crouch", "If true then Pathfinder will add aditional area where agent can crouch"), myTarget.canCrouch);

                if (myTarget.canCrouch) {
                    myTarget.crouchHeight = EditorGUILayout.FloatField(new GUIContent("Crouch height", "lowest limit where crouch start in world units (upper obliviously is agent height)"), myTarget.crouchHeight);
                    myTarget.crouchMod = EditorGUILayout.FloatField(new GUIContent("Crouch mod", "crouch move cost modifyer,  1f == move cost equal to distance of movement"), myTarget.crouchMod);

                    if (myTarget.crouchHeight < 0 | myTarget.crouchHeight > myTarget.height)
                        myTarget.crouchHeight = myTarget.height * 0.5f;
                }

                myTarget.canJump = EditorGUILayout.Toggle(new GUIContent("Can jump", "If true then Pathfinder will add aditional info about jump spots"), myTarget.canJump);

                if (myTarget.canJump) {
                    myTarget.JumpDown = EditorGUILayout.FloatField(new GUIContent("Jump down", "Max distance to jump down"), myTarget.JumpDown);
                    myTarget.jumpDownMod = EditorGUILayout.FloatField(new GUIContent("Jump down mod", "Cost modifyer to jump down"), myTarget.jumpDownMod);
                    myTarget.JumpUp = EditorGUILayout.FloatField(new GUIContent("Jump up", "Max distance to jump up"), myTarget.JumpUp);
                    myTarget.jumpUpMod = EditorGUILayout.FloatField(new GUIContent("Jump up mod", "Cost modifyer to jump up"), myTarget.jumpUpMod);
                }
            }

            GUILayout.Box(string.Empty, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            myTarget.canCover = EditorGUILayout.Toggle(new GUIContent("Do Cover", "If true then Pathfinder will add aditional info about covers"), myTarget.canCover);

            if (myTarget.canCover) {
                myTarget.coverExtraSamples = EditorGUILayout.IntField(new GUIContent("Extra samples", "Cover on diagonal can be funky. Here you can add some extra samples into it"), myTarget.coverExtraSamples);

                if (myTarget.coverExtraSamples < 0)
                    myTarget.coverExtraSamples = 0;

                myTarget.fullCover = EditorGUILayout.FloatField(new GUIContent("Cover full", "How much height are considered as cover"), myTarget.fullCover);
                myTarget.canHalfCover = EditorGUILayout.Toggle(new GUIContent("Can half cover", "should we add half covers?"), myTarget.canHalfCover);         

                //if half cover too low
                if (myTarget.canHalfCover) {
                    myTarget.halfCover = EditorGUILayout.FloatField(new GUIContent("Cover half", "How much height are considered as half cover"), myTarget.halfCover);

                    if (myTarget.halfCover < 0)
                        myTarget.halfCover = 0;
                }

                //if full covet too low
                if (myTarget.canHalfCover) {
                    if (myTarget.fullCover < myTarget.halfCover)
                        myTarget.fullCover = myTarget.halfCover;
                }
                else {
                    if (myTarget.fullCover < 0)
                        myTarget.fullCover = 0;
                }
            }

            GUILayout.Box(string.Empty, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            myTarget.battleGrid = EditorGUILayout.Toggle(new GUIContent("Do Battle grid", "If true then Pathfinder will add battle grid"), myTarget.battleGrid);

            if (myTarget.battleGrid) 
                myTarget.battleGridDensity = EditorGUILayout.IntField(new GUIContent("Battle grid density", "Size of space in battle grid in voxels. every this much voxel we sample data"), myTarget.battleGridDensity);
            GUILayout.Box(string.Empty, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });

            EditorGUIUtility.labelWidth = currentLabelWidth;

            if (GUI.changed) 
                EditorUtility.SetDirty(myTarget);            
        }        
    }
}


