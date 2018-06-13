using System;
using System.Runtime.InteropServices;

namespace Westwind.wwReflection
{

	[ClassInterface(ClassInterfaceType.AutoDual)]
	[ProgId("wwReflection.ObjectMethod")]
	[Serializable]
	public class ObjectMethod 
	{
		public string Name = "";
		public string Parameters = "";
		public string RawParameters = "";
		public string DescriptiveParameters = "";
		public string ReturnType = "";
        public string ReturnDescription = "";
		public string Scope = "";
        
		public bool Static = false; 
		public bool Literal = false;
		public bool bInternal = false;
        public bool bConstructor = false;
		public string cOther = "";

		public string[] aParameters = null;
        public MethodParameter[] aParameters2 = null;
		public int nParameterCount = 0;

		// *** Parsed from XMLDocs
		public string cHelpText = String.Empty;
		public string cRemarks = String.Empty;
		public string cExample = String.Empty;
        public string cExceptions = String.Empty;
        public string cContract = string.Empty;
		public string cSeeAlso = String.Empty;

		public string cSignature = String.Empty;
        public string cRawSignature = String.Empty;

        /// <summary>
        /// Determines the parent type
        /// - bad naming in hind sight. This maps to ReflectedType.
        /// </summary>
		public string cDeclaringType = String.Empty;
        
        /// <summary>
        /// Determines the actual type that implements this member
        /// - bad naming in hind sight. This maps to DeclaringType.
        /// </summary>
        public string cImplementedType = String.Empty;
        public string cGenericParameters = String.Empty;

        public override string ToString()
        {
            if (string.IsNullOrEmpty(this.cName))
                return base.ToString();

            return this.cName;
        }
	}

	[ClassInterface(ClassInterfaceType.AutoDual)]
	[ProgId("wwReflection.ObjectProperty")]
	[Serializable]
	public class ObjectProperty
	{
		public string cName = String.Empty;
		public string cType = String.Empty;
        		
		public string cScope = String.Empty;
		public string cAccessType = String.Empty;
		public string cDefaultValue = String.Empty;

		public bool bStatic = false;
		public bool bReadOnly = false;
		public bool bInternal = false;
		public bool bLiteral = false;

		public string cOther = String.Empty;
		
		/// Field or Property
		public string cFieldOrProperty = String.Empty;

		// *** Parsed from XML docs
		public string cHelpText = String.Empty;
		public string cRemarks = String.Empty;
		public string cExample = String.Empty;
        public string cContract = "";
		public string cSeeAlso = String.Empty;

		public string cSignature = String.Empty;
		public string cDeclaringType = String.Empty;


        /// <summary>
        /// Determines the actual type that implements this member
        /// - bad naming in hind sight. This maps to DeclaringType.
        /// </summary>
        public string cImplementedType = String.Empty;

        public override string ToString()
        {
            if (string.IsNullOrEmpty(this.cName))
                return base.ToString();

            return this.cName;
        }
	}

	[Serializable]
	public class ObjectEvent 
	{
		public string cName = String.Empty;
		public string cType = String.Empty;
        		
		public string cScope = String.Empty;
		public bool bStatic = false;        
		public bool bReadOnly = false;

		public string cOther = String.Empty;
		
		public string cHelpText = String.Empty;
		public string cRemarks = String.Empty;
		public string cExample = "";
		public string cSeeAlso = "";

		public string cSignature = "";
		public string cDeclaringType = "";
        /// <summary>
        /// Determines the actual type that implements this member
        /// - bad naming in hind sight. This maps to DeclaringType.
        /// </summary>
        public string cImplementedType = "";
        
        public override string ToString()
        {
            if (string.IsNullOrEmpty(this.cName))
                return base.ToString();

            return this.cName;
        }
    }

    [Serializable]
    public class MethodParameter
    {
        public string cName = "";
        public string cTypeName = "";
        public string cShortTypeName = "";
    }
}
