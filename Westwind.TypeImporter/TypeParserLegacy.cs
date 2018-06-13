#if false
using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Specialized;

using System.IO;
using System.Text;
using System.Reflection;
using System.Xml;

using System.Runtime.Remoting;

using System.Collections.Generic;
using System.Diagnostics;
using Westwind.Utilities;


namespace Westwind.TypeImporter
{
    /// <summary>
    /// Summary description for DotNetClassInfo.
    /// </summary>
    public class TypeParserLegacy : MarshalByRefObject
    {
        public TypeParserLegacy()
        {
        }

        /// <summary>
        /// Array of parsed objects after GetAllObjects() is called
        /// </summary>
        public DotnetObject[] aObjects;

        /// <summary>
        /// Array of native Type references
        /// </summary>
        public Type[] aXObjects;

        /// <summary>
        /// Number of objects retrieved
        /// </summary>
        public int nObjectCount = 0;

        /// <summary>
        /// Language Syntax to document in
        /// </summary>
        public string cSyntax = "VB";

        /// <summary>
        /// Word Wrapping options for Body and Remark text
        /// </summary>
        public bool bWordWrap = true;

        /// <summary>
        /// If calling GetObject() this returns a single object to parse
        /// </summary>
        public DotnetObject oObject;

        /// <summary>
        /// Constants are not used at this point
        /// </summary>
        public string[] aConstants;
        public int nConstantCount = 0;

        /// <summary>
        /// Error flag - can be checked after method calls
        /// </summary>
        public bool lError = false;

        /// <summary>
        /// Error Message if an error occurred. 
        /// </summary>
        public string cErrorMsg = "";


        /// <summary>
        /// File path to the Assembly to parse
        /// </summary>
        public string Filename = "";

        /// <summary>
        /// Determines whether inherited members are retrieved
        /// </summary>
        public bool RetrieveDeclaredMembersOnly = true;


        /// <summary>
        /// File path to the optional Xml documentation file. 
        /// If empty XML documentation is not used.
        /// </summary>
        public string cXmlFilename = "";


        public string GetVersionInfo()
        {
            return ".NET Version: " + Environment.Version.ToString() + "\r\n" +
            "wwReflection Assembly: " + typeof(TypeParser).Assembly.CodeBase.Replace("file:///","").Replace("/","\\") + "\r\n" +
            "Assembly Cur Dir: " + Directory.GetCurrentDirectory() + "\r\n" +
            "ApplicationBase: " + AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "\r\n" +
            "PrivateBinPath: " + AppDomain.CurrentDomain.SetupInformation.PrivateBinPath + "\r\n" +
            "PrivateBinProbe: " + AppDomain.CurrentDomain.SetupInformation.PrivateBinPathProbe + "\r\n" +
            "App Domain: " + AppDomain.CurrentDomain.FriendlyName + "\r\n";
        }

        

        private string lastAssembly = string.Empty;

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                if (lastAssembly != args.Name)
                {
                    lastAssembly = args.Name;

                    Assembly assembly = Assembly.Load(args.Name);
                    if (assembly != null)
                        return assembly;
                }
            }
            catch { }

            // *** Try to load by filename - split out the filename of the full assembly name
            // *** and append the base path of the original assembly (ie. look in the same dir)
            // *** NOTE: this doesn't account for special search paths but then that never
            //           worked before either.
            string[] Parts = args.Name.Split(',');
            string File = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + Parts[0].Trim() + ".dll";

