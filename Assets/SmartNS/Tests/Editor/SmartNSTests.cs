using NUnit.Framework;
using smartNs = GraviaSoftware.SmartNS.Editor.SmartNS;

namespace GraviaSoftware.SmartNS.Tests.Editor
{

    public class NewEditModeTest
    {

        [Test]
        public void NamespaceValueTest()
        {
            // Basic Tests, to ensure normal namespaces are created.
            Assert.AreEqual("Assets", smartNs.GetNamespaceValue("Assets/myScript.cs", "", "", ""));
            Assert.AreEqual("Assets.SomeFolder", smartNs.GetNamespaceValue("Assets/SomeFolder/myScript.cs", "", "", ""));
            Assert.AreEqual("Assets.SomeFolder", smartNs.GetNamespaceValue("Assets/Some Folder/myScript.cs", "", "", ""));
            Assert.AreEqual("Assets.Some_Folder", smartNs.GetNamespaceValue("Assets/Some.Folder/myScript.cs", "", "", ""));

            // Ensure that Script Root gets trimmed.
            Assert.IsNull(smartNs.GetNamespaceValue("Assets/myScript.cs", "Assets", "", ""));
            Assert.AreEqual("SomeFolder", smartNs.GetNamespaceValue("Assets/SomeFolder/myScript.cs", "Assets", "", ""));
            Assert.AreEqual("SomeFolder", smartNs.GetNamespaceValue("Assets/Some Folder/myScript.cs", "Assets", "", ""));
            Assert.AreEqual("Some_Folder", smartNs.GetNamespaceValue("Assets/Some.Folder/myScript.cs", "Assets", "", ""));

            // Prefix tests
            Assert.AreEqual("MyPrefix.Assets.SomeFolder", smartNs.GetNamespaceValue("Assets/SomeFolder/myScript.cs", "", "MyPrefix", ""));
            Assert.AreEqual("MyPrefix.SomeFolder", smartNs.GetNamespaceValue("Assets/SomeFolder/myScript.cs", "Assets", "MyPrefix", ""));
            Assert.AreEqual("MyPrefix", smartNs.GetNamespaceValue("Assets/myScript.cs", "Assets", "MyPrefix", ""));

            // Universal Tests
            Assert.AreEqual("UniversalValue", smartNs.GetNamespaceValue("Assets/Some Folder/myScript.cs", "", "", "UniversalValue"));
            Assert.AreEqual("UniversalValue", smartNs.GetNamespaceValue("Assets/Some Folder/myScript.cs", "UniversalValue", "", "UniversalValue"));
            Assert.AreEqual("UniversalValue", smartNs.GetNamespaceValue("Assets/Some Folder/myScript.cs", "Assets", "", "UniversalValue"));
            Assert.AreEqual("UniversalValue", smartNs.GetNamespaceValue("Assets/Some Folder/myScript.cs", "", "PreFix", "UniversalValue"));

        }

    }
}
