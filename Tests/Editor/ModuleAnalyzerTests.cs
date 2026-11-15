using NUnit.Framework;
using UnityEngine;

namespace BuildKit.Tests
{
    public class ModuleAnalyzerTests
    {
        [Test]
        public void CubeMeasuresOneUnit()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var module = cube.AddComponent<BuildModule>();
                module.Analyze();

                Assert.IsTrue(module.analyzed);
                Assert.AreEqual(1f, module.LocalSize.x, 0.0001f);
                Assert.AreEqual(1f, module.LocalSize.y, 0.0001f);
                Assert.AreEqual(1f, module.LocalSize.z, 0.0001f);
                Assert.AreEqual(-0.5f, module.BottomCenterLocal.y, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(cube);
            }
        }

        [Test]
        public void ChildOffsetIsIncludedAndRootScaleIsExcluded()
        {
            var root = new GameObject("root");
            try
            {
                root.transform.localScale = new Vector3(2f, 2f, 2f);
                var child = GameObject.CreatePrimitive(PrimitiveType.Cube);
                child.transform.SetParent(root.transform, false);
                child.transform.localPosition = new Vector3(0f, 0.5f, 0f);

                var module = root.AddComponent<BuildModule>();
                module.Analyze();

                Assert.AreEqual(1f, module.LocalSize.y, 0.0001f);
                Assert.AreEqual(0f, module.BottomCenterLocal.y, 0.0001f);
                Assert.AreEqual(2f, module.WorldSize.y, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EmptyObjectIsNotAnalyzed()
        {
            var go = new GameObject("empty");
            try
            {
                var module = go.AddComponent<BuildModule>();
                module.Analyze();
                Assert.IsFalse(module.analyzed);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
