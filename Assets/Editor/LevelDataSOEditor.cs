using UnityEngine;
using UnityEditor;
using PairPop.Data;

namespace PairPop.EditorTools {
    [CustomEditor(typeof(LevelDataSO))]
    public class LevelDataSOEditor : Editor {
        public override void OnInspectorGUI() {
            LevelDataSO level = (LevelDataSO)target;

            GUILayout.Label("PairPop Level Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Vẽ các thuộc tính mặc định, sau đó ta cảnh báo
            DrawDefaultInspector();

            EditorGUILayout.Space();
            GUILayout.Label("Validation Analysis", EditorStyles.boldLabel);

            // Kiểm tra số lượng chia ban đầu
            if (level.activeGroupCount > level.totalGroupCount) {
                EditorGUILayout.HelpBox("LỖI: activeGroupCount lớn hơn totalGroupCount!", MessageType.Error);
            }

            int totalCardCount = level.totalGroupCount * level.columnCount;
            EditorGUILayout.LabelField($"Total Cards to solve: {totalCardCount}", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Auto-Config Columns & Validate")) {
                level.columnCount = 4; // Bắt buộc là 4 bài mỗi dòng (theo luật game)
                EditorUtility.SetDirty(level);
            }
        }
    }
}
