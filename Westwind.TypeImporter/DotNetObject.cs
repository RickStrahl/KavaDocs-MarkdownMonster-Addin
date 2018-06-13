using System;
using System.Reflection;
using System.Collections.Generic;
using Mono.Cecil;
using Westwind.Utilities;

namespace Westwind.TypeImporter
{

    public class DotnetObject : IComparable
    {
        public string Name;
        public string RawTypeName;
        public string FormattedName;

        public string Scope;
        public bool Internal = false;

        public TypeDefinition TypeDefinition;

        public string Syntax;


        public List<ObjectMethod> Methods = new List<ObjectMethod>();
        public List<ObjectProperty> Properties = new List<ObjectProperty>();
        public List<ObjectEvent> Events = new List<ObjectEvent>();

        

        public string HelpText = "";
        public string Remarks = "";
        public string Example = "";
        public string SeeAlso = "";

        public string Assembly = "";
        public string Namespace = "";
        public string InheritsFrom = "";
        public string InheritanceTree = "";
        public string Implements = "";
        public string Contract = "";        

        /// <summary>
        /// class, interface, enum, delegate
        /// </summary>
        public string Type = "";

        /// <summary>
        /// Full signature of the class - namespace + classname
        /// </summary>
        public string Signature = "";

        /// <summary>
        /// The raw signature returned from Type member
        /// </summary>
        public string RawSignature = "";

        /// <summary>
        /// All class level identifiers except visibility. ie: static override virtual etc.
        /// </summary>
        public string Other = "";

        /// <summary>
        /// Determines whether inherited members are retrieved
        /// </summary>
        public bool RetrieveDeclaredMembersOnly
        {
            get { return _RetrieveDeclaredMembersOnly; }
            set
            {
                if (value)
                    RetrievalFlags = BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                else
                    RetrievalFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

                _RetrieveDeclaredMembersOnly = value;
            }
        }
        bool _RetrieveDeclaredMembersOnly = false;

        /// <summary>
        /// Reflection retrieval flags.
        /// </summary>
        protected BindingFlags RetrievalFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        ///// <summary>
        ///// Loads all methods for a given type into the aMethod array
        ///// </summary>
        ///// <param name="loType">the type to load methods from</param>
        ///// <returns></returns>
        //public int LoadMethods(Type loType)
        //{
        //    if(loType.Name.StartsWith("<>"))
        //        return 0;

        //    MethodInfo[] loMethods = loType.GetMethods(RetrievalFlags);

        //    ConstructorInfo[] loCtors = loType.GetConstructors(RetrievalFlags);

        //    if (loMethods.Length < 1 && loCtors.Length < 1)
        //    {
        //        MethodCount = 0;
        //        return 0;
        //    }

        //    int lnCount = loMethods.Length;
        //    Methods = new ObjectMethod[lnCount + loCtors.Length];


        //    // *** Loop through methods
        //    for (int x = 0; x < lnCount; x++)
        //    {
        //        MethodInfo method = loMethods[x];
        //        Methods[x] = new ObjectMethod();

        //        // *** Skip Property Get and Set methods -- We must check for shorter props
        //        ///				if (loMethod.Name.Substring(0,4) == "set_" ||
        //        ///					loMethod.Name.Substring(0,4) == "get_")
        //        ///				   continue;

        //        // skip obsolete
        //        IList<CustomAttributeData> attrs = CustomAttributeData.GetCustomAttributes(method);
        //        bool isObsolete = false;
        //        foreach (CustomAttributeData attr in attrs)
        //        {
        //            string name = attr.ToString();
        //            if (name.Contains("ObsoleteAttribute("))
        //            {
        //                isObsolete = true;
        //                break;
        //            }
        //        }                
        //        if (isObsolete)
        //            continue;
                

        //        ObjectMethod loM = Methods[x];
        //        loM.Name = method.Name;
        //        if (loM.Name == null)
        //            continue;

        //        MethodCount++;

        //        if (method.IsPublic)
        //            loM.Scope = "public";
        //        else if (method.IsPrivate)
        //            loM.Scope = "private";
        //        else
        //            loM.Scope = "protected";

        //        if (method.IsAssembly || method.IsFamilyOrAssembly)
        //        {
        //            loM.Internal = true;
        //            if (IsVb)
        //                loM.Scope = "Friend";
        //            else
        //                loM.Scope = "internal";
        //        }

        //        if (IsVb)
        //            loM.Scope = StringUtils.ProperCase(loM.Scope);

        //        if (method.IsStatic)
        //        {
        //            if (IsVb)
        //                loM.Other += "Shared|";
        //            else
        //                loM.Other += "static|";
        //            loM.Static = true;
        //        }