            Assembly asmbl = Assembly.LoadFrom(File);
            return asmbl;
        }

        /// <summary>
        /// Reflection only callback function to resolve assembly. Required to make ReflectionOnly
        /// work properly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                if (lastAssembly != args.Name)
                {
                    lastAssembly = args.Name;

                    Assembly assembly = Assembly.Load(args.Name);
                    if (assembly != null)
                        return assembly;
                }
            }
            catch { ;}

            // *** Try to load by filename - split out the filename of the full assembly name
            // *** and append the base path of the original assembly (ie. look in the same dir)
            // *** NOTE: this doesn't account for special search paths but then that never
            //           worked before either.
            string[] Parts = args.Name.Split(',');
            string File = Path.GetDirectoryName(Filename) + "\\" + Parts[0].Trim() + ".dll";

            return Assembly.ReflectionOnlyLoadFrom(File);
        }



    
        /// <summary>
        /// Gets all objects into the aObjects array and parses all method/properties into the subarrays.
        /// </summary>
        /// <param name="DontParseMethods">if true methods and properties are not parsed</param>
        /// <returns></returns>
        public int GetAllObjects(bool DontParseMethods)
        {
            lError = false;
            
            Assembly assembly;
            try
            {
                // Load up full assembly
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                assembly = Assembly.LoadFrom(Filename);

                // Load up reflection only assembly - fails with runtime version fixups
                //AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomain_ReflectionOnlyAssemblyResolve;
                //assembly = System.Reflection.Assembly.ReflectionOnlyLoadFrom(this.cFilename);
            }
            catch (Exception ex)
            {
                lError = true;
                cErrorMsg = String.Format("Unable to load assembly.\r\n" +
                                               "Typically this is caused by missing dependent assemblies\r\n" +
                                               "or an assembly compiled using a runtime higher than this version: {0}.\r\n\r\n" +
                                               "Raw Error:\r\n{1}", Environment.Version.ToString(), ex.GetBaseException().Message);
                return 0;
            }

            try
            {
                aXObjects = assembly.GetTypes();                
            }
            catch(Exception ex)
            {
                lError = true;
                cErrorMsg = String.Format("Unable to load types from assembly.\r\n" +
                                                "Typically this is caused by missing dependent assemblies\r\n" +
                                                "or an assembly compiled using a runtime higher than this version: {0}.\r\n\r\n" +
                                                "Raw Error:\r\n{1}", Environment.Version.ToString(), ex.GetBaseException().Message);
          
                return 0;
            }


            try
            {
                int Count = aXObjects.Length;
                if (Count == 0)
                    return 0;

                nObjectCount = Count;
                aObjects = new DotnetObject[Count];


                for (int x = 0; x < aXObjects.Length; x++)
                {
                    Type type = aXObjects[x];
                    aObjects[x] = new DotnetObject();                    
                    ParseObject(aObjects[x], type, DontParseMethods);
                }

                if (!DontParseMethods && cXmlFilename.Length > 0)
                {
                    ParseXmlProperties(null);
                }

                ArrayList al = new ArrayList(aObjects);
                al.Sort();
                aObjects = al.ToArray(typeof (DotnetObject)) as DotnetObject[];

                return Count;
            }
            catch (Exception ex)
            {
                lError = true;
                cErrorMsg = ex.Message + "\r\n\r\n" +
                                 ex.StackTrace + "\r\n";
                return 0;
            }            
        }

        /// <summary>
        /// Retrieves all objects into the aObjects array. Also
        /// parses all the subjects.
        /// </summary>
        /// <returns>Count of objects</returns>
        public int GetAllObjects()
        {
            return GetAllObjects(false);
        }


        /// <summary>
        /// Parses an object by name and sets the oObject 
        /// member with the result.
        /// </summary>
        /// <param name="className">Class name to search</param>
        /// <returns></returns>
        public bool GetObject(string className)
        {
            lError = false;

            bool GenericType = (className.IndexOf("<") > -1);
            string BaseType = className;
            if (GenericType)
                BaseType = className.Substring(0, className.IndexOf("<"));

            if (aObjects == null)
            {
                if (GetAllObjects(true) == 0 && lError)
                    return false;
            }

            for (int x = 0; x < aXObjects.Length; x++)
            {
                Type loType = aXObjects[x];

                // Check for Obsolete members
                IList<CustomAttributeData> attrs = CustomAttributeData.GetCustomAttributes(loType);
                bool isObsolete = false;
                foreach (CustomAttributeData attr in attrs)
                {
                    string name = attr.ToString();
                    if (name.Contains("ObsoleteAttribute("))
                    {
                        isObsolete = true;
                        break;
                    }
                }
                
                // skip over this type
                if (isObsolete)
                    continue;

                // *** Check for matching type or generic type
                if (!(loType.Name.ToLower() == className.ToLower() ||
                      (GenericType && loType.Name.ToLower().StartsWith(BaseType.ToLower() + "`")))
                   )
                    continue;

                oObject = new DotnetObject();
                oObject.Name = loType.Name;
                oObject.RawTypeName = loType.Name;
  
                oObject.Syntax = cSyntax;
                oObject.RetrieveDeclaredMembersOnly = RetrieveDeclaredMembersOnly;

                ParseObject(oObject, loType, false);
            }

            if (cXmlFilename.Length > 0)
                ParseXmlProperties(oObject);

            return true;
        }

        /// <summary>
        /// Internal method that's used to parse the currently selected object
        /// parsing methods and properties and the various object properties including
        /// object inheritance etc.
        /// </summary>
        /// <param name="dotnetObject"></param>
        /// <param name="type"></param>
        /// <param name="dontParseMethods"></param>
        protected void ParseObject(DotnetObject dotnetObject, Type type, bool dontParseMethods)
        {
            // *** Pass down high level options
            dotnetObject.Name = type.Name;
            dotnetObject.RawTypeName = type.Name;
            
            dotnetObject.Syntax = cSyntax;
            dotnetObject.RetrieveDeclaredMembersOnly = RetrieveDeclaredMembersOnly;

            bool IsVb = cSyntax.StartsWith("VB");

            // *** If we have a generic type strip off 
            if (type.IsGenericType)
                dotnetObject.Name = DotnetObject.GetGenericTypeName(type, GenericTypeNameFormats.TypeName); 
            
            dotnetObject.FormattedName = dotnetObject.TypeNameForLanguage(dotnetObject.Name);
            

            // *** Parse basic type features
            if (type.IsPublic || type.IsNestedPublic)
                dotnetObject.Scope = "public";
            if (!type.IsVisible)
            {                
                dotnetObject.Scope = "internal";
                dotnetObject.Internal = true;
            }
            else if (type.IsNotPublic || type.IsNestedPrivate)
                dotnetObject.Scope = "private";

            if (IsVb)
                dotnetObject.Scope = StringUtils.ProperCase(dotnetObject.Scope);

            if (type.IsSealed && type.IsAbstract)
            {
                dotnetObject.Other = "static";
            }
            else
            {
                if (type.IsSealed)
                    if (IsVb)
                        dotnetObject.Other = "NotInheritable";
                    else
                        dotnetObject.Other = "sealed";

                if (type.IsAbstract)
                {
                    if (IsVb)
                        dotnetObject.Other = "MustInherit";
                    else
                        dotnetObject.Other += "abstract";
                }
            }

            // *** Get the type 'signature'
            dotnetObject.Signature = type.FullName;
            dotnetObject.RawSignature = dotnetObject.Signature;

            //// *** Generic Type: Replace signature wwBusiness`1 with wwBusiness<EntityType> (cName)
            //if (loType.IsGenericType)
            //    loObject.cSignature = loObject.cSignature.Substring(0, loObject.cSignature.LastIndexOf(".")+1) + loObject.cName;

            dotnetObject.Namespace = type.Namespace;
            dotnetObject.Assembly = type.Assembly.CodeBase;
            dotnetObject.Assembly = dotnetObject.Assembly.Substring(dotnetObject.Assembly.LastIndexOf("/") + 1);

            dotnetObject.Type = "class";
            if (type.IsInterface)
                dotnetObject.Type = "interface";
            else if (type.IsEnum)
                dotnetObject.Type = "enum";
            else if (type.IsValueType)
                dotnetObject.Type = "struct";

            // *** Figure out the base type and build inheritance tree
            // *** for this class
            if (type.BaseType != null)
            {
                string BaseTypeName = null;
                if (type.BaseType.IsGenericType)
                    //BaseTypeName = DotnetObject.GetGenericTypeName(type.BaseType, GenericTypeNameFormats.TypeName);
                //else
                    BaseTypeName = type.BaseType.Name;

                dotnetObject.InheritsFrom = dotnetObject.TypeNameForLanguage(BaseTypeName);
                
                if (dotnetObject.InheritsFrom == "MulticastDelegate" || dotnetObject.InheritsFrom == "Delegate")
                    dotnetObject.Type = "delegate";

                Type[] Implementations = type.GetInterfaces();

                if (Implementations != null)
                {
                    foreach (Type Implementation in Implementations)
                    {
                        // *** This will work AS LONG AS THE INTERFACE HAS AT LEAST ONE MEMBER!
                        // *** This will give not show an 'empty' placeholder interface
                        // *** Can't figure out a better way to do this...
                        InterfaceMapping im = type.GetInterfaceMap(Implementation);
                        if (im.TargetMethods.Length > 0 && im.TargetMethods[0].DeclaringType == type)
                        {
                            if (Implementation.IsGenericType)
                                dotnetObject.Implements += DotnetObject.GetGenericTypeName(Implementation, GenericTypeNameFormats.TypeName) + ",";
                            else
                                dotnetObject.Implements += Implementation.Name + ",";
                        }
                    }
                    if (dotnetObject.Implements != "")
                        dotnetObject.Implements = dotnetObject.Implements.TrimEnd(',');
                }

                
                // *** Create the Inheritance Tree
                List<string> Tree = new List<string>();
                Type Current = type;
                while (Current != null)
                {
                    if (Current.IsGenericType)
                        Tree.Add(   dotnetObject.TypeNameForLanguage( DotnetObject.GetGenericTypeName(Current,GenericTypeNameFormats.FullTypeName) )  );
                    else
                        Tree.Add(Current.FullName);

                    Current = Current.BaseType;
                }

                // *** Walk our list backwards to build the string
                for (int z = Tree.Count - 1; z >= 0; z--)
                {
                    dotnetObject.InheritanceTree += Tree[z] + "\r\n";
                }
            }

            if (!dontParseMethods)
            {
                //dotnetObject.LoadMethods(type);
                //dotnetObject.LoadProperties(type);
                //dotnetObject.LoadEvents(type);
            }
        }


  

        /// <summary>
        /// This method parses comments from C# XML documentation into the appropriate
        /// cHelpText properties.
        /// </summary>
        /// <returns></returns>
        public bool ParseXmlProperties(DotnetObject PassedObject)
        {
            string xPath;
            DotnetObject Object;

            XmlDocument oDom = new XmlDocument();

            try
            {
                oDom.Load("file://" + cXmlFilename);
            }
            catch (Exception ex)
            {
                cErrorMsg = "Unable to load XML Documentation file." + ex.Message ;
                return false;
            }

            // *** Get object descriptions
            for (int Objs = 0; Objs < nObjectCount; Objs++)
            {
                XmlNode Match = null;
                XmlNode SubMatch = null;

                if (PassedObject == null)
                    Object = aObjects[Objs];
                else
                {
                    Object = PassedObject;
                    Objs = nObjectCount - 1;
                }

                if (PassedObject != null && PassedObject.Name.ToLower() != Object.Name.ToLower())
                    continue;

                try
                {
                    Match = oDom.SelectSingleNode("/doc/members/member[@name='" +
                        "T:" + Object.RawSignature +  "']");

                    Object.HelpText = GetNodeValue(Match, "summary");

                    Object.Remarks = GetNodeValue(Match, "remarks");
                    Object.Example = GetExampleCode(Match);
                    Object.SeeAlso = FixupSeeAlsoLinks(ref Object.HelpText) +
                                      FixupSeeAlsoLinks(ref Object.Remarks); 

                    Object.Contract = GetContracts(Match);

                    Match = null;
                }
                catch (Exception ex)
                {
                    string lcError = ex.Message;
                }

                // *** Loop through the methods
                for (int lnMethods = 0; lnMethods < Object.MethodCount; lnMethods++)
                {
                    ObjectMethod method = Object.Methods[lnMethods];

                    // *** Strip off empty method parens
                    string methodSignature = method.Signature.Replace("()", "");

                    // *** Replace Reference of Reflection for the one use by XML docs 
                    methodSignature = methodSignature.Replace("&", "@");

                    if (method.RawParameters == "")
                        xPath = "/doc/members/member[@name='" +
                            "M:" + methodSignature + "']";  // .cNamespace + "." + loObject.cName + "." + loMethod.cName + "']";
                    else
                        xPath = "/doc/members/member[@name='" +
                            "M:" + methodSignature + "']";

                    //							"M:" + loObject.cNamespace + "." + loObject.cName + "." + loMethod.cName + 
                    //							"(" + loMethod.cRawParameters + ")']";


                    try
                    {
                        Match = oDom.SelectSingleNode(xPath);
                        if (Match == null)
                            continue;
                      
                        method.HelpText = GetNodeValue(Match, "summary");
                        method.Remarks = GetNodeValue(Match, "remarks");
                        method.Example = GetExampleCode(Match);
                        method.Contract = GetContracts(Match);

                        method.SeeAlso = FixupSeeAlsoLinks(ref method.HelpText) +
                                          FixupSeeAlsoLinks(ref method.Remarks);


                        //this.GetNodeValue(Match, "example");
                        
                        // *** Only update returns if the return comment is actually set
                        // *** Otherwise leave the type intact
                        method.ReturnDescription = GetNodeValue(Match, "returns");

                        // *** Parse the parameters
                        string lcParmDescriptions = "";
                        for (int x = 0; x < method.ParameterCount; x++)
                        {
                            string parm = method.ParameterList[x].Trim();
                            lcParmDescriptions = lcParmDescriptions +
                                "**" + parm + "**  \r\n";

                            SubMatch = Match.SelectSingleNode("param[@name='" + parm + "']");
                            if (SubMatch != null)
                            {
                                string lcValue = SubMatch.InnerText;
                                if (lcValue.Length > 0)
                                {
                                    lcValue = lcValue.Trim(' ', '\t', '\r', '\n');
                                    lcParmDescriptions = lcParmDescriptions + lcValue + "\r\n\r\n";
                                }
                                else
                                    // empty line
                                    lcParmDescriptions = lcParmDescriptions + "\r\n";
                            }

                        }
                        method.DescriptiveParameters = lcParmDescriptions;

                        string Exceptions = "";
                        XmlNodeList ExceptionMatches = null;
                        try
                        {
                            ExceptionMatches = Match.SelectNodes("exception");
                        }
                        catch { ;}

                        if (ExceptionMatches != null)
                        {
                            foreach (XmlNode Node in ExceptionMatches)
                            {
                                XmlAttribute Ref = Node.Attributes["cref"];
                                if (Ref != null)
                                    Exceptions += "**" + Ref.Value.Replace("T:", "").Replace("!:","") + "**  \r\n";
                                if (!string.IsNullOrEmpty(Node.InnerText))
                                    Exceptions += GetNodeValue(Node,"") + "\r\n";
                                Exceptions += "\r\n";
                            }
                            method.Exceptions = Exceptions.TrimEnd('\r', '\n');
                        }

                        Match = null;
                    }
                    catch (Exception ex)
                    {
                        string lcError = ex.Message;
                    }
                }  // for methods

                // *** Loop through the properties
                for (int lnFields = 0; lnFields < Object.PropertyCount; lnFields++)
                {
                    ObjectProperty property = Object.Properties[lnFields];

                    string fieldPrefix = "F:";
                    if (property.FieldOrProperty != "Field")
                        fieldPrefix = "P:";

                    xPath = "/doc/members/member[@name='" + fieldPrefix +
                        property.Signature + "']";

                    Match = oDom.SelectSingleNode(xPath);

                    property.HelpText = GetNodeValue(Match, "summary");
                    property.Remarks = GetNodeValue(Match, "remarks");
                    property.Example = GetExampleCode(Match);
                    property.Contract = GetContracts(Match);
                    
                    property.SeeAlso = FixupSeeAlsoLinks(ref property.HelpText) +
                                        FixupSeeAlsoLinks(ref property.Remarks);


                    string value = GetNodeValue(Match, "value");
                    if (!string.IsNullOrEmpty(value))
                        property.DefaultValue = value;

                    Match = null;

                }  // for properties

                // *** Loop through the fields
                for (int lnFields = 0; lnFields < Object.EventCount; lnFields++)
                {
                    ObjectEvent loEvent = Object.Events[lnFields];

                    string lcFieldPrefix = "E:";

                    xPath = "/doc/members/member[@name='" + lcFieldPrefix +
                        loEvent.Signature + "']";

                    Match = oDom.SelectSingleNode(xPath);
                    loEvent.HelpText = GetNodeValue(Match, "summary");
                    loEvent.Remarks = GetNodeValue(Match, "remarks");
                    loEvent.Example = GetExampleCode(Match);

                    loEvent.SeeAlso = FixupSeeAlsoLinks(ref loEvent.HelpText) +
                                       FixupSeeAlsoLinks(ref loEvent.Remarks);
                    
                    Match = null;

                }  // for events

            }  // for objects

            return true;
        } // ParseXMLProperties (method)



        /// <summary>	
        /// Retrieves the text from a child XML node
        /// </summary>
        /// <param name="node">the parent node</param>
        /// <param name="keyword">subnode XPATH expression. Pass empty string for current node</param>
        /// <returns></returns>
        protected string GetNodeValue(XmlNode node, string keyword)
        {
            return GetNodeValue(node, keyword, bWordWrap);
        }

        /// <summary>	
        /// Retrieves the text from a child XML node
        /// </summary>
        /// <param name="node">the parent node</param>
        /// <param name="keyword">subnode XPATH expression. Pass empty string for current node</param>
        /// <returns></returns>
        protected string GetNodeValue(XmlNode node, string keyword, bool wordWrap)
        {
            XmlNode subMatch = null;
            string result = "";

            if (node != null)
            {
                if (string.IsNullOrEmpty(keyword))
                    subMatch = node;
                else
                    subMatch = node.SelectSingleNode(keyword);

                if (subMatch != null)
                {
                    // *** Need to pull inner XML so we can get the sub XML strings ilke <seealso> etc.
                    if (keyword == "example")
                        result = FixHelpString(subMatch.InnerXml.Replace("\r\n","\r"),false);
                    else
                        result = FixHelpString(subMatch.InnerXml.Replace("\r\n","\r"),wordWrap);

                    result = FixupSeeLinks(result);

                    subMatch = null;

                    // *** Deal with embedded list types - suboptimal at best but...
                    if (result.IndexOf("<list type=\"table\"") > -1 && result.IndexOf("<description>") > -1)
                    {
                        /// <list type="table">
                        /// <item>
                        ///		<term>test item</term>
                        ///		<description>aDescription</description>
                        /// </item>
                        /// </list>
                        string list = StringUtils.ExtractString(result, "<list type=\"table\">", "</list>");

                        XmlDocument loDom = new XmlDocument();
                        try
                        {
                            loDom.LoadXml("<list>" + list + "</list>");
                            StringBuilder sbHtml = new StringBuilder();

                            sbHtml.Append("<RAWHTML>\r\n<table border=\"1\" cellpadding=\"3\" width=\"400\">\r\n");

                            XmlNode Header = loDom.DocumentElement.SelectSingleNode("listheader");
                            if (Header != null)
                            {
                                sbHtml.Append("<tr><th>" + Header.SelectSingleNode("term").InnerText);
                                sbHtml.Append("</td><th>" + Header.SelectSingleNode("description").InnerText);
                                sbHtml.Append("</th></tr>\r\n");
                            }
                            XmlNodeList nodeItems = loDom.DocumentElement.SelectNodes("item");
                            foreach (XmlNode Item in nodeItems)
                            {
                                sbHtml.Append("<tr><td>" + Item.SelectSingleNode("term").InnerText);
                                sbHtml.Append("</td><td>" + Item.SelectSingleNode("description").InnerText);
                                sbHtml.Append("</td></tr>\r\n");
                            }
                            sbHtml.Append("</table>\r\n</RAWHTML>\r\n");

                            string output = sbHtml.ToString();
                            int lnAt = result.IndexOf(list);

                            result = StringUtils.ReplaceString(result, "<list type=\"table\">" + list + "</list>", output, true);

                        }
                        catch (Exception e) { string lcError = e.Message; }  /// ignore errors - leave as is
                    }
                }
            }

            // *** Now un-markup what's left of the XML 
            result = result.Replace("&lt;", "<");
            result = result.Replace("&gt;", ">");

            result = result.TrimEnd(new char[] { ' ','\t','\r','\n' });
            
            return result;
        }

        /// <summary>
        /// Returns the Example code snippet either as a single
        /// string or as a collection of code snippets from multiple
        /// code sub items.
        /// </summary>
        /// <param name="match">Pass in the member root node</param>
        /// <returns></returns>
        string GetExampleCode(XmlNode match)
        {
            string code = string.Empty;
            if (match == null)
                return code;

            // Check for <code lang="c#"> tags
            try
            {
                XmlNodeList codeNodes = match.SelectNodes("example/code");
                if (codeNodes.Count > 0)
                {
                    StringBuilder codeSb = new StringBuilder();

                    foreach (XmlNode codeNode in codeNodes)
                    {
                        string lang = codeNode.Attributes["lang"].Value as string;
                        if (lang != null)
                        {
                            codeSb.AppendLine("```" + lang);
                        }
                        string plainCode = FixHelpString(codeNode.InnerXml.Replace("\r\n","\r"), false); 
                        //this.GetNodeValue(codeNode, "", false) ?? string.Empty;
                        
                        codeSb.AppendLine(plainCode.TrimEnd());

                        if (lang != null)
                            codeSb.AppendLine("```");
                    }
                    code = codeSb.ToString();
                    Debug.WriteLine(code);
                }
                else 
                    code = GetNodeValue(match, "example");
            }
            catch 
            {
                code = GetNodeValue(match, "example");            
            }

            // *** Now un-markup what's left of the XML 
            code = code.Replace("&lt;", "<").Replace("&gt;", ">");

            return code;
        }

        /// <summary>
        /// Return code contract information
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        private string GetContracts(XmlNode match, bool isProperty = false)
        {
            if (match == null)
                return string.Empty;
            
            XmlNodeList nodes;

            StringBuilder sb = new StringBuilder(256);
            
            // 0 - not a property, 1 get, 2 set
            int getSet;
            
            var tokens = new string[] { "invariant","ensures","requires","getter/requires","setter/requires","getter/ensures","setter/ensures" };
            foreach(string token in tokens)
            {
                nodes = match.SelectNodes(token);
                if (nodes == null || nodes.Count < 1)
                    continue;

                if (token.StartsWith("getter"))
                    getSet=1;
                else if(token.StartsWith("setter"))
                    getSet=2;
                else
                    getSet = 0;
                            
                string headerText = StringUtils.ProperCase(nodes[0].Name);
                if (headerText == "Invariant")
                    headerText = "Invariants";

                if (getSet == 1)
                    sb.AppendLine("<div class=\"contractgetset\">Get</div>");
                if (getSet == 2)
                    sb.AppendLine("<div class=\"contractgetset\">Set</div>");                

                sb.AppendLine("<div class=\"contractitemheader\">" +  headerText + "</div>");
                foreach (XmlNode node in nodes)
                {
                    string val = node.InnerText;
                    string desc = string.Empty;
                    string exception = string.Empty;
                    XmlAttribute att = node.Attributes["description"];
                    if (att != null)
                        desc = att.InnerText;

                    att = node.Attributes["exception"];
                    if (att != null)
                        exception = att.InnerText;

                    sb.AppendLine(val);
                    if ( !string.IsNullOrEmpty(exception) )
                        sb.AppendLine("*Exception: " + exception + "*");
                    if ( !string.IsNullOrEmpty(desc) )
                        sb.AppendLine("*Description: " + desc + "*");
                
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }


        /// <summary>
        /// Fixes up <see></see> links in various formats.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        protected string FixupSeeLinks(string result)
        {
            int Mode = 0;
            while (result.IndexOf("<see") > -1)
            {
                string lcCaption = "";
                string lcRef = "";

                // *** <see>Class Test</see>
                string lcSee = StringUtils.ExtractString(result, "<see>", "</see>");
                if (lcSee != string.Empty)
                {
                    // plain tag
                    lcRef = lcSee;
                    lcCaption = lcSee;
                    Mode = 0;
                }
                else
                {
                    // *** Full CRef syntax: 
                    lcSee = StringUtils.ExtractString(result, "<see ", "/>", true);
                    if (lcSee != string.Empty)
                    {
                        // <see cref="..." />
                        Mode = 2;
                        lcRef = StringUtils.ExtractString(lcSee, "cref=\"", "\"");
                        if (lcRef != string.Empty)
                        {
                            if (lcRef.IndexOf(":") == 1)
                                lcCaption = lcRef.Substring(2);
                            else
                                lcCaption = lcRef;
                        }
                    }
                    else
                    {
                        lcSee = StringUtils.ExtractString(result, "<see ", "</see>",true);
                        if (lcSee == string.Empty)
                            break;

                        Mode = 1;
                        lcRef = StringUtils.ExtractString(lcSee, "cref=\"", "\"");
                        lcCaption = lcSee.Substring(lcSee.IndexOf(">") + 1);
                    }

                    if (lcCaption == "")
                    {
                        lcCaption = lcRef;
                        if (lcCaption.IndexOf(":") == 1)
                            lcCaption = lcCaption.Substring(2);

                        int At = lcCaption.IndexOf("(");
                        if (At > -1)
                            lcCaption = lcCaption.Substring(0, At);
                    }
                    if (lcRef == "")
                        lcRef = lcCaption; // try to find in Help File by name

#if VSNet_Addin
							// *** Check for .NET References -  point at MSDN
							if ( lcRef.StartsWith("System.") || lcRef.StartsWith("Microsoft.") )
								lcRef = "msdn:T:" + lcRef;
							else 
							{
								lcRef = "sig:" + lcRef;
							}
						
#else
                    // *** Check for .NET References -  point at MSDN
                    if (lcRef.IndexOf("System.") == 2 || lcRef.IndexOf("Microsoft.") == 2)
                        lcRef = "msdn:" + lcRef;
                    else
                    {
                        // *** Nope must be internal links - look up in project.
                        // *** Strip of prefix and add sig: prefix
                        if (lcRef.IndexOf(":") > -1)
                        {
                            // *** Add method brackets on empty methods because that's
                            // *** that's how we are importing them
                            if (lcRef.StartsWith("M:") && lcRef.IndexOf("(") == -1)
                                lcRef += "()";

                            // *** Mark as a Signature link skipping over prefix
                            lcRef = "sig:" + lcRef.Substring(2);
                        }
                        else
                          lcRef = "sig:" + lcRef;
                    }
#endif
                }


                if (Mode == 0)
                    result = result.Replace("<see>" + lcSee + "</see>",
                        "<%=TopicLink(\"" + lcCaption + "\",\"" + lcRef + "\") %>");
                else if (Mode == 1)
                    result = result.Replace( "<see " + lcSee + "</see>",
                        "<%=TopicLink(\"" + lcCaption + "\",\"" + lcRef + "\") %>");
                else if (Mode == 2)
                    result = result.Replace( "<see " + lcSee + "/>",
                        "<%=TopicLink(\"" + lcCaption + "\",\"" + lcRef + "\") %>");

            }

            return result;
        }

        /// <summary>
        /// Extracts seealso sections out of the XML into a CR delimited strings
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        protected string FixupSeeAlsoLinks(ref string body)
        {
            if (body.IndexOf("<seealso") < 0)
                return "";

            if (body.Contains("Remarks duplicated from summary"))
            {
                string x2 = "";
                x2 += body;
            }

            string result = "";
            string extract = "XX";

            while (extract != "")
            {
                extract = StringUtils.ExtractString(body, "<seealso>", "</seealso>");
                if (extract != "")
                {
                    result += extract + "\r\n";
                    body = body.Replace("<seealso>" + extract + "</seealso>", "");
                }
                else
                {
                    // *** retrieve  CREF 
                    extract = StringUtils.ExtractString(body, "<seealso cref=\"", ">", false, false, true);
                    if (extract != "")
                    {
                        // Extract just the class string
                        string cref = StringUtils.ExtractString(extract, "cref=\"", "\"");
                        cref = FixupCRefUrl(cref);

                        // strip out type prefix (T:System.String)
                        string insertText = StringUtils.ExtractString(cref, ":", "xx", false, true);

                        body = body.Replace(extract, "<%= TopicLink(\"" + insertText + "\",\"" + cref + "\") %>");
                        result += cref + "\r\n";
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cref">.NET Signature like T:NameSpace.Class or M:NameSpace.Class.Method(string,string)</param>
        /// <returns></returns>
        string FixupCRefUrl(string cref)
        {
            
            // *** Check for .NET References -  point at MSDN
            if (cref.IndexOf("System.") == 2 || cref.IndexOf("Microsoft.") == 2)
                cref = "msdn:" + cref;
            else
            {
                // *** Nope must be internal links - look up in project.
                // *** Strip of prefix and add sig: prefix
                if (cref.IndexOf(":") > -1)
                {
                    // *** Add method brackets on empty methods because that's
                    // *** that's how we are importing them
                    if (cref.StartsWith("M:") && cref.IndexOf("(") == -1)
                        cref += "()";

                    // *** Mark as a Signature link skipping over prefix
                    cref = "sig:" + cref.Substring(2);
                }
                else
                    cref = "sig:" + cref;
            }
            return cref;
        }

        protected string GetAttributeValue(XmlNode Node, string Keyword, string Attribute)
        {
            XmlNode loSubMatch = null;
            string lcResult = "";

            if (Node != null)
            {
                loSubMatch = Node.SelectSingleNode(Keyword);
                if (loSubMatch != null)
                {
                    lcResult = FixHelpString(loSubMatch.InnerText,bWordWrap);
                    loSubMatch = null;
                }
            }

            return lcResult;
        }


        protected string GetHelpDetails()
        {
            return "";
        }


        /// <summary>
        /// Fixes up a description string by splitting it
        /// into separate lines on Char(13)'s.
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        protected string FixHelpString(string inputString,bool wordWrap)
        {
            string[] strings = inputString.Split(new char[2] {'\r','\n'});
            string output = "";

            ///*** We need to strip padding that XML Help topics add
            ///*** so we look at the first line of non-empty text and figure
            ///*** out the padding applied, then skip over that many leading chars
            ///*** for every line.
            int padding = -1;            
            
            for (int x = 0; x < strings.Length; x++)
            {
                // *** Strip off leading spaces and the \n
                string currentString = strings[x].TrimStart(new char[2] { '\r','\n' }).TrimEnd();       

                // *** Padding is unset until we hit the first line
                if (padding == -1)
                {                    
                    int len = currentString.TrimStart().Length;
                    if (len != currentString.Length)
                        padding = currentString.Length - len;
                    
                    if (padding == -1 && len > 0)
                        padding = 0;
                }

                // *** Don't do anything if padding is -1
                // *** line is empty and no content's been hit yet
                // *** IOW, skip over it                
                if (padding == -1)                
                {
                    // *** In some situations there's no padding on the first line
                    // *** Example: Starts with <para> tag. So just add it.
                    //     If empty don't append.
                    if (!string.IsNullOrEmpty(currentString))
                        output += currentString + "\r\n";

                    // if string is still -1 check for for non-empty
                    // in that case: no padding
                    if (padding == -1 && !string.IsNullOrEmpty(currentString))
                        padding = 0;
                }
                else
                {
                    
                    if (currentString.Length > padding)
                        output += currentString.Substring(padding) + "\r\n";
                    else
                        // *** Empty line
                        output += "\r\n";
                }                
            }

            output = output.Replace("<pre>", "<pre>");
            output = output.Replace("</pre>", "</pre>");

            output = output.Replace("<c>", "<span class=\"code\">");
            output = output.Replace("</c>", "</span>");

            output = output.Replace("<code>", "<pre>");
            output = output.Replace("</code>", "</pre>");

            // *** Pick out <pre> blocks and combine lines to wrap
            if (wordWrap)
            {
                ArrayList al = new ArrayList();

                // remove code blocks and insert back after formatting
                // to avoid encoding of code
                while (true)
                {
                    string Text = StringUtils.ExtractString(output, "<pre>", "</pre>");
                    if (Text == null || Text == "")
                        break;

                    output = output.Replace("<pre>" + Text + "</pre>", "@@!@@");
                    al.Add(Text);
                }

                // *** Try to fix up line feeds - keep only double breaks
                output = output.Replace("\r\n\r\n", "^^^^"); 
                output = output.Replace("\r\n", " ");
                output = output.Replace("^^^^", "\r\n\r\n");

                // *** Replace paragraph tags after the fact
                output = output.Replace("<para>", "\r\n");
                output = output.Replace("</para>", "\r\n");
                output = output.Replace("<para />", "\r\n");
                output = output.Replace("<para/>", "\r\n");

                foreach (string Text in al)
                {
                    output = StringUtils.ReplaceString(output, "@@!@@", "<pre>" + Text + "</pre>", true);
                }
            }

            return output;
        }



    }


}
#endif
