using System.Collections.Generic;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    public abstract class VFXBlockType
    {
        protected void Add(VFXProperty property)
        {
            m_Properties.Add(property);
        }

        protected void Add(VFXAttribute attrib)
        {
            m_Attributes.Add(attrib);
        }      

        public string Name
        {
            get { return m_Name; }
            protected set { m_Name = value; }
        }
        
        public string Icon
        {
            get { return m_Icon; }
            protected set { m_Icon = value; }
        }
        
        public string Category
        {
            get { return m_Category; }
            protected set { m_Category = value; }
        }

        public string Description
        {
            get { return m_Description; }
            protected set { m_Description = value; }
        }

        public string Source
        {
            get { return m_Source; }
            protected set { m_Source = value; }
        }

        public List<VFXProperty> Properties
        {
            get { return m_Properties; }
        }

        public List<VFXAttribute> Attributes
        {
            get { return m_Attributes; }
        }

        private string m_Name = "";
        private string m_Icon = "";
        private string m_Category = "";
        private string m_Description = "";
        private string m_Source = "";

        private List<VFXProperty> m_Properties = new List<VFXProperty>();
        private List<VFXAttribute> m_Attributes = new List<VFXAttribute>();
    }

    // Test
    class VFXBlockTest : VFXBlockType
    {
        public VFXBlockTest()
        {
            Name = "Test";
            Icon = "";
            Category = "Tests";
            Description = "This is a description of the block";

            Add(VFXProperty.Create<VFXCurveType>("myCurve"));
            Add(VFXProperty.Create<VFXFloatType>("myFloat"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
float f = myFloat * 2.0f;
position = SAMPLE(myCurve,myFloat);";

        }
    }

}