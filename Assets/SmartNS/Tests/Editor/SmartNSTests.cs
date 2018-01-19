using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

namespace GraviaSoftware.SmartNS.Tests.Editor
{

    public class NewEditModeTest
    {

        [Test]
        public void NamespaceValueTest()
        {
            // Basic Tests, to ensure normal namespaces are created.
            Assert.AreEqual( "Assets", GraviaSoftware.SmartNS.SmartNS.GetNamespaceValue( "Assets/myScript.cs", "", "", "" ) );
            Assert.AreEqual( "Assets.SomeFolder", GraviaSoftware.SmartNS.SmartNS.GetNamespaceValue( "Assets/SomeFolder/myScript.cs", "", "", "" ) );
            Assert.AreEqual( "Assets.SomeFolder", GraviaSoftware.SmartNS.SmartNS.GetNamespaceValue( "Assets/Some Folder/myScript.cs", "", "", "" ) );
            Assert.AreEqual( "Assets.Some_Folder", GraviaSoftware.SmartNS.SmartNS.GetNamespaceValue( "Assets/Some.Folder/myScript.cs", "", "", "" ) );

            // Ensure that Script Root gets trimmed.
            Assert.IsNull( GraviaSoftware.SmartNS.SmartNS.GetNamespaceValue( "Assets/myScript.cs", "Assets", "", "" ) );
            Assert.AreEqual( "SomeFolder", GraviaSoftware.SmartNS.SmartNS.GetNamespaceValue( "Assets/SomeFolder/myScript.cs", "Assets", "", "" ) );
            Assert.AreEqual( "SomeFolder", GraviaSoftware.SmartNS.SmartNS.GetNamespaceValue( "Assets/Some Folder/myScript.cs", "Assets", "", "" ) );
            Assert.AreEqual( "Some_Folder", GraviaSoftware.SmartNS.SmartNS.GetNamespaceValue( "Assets/Some.Folder/myScript.cs", "Assets", "", "" ) );

            // Prefix tests
            Assert.AreEqual( "MyPrefix.Assets.SomeFolder", GraviaSoftware.SmartNS.SmartNS.GetNamespaceValue( "Assets/SomeFolder/myScript.cs", "", "MyPrefix", "" ) );
            Assert.AreEqual( "MyPrefix.SomeFolder", GraviaSoftware.SmartNS.SmartNS.GetNamespaceValue( "Assets/SomeFolder/myScript.cs", "Assets", "MyPrefix", "" ) );

            // Universal Tests
            Assert.AreEqual( "UniversalValue", GraviaSoftware.SmartNS.SmartNS.GetNamespaceValue( "Assets/Some Folder/myScript.cs", "", "", "UniversalValue" ) );
            Assert.AreEqual( "UniversalValue", GraviaSoftware.SmartNS.SmartNS.GetNamespaceValue( "Assets/Some Folder/myScript.cs", "UniversalValue", "", "UniversalValue" ) );
            Assert.AreEqual( "UniversalValue", GraviaSoftware.SmartNS.SmartNS.GetNamespaceValue( "Assets/Some Folder/myScript.cs", "Assets", "", "UniversalValue" ) );
            Assert.AreEqual( "UniversalValue", GraviaSoftware.SmartNS.SmartNS.GetNamespaceValue( "Assets/Some Folder/myScript.cs", "", "PreFix", "UniversalValue" ) );

        }

    }
}
