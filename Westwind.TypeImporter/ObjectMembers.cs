using System;

namespace Westwind.TypeImporter
{
	public class ObjectMethod 
	{
		public string Name = "";
		public string Parameters = "";
		public string ReturnType = "";
        public string ReturnDescription = "";
		public string Scope = "";
        
		public bool Static = false; 
		public bool Literal = false;
		public bool Internal = false;
        public bool Constructor = false;
		public string Other = "";

		public string[] ParameterList = null;
        public MethodParameter[] ParametersList2 = null;
		public int ParameterCount = 0;

		// *** Parsed from XMLDocs
		public string HelpText = string.Empty;
		public string Remarks = string.Empty;
		public string Example = string.Empty;
        public string Exceptions = string.Empty;
        public string Contract = string.Empty;
		public string SeeAlso = string.Empty;

		public string Signature = string.Empty;
        public string RawSignature = string.Empty;

        /// <summary>
        /// Determines the parent type
        /// - bad naming in hind sight. This maps to ReflectedType.
        /// </summary>
		public string DeclaringType = string.Empty;
        
        /// <summary>
        /// Determines the actual type that implements this member
        /// - bad naming in hind sight. This maps to DeclaringType.
        /// </summary>
        public string ImplementedType = string.Empty;
        public string GenericParameters = string.Empty;
	    public string RawParameters;
	    public string DescriptiveParameters;

	    public override string ToString()
        {
            if (string.IsNullOrEmpty(Name))
                return base.ToString();

            return Name;
        }
	}

	public class ObjectProperty
	{
		public string Name = string.Empty;
		public string Type = string.Empty;
        		
		public string Scope = string.Empty;
		public string AccessType = string.Empty;
		public string DefaultValue = string.Empty;

		public bool Static = false;
		public bool ReadOnly = false;
		public bool Internal = false;
		public bool Literal = false;

		public string Other = string.Empty;
		
		/// Field or Property
		public string FieldOrProperty = string.Empty;

		// *** Parsed from XML docs
		public string HelpText = string.Empty;
		public string Remarks = string.Empty;
		public string Example = string.Empty;
        public string Contract = "";
		public string SeeAlso = string.Empty;

		public string Signature = string.Empty;
		public string DeclaringType = string.Empty;


        /// <summary>
        /// Determines the actual type that implements this member
        /// - bad naming in hind sight. This maps to DeclaringType.
        /// </summary>
        public string ImplementedType = string.Empty;

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Name))
                return base.ToString();

            return Name;
        }
	}

	
	public class ObjectEvent 
	{
		public string Name = string.Empty;
		public string Type = string.Empty;
        		
		public string Scope = string.Empty;
		public bool Static = false;        
		public bool ReadOnly = false;

		public string Other = string.Empty;
		
		public string HelpText = string.Empty;
		public string Remarks = string.Empty;
		public string Example = "";
		public string SeeAlso = "";

		public string Signature = "";
		public string DeclaringType = "";
        /// <summary>
        /// Determines the actual type that implements this member
        /// - bad naming in hind sight. This maps to DeclaringType.
        /// </summary>
        public string ImplementedType = "";
        
        public override string ToString()
        {
            if (string.IsNullOrEmpty(Name))
                return base.ToString();

            return Name;
        }
    }

    public class MethodParameter
    {
        public string Name = "";
        public string TypeName = "";
        public string ShortTypeName = "";
    }
}
