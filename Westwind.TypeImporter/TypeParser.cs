using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Configuration;
using Mono.Cecil;
using Westwind.Utilities;

namespace Westwind.TypeImporter
{
    public class TypeParser
    {
        public bool WordWrapDocumentation { get; set; } = true;

        public string AssemblyFilename { get; set; }
        public bool ParseXmlDocumentation { get; set; } = true;

        public List<DotnetObject> GetAllTypes(string assemblyPath = null, bool dontParseMethods=false)
        {
            if (assemblyPath == null)
                assemblyPath = AssemblyFilename;

            AssemblyFilename = assemblyPath;

            var typeList = new List<DotnetObject>();

            var assembly = ModuleDefinition.ReadModule(assemblyPath);
            if (assembly == null)
            {
                SetError("Unable to load assembly: " + assemblyPath);
                return null;
            }

            var types = assembly.Types;

            foreach (var type in types)
            {

                var dotnetObject = ParseObject(type, dontParseMethods);
                if(dotnetObject != null)
                    typeList.Add(dotnetObject);
            }


            return typeList;
        }

        public DotnetObject ParseObject(TypeDefinition type, bool dontParseMembers = false )
        {

            if (type.Name.StartsWith("<"))
                return null;

            var dotnetObject = new DotnetObject
            {
                Name = type.Name,
                RawTypeName = type.Name,
                Assembly = type.Module.Assembly.Name.Name,
                TypeDefinition = type
            };

            // *** If we have a generic type strip off
            if (type.HasGenericParameters)
            {
                dotnetObject.Name = DotnetObject.GetGenericTypeName(type, GenericTypeNameFormats.TypeName);
            }

            dotnetObject.FormattedName = FixupStringTypeName(dotnetObject.Name);

            // *** Parse basic type features
            if (type.IsPublic || type.IsNestedPublic)
                dotnetObject.Scope = "public";
            else if (type.IsNestedFamilyOrAssembly || type.IsNestedFamily)
            {
                dotnetObject.Scope = "internal";
                dotnetObject.Internal = true;
            }
            else if (type.IsNotPublic || type.IsNestedPrivate)
                dotnetObject.Scope = "private";

            if (type.IsSealed && type.IsAbstract)
            {
                dotnetObject.Other = "static";
            }
            else
            {
                if (type.IsSealed && !type.IsEnum)
                   dotnetObject.Other = "sealed";

                if (type.IsAbstract && !type.IsInterface)
                    dotnetObject.Other += "abstract";
            }

            dotnetObject.Namespace = type.Namespace;
            dotnetObject.Signature = type.FullName;

            dotnetObject.Type = "class";
            if (type.IsInterface)
                dotnetObject.Type = "interface";
            else if (type.IsEnum)
                dotnetObject.Type = "enum";
            else if (type.IsValueType)
                dotnetObject.Type = "struct";

            if (type.BaseType != null)
            {
                string baseTypeName = null;
                if (type.BaseType.HasGenericParameters || type.BaseType.IsGenericInstance)
                   baseTypeName = DotnetObject.GetGenericTypeName(type.BaseType, GenericTypeNameFormats.TypeName);
                else
                    baseTypeName = type.BaseType.Name;

                if (baseTypeName == "Object" || baseTypeName == "Enum" || baseTypeName == "Delegate")
                    dotnetObject.InheritsFrom = null;
                else
                    dotnetObject.InheritsFrom = FixupStringTypeName(baseTypeName);


                if (dotnetObject.InheritsFrom == "MulticastDelegate" || dotnetObject.InheritsFrom == "Delegate")
                    dotnetObject.Type = "delegate";

                var implentations = type.Interfaces; //.GetInterfaces();

                if (implentations != null)
                {
                    foreach (var implementation in implentations)
                    {
                        // *** This will work AS LONG AS THE INTERFACE HAS AT LEAST ONE MEMBER!
                        // *** This will give not show an 'empty' placeholder interface
                        // *** Can't figure out a better way to do this...
                        //InterfaceMapping im = type.GetInterfaceMap(implementation);
                        //if (im.TargetMethods.Length > 0 && im.TargetMethods[0].DeclaringType == type)
                        //{
                        if (implementation.InterfaceType.IsGenericInstance)
                            dotnetObject.Implements += DotnetObject.GetGenericTypeName(implementation.InterfaceType, GenericTypeNameFormats.TypeName) + ",";
                        else
                            dotnetObject.Implements += implementation.InterfaceType.Name + ",";
                        //}
                    }
                    if (!string.IsNullOrEmpty(dotnetObject.Implements))
                        dotnetObject.Implements = dotnetObject.Implements.TrimEnd(',');
                }


                // *** Create the Inheritance Tree
                List<string> Tree = new List<string>();
                var current = type;
                while (current != null)
                {
                    if (current.IsGenericInstance)
                        Tree.Add(FixupStringTypeName(DotnetObject.GetGenericTypeName(current, GenericTypeNameFormats.FullTypeName)));
                    else
                        Tree.Add(current.FullName);

                    current = current.BaseType as TypeDefinition;
                }

                // *** Walk our list backwards to build the string
                for (int z = Tree.Count - 1; z >= 0; z--)
                {
                    dotnetObject.InheritanceTree += Tree[z] + "\r\n";
                }


            }

            dotnetObject.Syntax = $"{dotnetObject.Scope} {dotnetObject.Other} {dotnetObject.Type} {dotnetObject.Name}"
                .Replace("   ", " ")
                .Replace("  ", " ");

            if (!string.IsNullOrEmpty(dotnetObject.InheritsFrom))
                dotnetObject.Syntax += " : " + dotnetObject.InheritsFrom;

            dotnetObject.Assembly = type.Module.FileName;
            dotnetObject.Assembly = Path.GetFileName(dotnetObject.Assembly);

            if (!dontParseMembers)
            {
                ParseMethods(dotnetObject);
                ParseFields(dotnetObject);
                ParseProperties(dotnetObject);
                ParseEvents(dotnetObject);
            }

            if (ParseXmlDocumentation)
            {
                var docFile = Path.ChangeExtension(AssemblyFilename, "xml");
                if (File.Exists(docFile))
                {
                    var parser = new XmlDocumentationParser(docFile);
                    if (!parser.ParseXmlProperties(dotnetObject))
                    {
                        SetError(parser.ErrorMessage);
                    }
                }
            }

            return dotnetObject;
        }

