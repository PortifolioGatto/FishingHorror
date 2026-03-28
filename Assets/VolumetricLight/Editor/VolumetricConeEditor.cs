using UnityEngine;
using UnityEditor;

namespace VolumetricLight
{
    [CustomEditor(typeof(VolumetricConeMesh))]
    public class VolumetricConeEditor : Editor
    {
        private SerializedProperty baseRadius, tipRadius, length, segments, rings;
        private SerializedProperty renderStart, renderEnd;
        private SerializedProperty lightColor, intensity;
        private SerializedProperty coneMaterial;

        private void OnEnable()
        {
            baseRadius = serializedObject.FindProperty("baseRadius");
            tipRadius = serializedObject.FindProperty("tipRadius");
            length = serializedObject.FindProperty("length");
            segments = serializedObject.FindProperty("segments");
            rings = serializedObject.FindProperty("rings");
            renderStart = serializedObject.FindProperty("renderStart");
            renderEnd = serializedObject.FindProperty("renderEnd");
            lightColor = serializedObject.FindProperty("lightColor");
            intensity = serializedObject.FindProperty("intensity");
            coneMaterial = serializedObject.FindProperty("coneMaterial");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            VolumetricConeMesh cone = (VolumetricConeMesh)target;

            // Header
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Volumetric Light Cone", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Cone Shape
            EditorGUILayout.LabelField("Cone Shape", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(baseRadius, new GUIContent("Base Radius"));
            EditorGUILayout.PropertyField(tipRadius, new GUIContent("Tip Radius"));
            EditorGUILayout.PropertyField(length, new GUIContent("Length"));
            EditorGUILayout.PropertyField(segments, new GUIContent("Segments"));
            EditorGUILayout.PropertyField(rings, new GUIContent("Rings"));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);

            // Render Range com MinMaxSlider
            EditorGUILayout.LabelField("Render Range", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;

            float start = renderStart.floatValue;
            float end = renderEnd.floatValue;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Range", GUILayout.Width(EditorGUIUtility.labelWidth));
            start = EditorGUILayout.FloatField(start, GUILayout.Width(50));
            EditorGUILayout.MinMaxSlider(ref start, ref end, 0f, 1f);
            end = EditorGUILayout.FloatField(end, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            renderStart.floatValue = Mathf.Clamp(start, 0f, 1f);
            renderEnd.floatValue = Mathf.Clamp(end, 0f, 1f);

            // Info box
            float visibleLength = (end - start) * cone.length;
            EditorGUILayout.HelpBox(
                $"Visible: {start:P0} to {end:P0} ({visibleLength:F1}m of {cone.length:F1}m total)",
                MessageType.Info
            );

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);

            // Light Settings
            EditorGUILayout.LabelField("Light Settings", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(lightColor, new GUIContent("Color"));
            EditorGUILayout.PropertyField(intensity, new GUIContent("Intensity"));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);

            // Material
            EditorGUILayout.LabelField("References", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(coneMaterial, new GUIContent("Material"));
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // Botão para forçar regeneração
            if (GUILayout.Button("Regenerate Mesh", GUILayout.Height(30)))
            {
                cone.GenerateMesh();
                EditorUtility.SetDirty(target);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
