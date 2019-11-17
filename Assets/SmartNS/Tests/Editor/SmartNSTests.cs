using NUnit.Framework;
using smartNs = GraviaSoftware.SmartNS.Editor.SmartNS;

namespace GraviaSoftware.SmartNS.Tests.Editor
{
    public class SmartNSTests
    {
        [Test]
        public void EnsureValidNamespacesAreCreatedFromInvalidCharacters()
        {
            // Basic Tests, to ensure normal namespaces are created.
            Assert.AreEqual("Assets", smartNs.GetNamespaceValue("Assets/myScript.cs", "", "", ""));
            Assert.AreEqual("Assets.SomeFolder", smartNs.GetNamespaceValue("Assets/SomeFolder/myScript.cs", "", "", ""));
            Assert.AreEqual("Assets.SomeFolder", smartNs.GetNamespaceValue("Assets/Some Folder/myScript.cs", "", "", ""));
            Assert.AreEqual("Assets.Some_Folder", smartNs.GetNamespaceValue("Assets/Some.Folder/myScript.cs", "", "", ""));
        }

        [Test]
        public void EnsureProperNamespacesForFilesPlacedAtScriptRoot()
        {
            // In this test, we're told SmartNS to strip off anything starting with "Assets". But if the script is located directly
            // in Assets, that means there's nothing left to create a namespace with. In that case, no namespace will be added.
            Assert.IsNull(smartNs.GetNamespaceValue("Assets/myScript.cs", "Assets", "", ""));


            // Make sure the same concept work in subdirectories
            Assert.IsNull(smartNs.GetNamespaceValue("Assets/IgnoreThis/myScript.cs", "Assets/IgnoreThis", "", ""));


            // What should happen this this case? We've put a script at a higher level than the script root.
            // In that case, we're expect no stripping to occur at all, and we expect the namespace to
            // contains "Assets"
            Assert.AreEqual("Assets", smartNs.GetNamespaceValue("Assets/myScript.cs", "Assets/IgnoreThis", "", ""));
        }

        [Test]
        public void EnsurePrefixesAreRemovedFromNamespaces()
        {
            // Ensure that Script Root gets trimmed.
            Assert.IsNull(smartNs.GetNamespaceValue("Assets/myScript.cs", "Assets", "", ""));
            Assert.AreEqual("SomeFolder", smartNs.GetNamespaceValue("Assets/SomeFolder/myScript.cs", "Assets", "", ""));
            Assert.AreEqual("SomeFolder", smartNs.GetNamespaceValue("Assets/Some Folder/myScript.cs", "Assets", "", ""));

        }

        [Test]
        public void AdditionalPrefixTests()
        {
            // Prefix tests
            Assert.AreEqual("MyPrefix.Assets.SomeFolder", smartNs.GetNamespaceValue("Assets/SomeFolder/myScript.cs", "", "MyPrefix", ""));
            Assert.AreEqual("MyPrefix.SomeFolder", smartNs.GetNamespaceValue("Assets/SomeFolder/myScript.cs", "Assets", "MyPrefix", ""));
            Assert.AreEqual("MyPrefix", smartNs.GetNamespaceValue("Assets/myScript.cs", "Assets", "MyPrefix", ""));

        }

        [Test]
        public void EnsureUniveralValueBehavior()
        {
            // Universal Tests
            Assert.AreEqual("UniversalValue", smartNs.GetNamespaceValue("Assets/Some Folder/myScript.cs", "", "", "UniversalValue"));
            Assert.AreEqual("UniversalValue", smartNs.GetNamespaceValue("Assets/Some Folder/myScript.cs", "UniversalValue", "", "UniversalValue"));
            Assert.AreEqual("UniversalValue", smartNs.GetNamespaceValue("Assets/Some Folder/myScript.cs", "Assets", "", "UniversalValue"));
            Assert.AreEqual("UniversalValue", smartNs.GetNamespaceValue("Assets/Some Folder/myScript.cs", "", "PreFix", "UniversalValue"));

        }

        [Test]
        public void EnsureNumericDirectoryNamesProduceValidNamespaces()
        {
            // Make sure the namespace doesn't start any portion with a number. 
            Assert.AreEqual("_3rdParty", smartNs.GetNamespaceValue("Assets/3rdParty/myScript.cs", "Assets", "", ""));
            Assert.AreEqual("Scripts._123Scripts", smartNs.GetNamespaceValue("Assets/Scripts/123Scripts/myScript.cs", "Assets", "", ""));
            Assert.AreEqual("_3rdParty._123Scripts", smartNs.GetNamespaceValue("Assets/3rdParty/123Scripts/myScript.cs", "Assets", "", ""));
        }

        [Test]
        public void EnsureSpecialCharacterReplacement()
        {
            // If one of the directories in the path contains a ".", replace that with a "_"
            Assert.AreEqual("Some_Dir", smartNs.GetNamespaceValue("Assets/Some.Dir/myScript.cs", "Assets", "", ""));
        }


    }
}