        /// <summary>
        /// Parses methods into the dotnet object passed in.
        /// Expects methods to be empty
        /// </summary>
        /// <param name="dotnetObject"></param>
        public void ParseMethods(DotnetObject dotnetObject)
        {
            var dotnetType = dotnetObject.TypeDefinition;

            var methodList = new List<ObjectMethod>();

            foreach (var mi in dotnetType.Methods)
            {
                var meth = new ObjectMethod();
                var miRef = mi.GetElementMethod();


                meth.Name = mi.Name;
                if (meth.Name.StartsWith("<") || mi.IsGetter || mi.IsSetter || mi.IsAddOn || mi.IsRemoveOn)
                    continue;

                if (mi.IsConstructor)
                {
                    if (mi.IsStatic)
                        continue; // don't document static constructors

                    meth.IsConstructor = mi.IsConstructor;
                    meth.Name = dotnetObject.Name;
                }

                if (mi.IsPublic)
                    meth.Scope = "public";
                // TODO: Internal Protected needs to be addressed
                else if (mi.IsAssembly || mi.IsFamilyOrAssembly)
                {
                    meth.Scope = "internal";
                    meth.Internal = true;
                }
                else if (mi.IsPrivate)
                    meth.Scope = "private";

                if (mi.IsAbstract)
                    meth.Other += "abstract ";

                if (mi.IsVirtual && !mi.IsAbstract)
                    meth.Other += "virtual ";

                if (mi.IsStatic)
                {
                    meth.Static = mi.IsStatic;
                    meth.Other += "static ";
                }
                if (mi.IsFinal)
                    meth.Other += "sealed ";

                if (mi.HasGenericParameters || mi.ContainsGenericParameter)
                {
                    meth.GenericParameters = "<";
                    var genericParms = miRef.GenericParameters;
                    foreach (var genericArg in genericParms)
                    {
                        meth.GenericParameters += genericArg.Name + ",";
                    }
                    meth.GenericParameters = meth.GenericParameters.TrimEnd(',');
                    meth.GenericParameters += ">";
                    if (meth.GenericParameters == "<>")
                        meth.GenericParameters = "";
                }

                foreach (var parm in mi.Parameters)
                {
                    var methodParm = new MethodParameter();
                    methodParm.Name = parm.Name;
                    if (methodParm.Name.EndsWith("&"))
                    {
                        methodParm.Other = "ref ";
                        methodParm.Name = methodParm.Name.TrimEnd('&');
                    }
                    methodParm.ShortTypeName = FixupStringTypeName(parm.ParameterType.Name);
                    if(parm.ParameterType.IsGenericInstance)
                        methodParm.ShortTypeName = DotnetObject.GetGenericTypeName(parm.ParameterType.GetElementType(),GenericTypeNameFormats.TypeName);


                    methodParm.Type = parm.ParameterType.FullName;


                    meth.ParameterList.Add(methodParm);
                }

                meth.ReturnType = mi.ReturnType.FullName;


                meth.Syntax = dotnetObject.Scope + " " + dotnetObject.Other  + " " + meth.Name +
                              meth.GenericParameters + "(";
                foreach (var parm in meth.ParameterList)
                {
                    meth.Syntax += $"{parm.ShortTypeName} {parm.Name}, ";
                }

                meth.Syntax = meth.Syntax.TrimEnd(' ',',') + ")";
                meth.Syntax = meth.Syntax.Replace("   ", " ").Replace("  ", " ");

                if (meth.IsConstructor)
                    meth.Signature = mi.DeclaringType.FullName + ".#ctor";

                string parameters = "";
                string rawParameters = "";



                meth.Signature = mi.FullName;

                // strip off return type
                var idx = meth.Signature.IndexOf(' ');
                if (idx > -1)
                    meth.Signature = meth.Signature.Substring(idx).TrimStart();

                // fix up ctor
                meth.Signature = meth.Signature.Replace("::.ctor", ".#ctor");

                // fix up object member syntax and double conversions
                meth.Signature = meth.Signature
                     .Replace("::",".")
                     .Replace("..",".")
                     .Trim();

                // fix up parameters
                if (meth.Signature.EndsWith("()"))
                    // no parms has no parens ie. .method
                    meth.Signature = meth.Signature.Substring(0, meth.Signature.Length - 2);
                else
                {
                    // fix up parameters for generics
                    // from:  .method(System.Collections.Generic.IDictionary`2(System.String,System.Object),System.String)
                    // to:    .method(System.Collections.Generic.IDictionary{System.String,System.Object},System.String)
                    var origParms = StringUtils.ExtractString(meth.Signature, "(", ")",returnDelimiters:true);
                    var newParms = origParms;
                    if (origParms.Contains("`"))
                    {
                        var regEx = new Regex("`.*?<(.*?)>");
                        var matches = regEx.Matches(meth.Signature);
                        foreach (Match match in matches)
                        {
                            var orig = match.Value;
                            var type = match.Groups[1].Value;
                            newParms = newParms.Replace(orig, "{" + type + "}");
                        }
                    }
                    if (!newParms.Equals(origParms))
                        meth.Signature =  meth.Signature.Replace(origParms, newParms);
                }

                methodList.Add(meth);
            }

            dotnetObject.AllMethods.AddRange(methodList);
        }