        //        if (method.IsAbstract && !loType.IsInterface)
        //        {
        //            if (IsVb)
        //                loM.Other += "MustOverride|";
        //            else
        //                loM.Other += "abstract|";
        //        }
        //        if (method.IsFinal)
        //        {
        //            if (IsVb)
        //                loM.Other += "NotOverridable|";
        //            else
        //                loM.Other += "sealed|";
        //        }
        //        // *** don't want virtual abstract or on Interfaces
        //        if (method.IsVirtual && !method.IsAbstract && !method.IsFinal && !loType.IsInterface)
        //        {
        //            if (IsVb)
        //                loM.Other += "Overridable|";
        //            else
        //                loM.Other += "virtual|";
        //        }

        //        if (method.ContainsGenericParameters)
        //        {
        //            Type[] genericArgs = method.GetGenericArguments();
        //            foreach (Type genericArg in genericArgs)
        //            {
        //                loM.GenericParameters += genericArg.Name + ",";
        //            }
        //            loM.GenericParameters = loM.GenericParameters.TrimEnd(',');
        //        }

        //        var parameterInfos = method.GetParameters();
        //        loM.ParameterCount = parameterInfos.Length;

        //        if (parameterInfos.Length > 0)
        //        {
        //            loM.ParameterList = new string[parameterInfos.Length];
        //            loM.ParametersList2 = new MethodParameter[parameterInfos.Length];
        //        }


        //        string parameters = "";
        //        string rawParameters = "";

        //        for (int y = 0; y < parameterInfos.Length; y++)
        //        {                            
        //            var parameterInfo = parameterInfos[y];
        //            string ParmType = parameterInfo.ParameterType.Name;
        //            bool IsByRef = false;
        //            if (ParmType.EndsWith("&"))
        //            {
        //                ParmType = ParmType.TrimEnd('&');
        //                IsByRef = true;
        //            }

        //            var ParameterType = parameterInfo.ParameterType;

        //            // *** Override name for Generic parms
        //            if (ParameterType.IsGenericType || ParameterType.ContainsGenericParameters)
        //                ParmType = GetGenericTypeName(ParameterType, GenericTypeNameFormats.TypeName);

        //            string Passby = "";
        //            if (IsVb)
        //            {
        //                if (IsByRef)
        //                    Passby = "ByRef ";
        //                else
        //                    Passby = "ByVal ";

        //                parameters = parameters + Passby + parameterInfo.Name + " as " + TypeNameForLanguage(ParmType);
        //            }
        //            else
        //            {
        //                if (IsByRef)
        //                    Passby = "ref ";

        //                parameters = parameters + Passby + TypeNameForLanguage(ParmType) + " " + parameterInfos[y].Name;
        //            }

        //            if (y < parameterInfos.Length - 1)
        //                parameters = parameters + ", ";

        //            // fix up generic parameters
        //            var signatureTypeName = GetSignatureParameterType(ParameterType);

        //            rawParameters = rawParameters + signatureTypeName;

        //            if (y < parameterInfos.Length - 1)
        //                rawParameters = rawParameters + ",";

        //            if (parameterInfo.Name == null)
        //                loM.ParameterList[y] = signatureTypeName;
        //            else
        //                loM.ParameterList[y] = parameterInfo.Name;

        //            loM.ParametersList2[y] = new MethodParameter();
        //            loM.ParametersList2[y].Name = loM.ParameterList[y].TrimEnd('&');
        //            loM.ParametersList2[y].TypeName = signatureTypeName;
        //            loM.ParametersList2[y].ShortTypeName = ParameterType.Name;
        //        }
        //        loM.Parameters = parameters;
        //        loM.RawParameters = rawParameters;


        //        loM.Signature = method.ReflectedType.FullName + "." + method.Name;
        //        if (!string.IsNullOrEmpty(loM.GenericParameters))
        //        {
        //            // *** Add generic signature
        //            loM.Signature += "``" + loM.GenericParameters.Split(',').Length.ToString();
        //            if (IsVb)
        //                loM.Name += "(of " + loM.GenericParameters + ")";
        //            else
        //                loM.Name += "<" + loM.GenericParameters + ">";
        //        }
        //        loM.Signature = loM.Signature + "(" + rawParameters + ")";

        //        loM.DeclaringType = method.ReflectedType.FullName;
        //        loM.ImplementedType = method.DeclaringType.FullName;

