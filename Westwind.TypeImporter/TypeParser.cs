using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Westwind.TypeImporter
{
    public class TypeParser
    {
        public bool WordWrap { get; set; } = true;

        public string AssemblyFilename { get; set; }


        public List<DotnetObject> GetAllTypes(string assemblyPath = null, bool dontParseMethods=false)
        {
            if (assemblyPath == null)
                assemblyPath = AssemblyFilename;

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

            dotnetObject.FormattedName = DotnetObject.TypeNameForLanguage(dotnetObject.Name);

            // *** Parse basic type features
            if (type.IsPublic || type.IsNestedPublic)
                dotnetObject.Scope = "public";
            if (type.IsNestedFamily)
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
            dotnetObject.Signature = type.Namespace + "." + dotnetObject.Name;
            dotnetObject.RawSignature = type.FullName;            

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
                    dotnetObject.InheritsFrom = DotnetObject.TypeNameForLanguage(baseTypeName);


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
                    if (dotnetObject.Implements != "")
                        dotnetObject.Implements = dotnetObject.Implements.TrimEnd(',');
                }


                // *** Create the Inheritance Tree
                List<string> Tree = new List<string>();
                var current = type;
                while (current != null)
                {
                    if (current.IsGenericInstance)
                        Tree.Add(DotnetObject.TypeNameForLanguage(DotnetObject.GetGenericTypeName(current, GenericTypeNameFormats.FullTypeName)));
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

            dotnetObject.Syntax = $"{dotnetObject.Scope} {dotnetObject.Other} {dotnetObject.Type} {dotnetObject.Namespace}.{dotnetObject.Name}"
                .Replace("   ", " ")
                .Replace("  ", " ");

            if (!string.IsNullOrEmpty(dotnetObject.InheritsFrom))
                dotnetObject.Syntax += " : " + dotnetObject.InheritsFrom;

            dotnetObject.Assembly = type.Module.FileName;
            dotnetObject.Assembly = Path.GetFileName(dotnetObject.Assembly);

            if (!dontParseMembers)
            {
                ParseProperties(dotnetObject);
            }

            return dotnetObject;
        }


        public void ParseProperties(DotnetObject dotnetObject)
        {
            var dotnetType = dotnetObject.TypeDefinition;
            var dotnetRef = dotnetType.GetElementType();


            var propertyList = new List<ObjectProperty>();
            foreach (var pi in dotnetType.Properties)
            {
                var piRef = pi.PropertyType;

                var prop = new ObjectProperty();
                
                prop.Name = pi.Name;
                prop.FieldOrProperty = "Property";              

                if(!piRef.IsGenericInstance)
                    prop.Type = DotnetObject.TypeNameForLanguage(pi.PropertyType.Name);
                else
                    prop.Type = DotnetObject.GetGenericTypeName(pi.PropertyType, GenericTypeNameFormats.TypeName);
                
                //prop.Static = piRef.Scope.PropertyType
                
                propertyList.Add(prop);
            }

            dotnetObject.Properties.AddRange(propertyList);            
        }


        public string ErrorMessage { get; set; }

        protected void SetError()
        {
            this.SetError("CLEAR");
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