        /// <summary>
        /// Parses all properties for the type passed
        /// </summary>
        /// <param name="dotnetObject"></param>
        public void ParseProperties(DotnetObject dotnetObject)
        {
            var dotnetType = dotnetObject.TypeDefinition;
            //var dotnetRef = dotnetType.GetElementType();

            var propertyList = new List<ObjectProperty>();
            foreach (var pi in dotnetType.Properties)
            {
                var piRef = pi.PropertyType;

                var prop = new ObjectProperty();
                prop.Name = prop.Name =FixupStringTypeName(pi.PropertyType.Name);

                prop.PropertyMode = PropertyModes.Property;


                MethodDefinition mi;
                if (pi.GetMethod != null)
                    mi = pi.GetMethod;
                else
                    mi = pi.SetMethod;

                if (mi.IsAbstract)
                    prop.Other += "abstract ";

                if (mi.IsVirtual && !mi.IsAbstract)
                    prop.Other += "virtual ";

                if (mi.IsStatic)
                {
                    prop.Static = mi.IsStatic;
                    prop.Other += "static ";
                }
                if (mi.IsFinal)
                    prop.Other += "sealed ";

                if(pi.SetMethod != null && pi.GetMethod != null)
                { }
                else if (pi.SetMethod == null)
                {
                    prop.Other += "readonly ";
                }
                else if (pi.GetMethod == null)
                    prop.Other += "writeonly ";

                if (!string.IsNullOrEmpty(prop.Other))
                    prop.Other = prop.Other.Trim();

                // *** Parse basic type features
                if (mi.IsPublic)
                    prop.Scope = "public";

                // TODO: Internal Protected needs to be addressed
                else if (mi.IsAssembly || mi.IsFamilyOrAssembly)
                {
                    prop.Scope = "internal";
                    prop.Internal = true;
                }
                else if (mi.IsPrivate)
                    prop.Scope = "private";

                if (!piRef.IsGenericInstance)
                    prop.Type = FixupStringTypeName( pi.PropertyType.Name);
                else
                    prop.Type = DotnetObject.GetGenericTypeName(pi.PropertyType, GenericTypeNameFormats.TypeName);

                prop.Syntax = $"{prop.Scope} {prop.Other} {prop.Type} {prop.Name}" + prop.Name;
                prop.Syntax = prop.Syntax.Replace("  ", " ");
                prop.Signature = FixupMemberNameForSignature(mi.FullName);
                prop.DeclaringType = pi.DeclaringType.FullName;

                propertyList.Add(prop);
            }

            dotnetObject.Properties.AddRange(propertyList);
        }