        //        string returnTypeName = method.ReturnType.Name;
        //        if (method.ReturnType.IsGenericType)
        //        {
        //            //returnTypeName = GetGenericTypeName(method.ReturnType, GenericTypeNameFormats.TypeName);
        //        }

        //        loM.ReturnType = TypeNameForLanguage(returnTypeName);
        //    }

        //    // *** Constructors
        //    for (int x = 0; x < loCtors.Length; x++)
        //    {
        //        ConstructorInfo loMethod = loCtors[x];
        //        Methods[MethodCount] = new ObjectMethod();

        //        // *** Skip Property Get and Set methods -- We must check for shorter props
        //        ///				if (loMethod.Name.Substring(0,4) == "set_" ||
        //        ///					loMethod.Name.Substring(0,4) == "get_")
        //        ///				   continue;


        //        ObjectMethod loM = Methods[MethodCount];
        //        if (loM.Name == null)
        //            continue;

        //        loM.Constructor = true;

        //        if (IsVb)
        //            loM.Name = "New";
        //        else
        //            loM.Name = "Constructor"; //" Constructor"; "_" + loMethod.Name.Substring(1);  

        //        MethodCount++;

        //        if (loMethod.IsPublic)
        //            loM.Scope = "public";
        //        else if (loMethod.IsPrivate)
        //            loM.Scope = "private";
        //        else
        //            loM.Scope = "protected";

        //        if (IsVb)
        //            loM.Scope = StringUtils.ProperCase(loM.Scope);

        //        if (loMethod.IsStatic)
        //        {
        //            if (IsVb)
        //                loM.Other += "Shared|";
        //            else
        //                loM.Other += "static|";
        //            loM.Static = true;
        //        }
        //        if (loMethod.IsAbstract && !loType.IsInterface)
        //        {
        //            if (IsVb)
        //                loM.Other += "MustOverride|";
        //            else
        //                loM.Other += "abstract|";
        //        }
        //        if (loMethod.IsFinal)
        //        {
        //            if (IsVb)
        //                loM.Other += "NotOverridable|";
        //            else
        //                loM.Other += "sealed|";
        //        }
        //        if (loMethod.IsVirtual && !loMethod.IsAbstract && !loMethod.IsFinal && !loType.IsInterface)  // don't want virtual abstract
        //        {
        //            if (IsVb)
        //                loM.Other += "Overridable|";
        //            else
        //                loM.Other += "virtual|";
        //        }

        //        if (loMethod.IsGenericMethod)
        //        {
        //            Type[] genericArgs = loMethod.GetGenericArguments();
        //            foreach (Type genericArg in genericArgs)
        //            {
        //                loM.GenericParameters += genericArg.Name + ",";
        //            }
        //            loM.GenericParameters = loM.GenericParameters.TrimEnd(',');
        //        }

        //        ParameterInfo[] loParameters = loMethod.GetParameters();
        //        loM.ParameterCount = loParameters.Length;

        //        if (loParameters.Length > 0)
        //        {
        //            loM.ParameterList = new string[loParameters.Length];
        //            loM.ParametersList2 = new MethodParameter[loParameters.Length];
        //        }
        //        string lcParameters = "";
        //        string lcRawParameters = "";

        //        for (int y = 0; y < loParameters.Length; y++)
        //        {
        //            var parm = loParameters[y];
        //            string ParmType = parm.ParameterType.FullName;
        //            if (ParmType == null)
        //                ParmType = parm.ParameterType.Name;
        //            Type ParameterType = parm.ParameterType;

        //            // *** Override name for Generic parms
        //            if (ParameterType.IsGenericType)
        //                ParmType = GetGenericTypeName(ParameterType, GenericTypeNameFormats.TypeName);

        //            string ParmTypeName = GetSignatureParameterType(ParameterType);                    
        //            //if (ParameterType.IsGenericType)
        //            //    ParmTypeName = GetGenericTypeName(ParameterType,GenericTypeNameFormats.TypeName);
        //            //else if (ParameterType.IsGenericParameter)
        //            //    ParmTypeName = "`" + ParameterType.GenericParameterPosition;
        //            //else
        //            //    ParmTypeName = ParameterType.Namespace + "." + ParameterType.Name;


        //            if (IsVb)
        //                lcParameters = lcParameters + loParameters[y].Name + " as " + TypeNameForLanguage(loParameters[y].ParameterType.Name);
        //            else
        //                lcParameters = lcParameters + TypeNameForLanguage(ParmType) + " " + loParameters[y].Name;
                    

        //            if (y < loParameters.Length - 1)
        //                lcParameters = lcParameters + ", ";

