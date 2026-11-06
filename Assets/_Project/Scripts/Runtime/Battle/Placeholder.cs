using UnityEngine;

namespace HearTheTide.Battle
{
    /// <summary>美术留白：用基础几何体代替角色与场景模型。替换时只需把模型挂到对应节点下。</summary>
    public static class Placeholder
    {
        public static GameObject Primitive(PrimitiveType type, string name, Color color, Transform parent, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            if (!keepCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
            }
            SetColor(go, color);
            return go;
        }

        public static void SetColor(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            // material.color 对 URP Lit（_BaseColor 标了 [MainColor]）和内置 Standard 都有效
            if (r != null) r.material.color = color;
        }

        /// <summary>角色占位：胶囊身体 + 朝向标记（小方块代表"脸"朝向）。</summary>
        public static GameObject Character(string name, Color color, Transform parent, float scale = 1f)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var body = Primitive(PrimitiveType.Capsule, "Body(美术留白)", color, root.transform);
            body.transform.localPosition = new Vector3(0, 1f, 0);
            var nose = Primitive(PrimitiveType.Cube, "Facing", Color.Lerp(color, Color.white, 0.5f), body.transform);
            nose.transform.localPosition = new Vector3(0, 0.5f, 0.45f);
            nose.transform.localScale = new Vector3(0.3f, 0.15f, 0.3f);
            root.transform.localScale = Vector3.one * scale;
            return root;
        }
    }
}