        /// <summary>
        /// Parses all fields for the type passed
        /// </summary>
        /// <param name="dotnetObject"></param>
        public void ParseFields(DotnetObject dotnetObject)
        {
            var dotnetType = dotnetObject.TypeDefinition;
            //var dotnetRef = dotnetType.GetElementType();

            var propertyList = new List<ObjectProperty>();
            foreach (var pi in dotnetType.Fields)
            {
                if (pi.Name.StartsWith("<"))
                    continue;

                var piRef = pi.FieldType;

                var prop = new ObjectProperty();
                prop.Name = FixupStringTypeName(pi.FieldType.Name);
                prop.PropertyMode = PropertyModes.Field;

                if (pi.IsStatic)
                {
                    prop.Static = pi.IsStatic;
                    prop.Other += "static ";
                }

                if (!string.IsNullOrEmpty(prop.Other))
                    prop.Other = prop.Other.Trim();

                // *** Parse basic type features
                if (pi.IsPublic)
                    prop.Scope = "public";

                // TODO: Internal Protected needs to be addressed
                else if (pi.IsAssembly || pi.IsFamilyOrAssembly)
                {
                    prop.Scope = "internal";
                    prop.Internal = true;
                }
                else if (pi.IsPrivate)
                    prop.Scope = "private";

                if (!piRef.IsGenericInstance)
                    prop.Type = FixupStringTypeName( pi.Name);
                else
                    prop.Type = DotnetObject.GetGenericTypeName(pi.FieldType, GenericTypeNameFormats.TypeName);

                prop.Syntax = $"{prop.Scope} {prop.Other} {prop.Type} {prop.Name}" + prop.Name;
                prop.Syntax = prop.Syntax.Replace("  ", " ");
                prop.Signature = prop.Signature = FixupMemberNameForSignature(pi.FullName);
                prop.DeclaringType = pi.DeclaringType.FullName;

                propertyList.Add(prop);
            }

            dotnetObject.Properties.AddRange(propertyList);
        }