        //            lcRawParameters = lcRawParameters + ParmTypeName;
        //            if (y < loParameters.Length - 1)
        //                lcRawParameters = lcRawParameters + ",";

        //            loM.ParameterList[y] = loParameters[y].Name;
        //        }
        //        loM.Parameters = lcParameters;
        //        loM.RawParameters = lcRawParameters;

        //        loM.Signature = loMethod.ReflectedType.FullName + ".#" + loMethod.Name.Replace(".", "");
        //        loM.Signature = loM.Signature + "(" + lcRawParameters + ")";

        //        loM.DeclaringType = loMethod.ReflectedType.FullName;
        //        loM.ImplementedType = loMethod.DeclaringType.FullName;

        //        loM.ReturnType = "";
        //    }

        //    return MethodCount;
        //}


       

        ///// <summary>
        ///// Load Events from an object into aEvents property
        ///// </summary>
        ///// <param name="type">the type to load from</param>
        ///// <returns></returns>
        //public int LoadEvents(Type type)
        //{
        //    EventInfo[] loFields = type.GetEvents(RetrievalFlags);

        //    int lnCount = loFields.Length;
        //    if (lnCount < 1)
        //        return 0;

        //    EventCount = lnCount;
        //    Events = new ObjectEvent[lnCount];

        //    for (int x = 0; x < loFields.Length; x++)
        //    {
        //        EventInfo foxEvent = loFields[x];
        //        Events[x] = new ObjectEvent();

        //        ObjectEvent loE = Events[x];
        //        loE.Name = foxEvent.Name;
        //        loE.Type = foxEvent.EventHandlerType.Name;


        //        if (!foxEvent.EventHandlerType.IsGenericType)
        //            loE.Type = TypeNameForLanguage(foxEvent.EventHandlerType.Name);
        //        else
        //            loE.Type = GetGenericTypeName(foxEvent.EventHandlerType, GenericTypeNameFormats.TypeName);
                

        //        loE.Scope = "public";
        //        if (IsVb)
        //            loE.Scope = StringUtils.ProperCase(loE.Scope);

        //        loE.Signature = foxEvent.ReflectedType.Namespace + "." + foxEvent.ReflectedType.Name + "." + foxEvent.Name;
        //        loE.DeclaringType = foxEvent.ReflectedType.FullName;
        //        loE.ImplementedType = foxEvent.DeclaringType.FullName;
        //    }

        //    return lnCount;
        //}


        /// <summary>
        /// Converts a CLR typename to VB or C# type name. Type must passed in as plain name
        /// not in full system format.
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="Language"></param>
        /// <returns></returns>
        public static string TypeNameForLanguage(string typeName)
        {
            switch (typeName)
            {
                case "String":
                    return "string";
                case "Boolean":
                    return "bool";
                case "Int32":
                    return "int";
                case "Int64":
                    return "long";
                case "Int16":
                    return "byte";
                case "Decimal":
                    return "decimal";
                case "Object":
                    return "object";
                case "Double":
                    return "double";
                case "Single":
                    return "float";
                case "Char":
                    return "char";
                case "Void":
                    return "void";
            }

            // *** Nullable types converted to int? or DateTime?
            if (typeName.StartsWith("Nullable"))
            {
                string ValueType = StringUtils.ExtractString(typeName, "<", ">");
                if (!string.IsNullOrEmpty(ValueType))
                    return TypeNameForLanguage(ValueType) + "?";
            }

            return typeName;
        }


        /// <summary>
        /// Fixes up a Generic Type name so that it displays properly for output.
        /// Fixes output from wwBusiness`1 to wwBusiness<EntityType>
        /// </summary>
        /// <param name="genericType"></param>
        /// <param name="typeNameFormat"></param>
        /// <returns></returns>
        public static string GetGenericTypeName(TypeReference genericType, GenericTypeNameFormats typeNameFormat)
        {
            string typeName = null;

            int index = genericType.Name.IndexOf("`");
            if (index == -1)
            {
                if (typeNameFormat == GenericTypeNameFormats.TypeName)
                    return genericType.Name;
                if (typeNameFormat == GenericTypeNameFormats.GenericListOnly)
                    return "";

                typeName = genericType.FullName;

                if (string.IsNullOrEmpty(typeName))
                    typeName = genericType.Namespace + "." + genericType.Name;

                return typeName;
            }

            // *** Strip off the Genric postfix
            typeName = genericType.Name.Substring(0, index);
            string FormattedName = typeName;

            // *** Parse the generic type arguments
                        
            string genericOutput = "<";
            bool start = true;

            
            var genericParameters = genericType.GenericParameters;
            if (genericParameters.Count < 1)
                genericParameters = genericType.GetElementType().GenericParameters;

            foreach (var genericArg in genericParameters)
            {
                var name = genericArg.Name;
                if (name.StartsWith("!"))
                    name = name.Replace("!", "T");

                if (start)
                {
                    genericOutput += name;
                    start = false;
                }
                else
                    genericOutput += "," + name;
            }
        
            genericOutput += ">";


            if (typeNameFormat == GenericTypeNameFormats.GenericListOnly)
                return genericOutput;

            FormattedName += genericOutput;

            // *** return the type name plus the generic list 
            if (typeNameFormat == GenericTypeNameFormats.TypeName)
                return FormattedName;


            // *** Add the full namespace
            return genericType.Namespace + "." + FormattedName;
        }