        /// <summary>
        /// Parses all properties for the type passed
        /// </summary>
        /// <param name="dotnetObject"></param>
        public void ParseEvents(DotnetObject dotnetObject)
        {
            var dotnetType = dotnetObject.TypeDefinition;
            //var dotnetRef = dotnetType.GetElementType();

            var propertyList = new List<ObjectEvent>();
            foreach (var ei in dotnetType.Events)
            {
                if (ei.Name.StartsWith("<"))
                    continue;

                var eiRef = ei.EventType;

                var eventObject = new ObjectEvent();
                eventObject.Name = ei.Name;


                eventObject.Type = ei.EventType.Name;
                eventObject.Scope = "public";
                eventObject.Signature = FixupMemberNameForSignature(ei.FullName);
                eventObject.DeclaringType = ei.DeclaringType.FullName;

                eventObject.Syntax = $"{eventObject.Scope} {eventObject.Other} {eventObject.Type} {eventObject.Name}";
                eventObject.Syntax = eventObject.Syntax.Replace("  ", " ");

                propertyList.Add(eventObject);
            }

            dotnetObject.Events.AddRange(propertyList);
        }

        /// <summary>
        /// Pass in a Cecil full member name
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        string FixupMemberNameForSignature(string fullName)
        {
            fullName = fullName.Replace("::", ".");

            var idx = fullName.IndexOf(' ');
            if (idx > -1)
                fullName = fullName.Substring(idx).Trim();

            return fullName;
        }


        /// <summary>
        /// Converts a CLR typename to VB or C# type name. Type must passed in as plain name
        /// not in full system format.
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="Language"></param>
        /// <returns></returns>
        public static string FixupStringTypeName(string typeName)
        {
            switch (typeName)
            {
                case "String":
                    return "string";
                case "Boolean":
                    return "bool";
                case "Object":
                    return "object";
                case "Object[]":
                    return "object[]";
                case "Int32":
                    return "int";
                case "Int64":
                    return "long";
                case "Int16":
                    return "byte";
                case "Decimal":
                    return "decimal";
                case "Double":
                    return "double";
                case "Single":
                    return "float";
                case "Char":
                    return "char";
                case "Void":
                    return "void";
                case "Byte":
                    return "byte";
                case "Byte[]":
                    return "byte[]";
            }

            // *** Nullable types converted to int? or DateTime?
            if (typeName.StartsWith("Nullable"))
            {
                string ValueType = StringUtils.ExtractString(typeName, "<", ">");
                if (!string.IsNullOrEmpty(ValueType))
                    return FixupStringTypeName(ValueType) + "?";
            }

            return typeName;
        }




        /// <summary>
        /// Converts a CLR typename to VB or C# type name. Type must passed in as plain name
        /// not in full system format.
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="Language"></param>
        /// <returns></returns>
        public static  string FixupTypename(TypeDefinition type)
        {
            var typeName = type.Name;

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
                return ValueType;
            }

            if (type.IsGenericInstance || type.IsGenericParameter)
                return DotnetObject.GetGenericTypeName(type.GetElementType(), GenericTypeNameFormats.TypeName);

            return typeName;
        }

        public string ErrorMessage { get; set; }

        protected void SetError()
        {
            SetError("CLEAR");
        }

        protected void SetError(string message)
        {
            if (message == null || message == "CLEAR")
            {
                this.ErrorMessage = string.Empty;
                return;
            }
            this.ErrorMessage += message;
        }

        protected void SetError(Exception ex, bool checkInner = false)
        {
            if (ex == null)
                this.ErrorMessage = string.Empty;

            Exception e = ex;
            if (checkInner)
                e = e.GetBaseException();

            ErrorMessage = e.Message;
        }

    }
}