        /// <summary>
        /// Retrieves the type of a parameter and parses out info for 
        /// generic parameter (`0,`1 for generic types).
        /// </summary>
        /// <param name="parameterType"></param>
        /// <returns></returns>
        private static string GetSignatureParameterType(Type parameterType)
        {
            string adjustedTypeName;

            try
            {
                if (parameterType.IsGenericType)
                {
                    string typeName = parameterType.FullName;
                    // GenericType.FullNameGenericType.Namespace + "." + GenericType.Name;

                    if (string.IsNullOrEmpty(typeName))
                        typeName = parameterType.Namespace + "." + parameterType.Name;


                    // Make sure the type is indeed generic in which case the` is in the name
                    int Index = typeName.IndexOf("`");
                    if (Index < 1)
                        adjustedTypeName = typeName;
                    else
                    {
                        // Strip off the Genric postfix
                        typeName = typeName.Substring(0, Index);
                        adjustedTypeName = typeName;

                        // parse the generic type arguments: ie. List<Type> -> List{`1}
                        Type[] genericArguments = parameterType.GetGenericArguments();

                        string genericOutput = "{";
                        foreach (Type GenericArg in genericArguments)
                        {
                            if (GenericArg.IsGenericParameter)
                                genericOutput += "`" + GenericArg.GenericParameterPosition;

                            else if (GenericArg.ContainsGenericParameters)
                            {
                                genericOutput += "`" + "XXX";
                            }
                            else
                                genericOutput += GenericArg.FullName;

                            genericOutput += ",";
                        }

                        genericOutput = genericOutput.TrimEnd(',') + "}";
                        adjustedTypeName += genericOutput;
                    }
                }
                else if (parameterType.IsGenericParameter)
                    adjustedTypeName = "`" + parameterType.GenericParameterPosition;

                // Array generic parameters require special handling
                else if (parameterType.ContainsGenericParameters)
                {
                    if (parameterType.IsArray)
                    {
                        var elementType = parameterType.GetElementType();
                        try
                        {
                            adjustedTypeName = "`" + elementType.GenericParameterPosition + "[]";
                        }
                        catch
                        {
                            // TODO: this fails with byRef and Value types
                            adjustedTypeName = elementType.Name + "[]";
                        }
                    }
                    else if (parameterType.IsByRef)
                        // TODO: how to handle this one?
                        adjustedTypeName = parameterType.Name.Replace("&", "@"); // .FullName - is null
                    else
                        adjustedTypeName = "`" + parameterType.GenericParameterPosition;
                }
                else
                {
                    adjustedTypeName = parameterType.FullName;
                    if (string.IsNullOrEmpty(adjustedTypeName))
                        adjustedTypeName = parameterType.Namespace + "." + parameterType.Name;
                }

                return adjustedTypeName;
            }
            catch (Exception)
            {
                // fall back to returning the value directly - incorrect, but better than failing here!
                adjustedTypeName = parameterType.FullName;
                if (string.IsNullOrEmpty(adjustedTypeName))
                    adjustedTypeName = parameterType.Namespace + "." + parameterType.Name + "|X";

                return adjustedTypeName;
            }

        }

        #region IComparable Members
        public int CompareTo(object obj)
        {
            DotnetObject Comp = (DotnetObject)obj;
            return (Namespace + "|" + Name).CompareTo(Comp.Namespace + "|" + Comp.Name);
            //	return this.Name.CompareTo( Comp.Name );
        }
        #endregion



        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Syntax))
                return Syntax;

            if (!string.IsNullOrEmpty(Name))
                return Name;

            return base.ToString(); 
        }
    }



    /// <summary>
    /// Flags for the GetGenericTypeName function which determines what type
    /// of value is returned.
    /// </summary>
    public enum GenericTypeNameFormats
    {
        TypeName,
        FullTypeName,
        GenericListOnly
    }
}
