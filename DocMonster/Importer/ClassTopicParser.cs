using System;
using System.Collections.Generic;

using System.Linq;
using System.Windows;
using DocMonster.Model;
using Westwind.TypeImporter;
using Westwind.Utilities;

namespace DocMonster.Importer
{
    /// <summary>
    /// Class used to parse type information into topics by
    /// creating a hierarchy of topics. 
    /// </summary>
    public class TypeTopicParser
    {
        private readonly DocProject _project;
        private readonly DocTopic _parentTopic;

        private TypeParser Parser = new TypeParser();


        public string ClassesToImport { get; set; }

        public bool NoInheritedMembers { get; set; } = true;
        


        public TypeTopicParser(DocProject project, DocTopic parentTopic)
        {
            _project = project;
            _parentTopic = parentTopic;
        }

        public DocTopic ParseProperty(ObjectProperty property, DocTopic parentClassTopic)
        {            
            var topic = new DocTopic(_project)
            {
                Title = parentClassTopic.ClassInfo.Classname + "." + property.Name,                
                //ListTitle = property.Name,                
                //DisplayType = property.PropertyMode == PropertyModes.Field  ? "classfield" : "classproperty",

                
                ClassInfo = new ClassInfo
                {
                    Classname = property.Classname,
                    MemberName = property.Name,
                    Signature = property.Signature,
                    Scope = property.Scope,
                    IsStatic = property.Static,
                    Syntax = property.Syntax                   
                },

                Remarks = property.Remarks,
                Example = property.Example,
                SeeAlso = property.SeeAlso,

                Parent = parentClassTopic,
                ParentId = parentClassTopic?.Id
            };

            if (property.PropertyMode == PropertyModes.Field)
                topic.DisplayType = "classfield";
            else
                topic.DisplayType = "classproperty";

            topic.Parent = parentClassTopic;
            topic.CreateRelativeSlugAndLink(topic);
            topic.Body = property.HelpText;            

            return topic;
        }

        public DocTopic ParseEvent(ObjectEvent ev, DocTopic parentClassTopic)
        {
            var topic = new DocTopic(_project)
            {
                Title = parentClassTopic.ClassInfo.Classname + "." + ev.Name,
                //ListTitle = property.Name,                
                DisplayType = "classevent",

                ClassInfo = new ClassInfo
                {
                    Classname = ev.Classname,
                    MemberName = ev.Name,
                    Signature = ev.Signature,
                    Scope = ev.Scope,
                    IsStatic = ev.Static,
                    Syntax = ev.Syntax, 
                },

                Remarks = ev.Remarks,
                Example = ev.Example,
                SeeAlso = ev.SeeAlso,

                Parent = parentClassTopic,
                ParentId = parentClassTopic?.Id
            };

            topic.Parent = parentClassTopic;
            topic.CreateRelativeSlugAndLink(topic);
            topic.Body = ev.HelpText;

            return topic;
        }


        public DocTopic ParseMethod(ObjectMethod method, DocTopic parentClassTopic)
        {
            var topic = new DocTopic(_project)
            {
                Title = parentClassTopic.ClassInfo.Classname + "." + method.Name,
                //ListTitle = method.Name,                
                DisplayType = method.IsConstructor ? "classconstructor" : "classmethod",

                ClassInfo = new ClassInfo
                {
                    Classname = method.Classname,
                    MemberName = method.IsConstructor ? "Constructor" : method.Name,
                    Signature = method.Signature,
                    Exceptions = method.Exceptions,
                    Scope = method.Scope,
                    IsStatic = method.Static,
                    Syntax = method.Syntax,                    
                    Parameters = method.Parameters,
                    IsConstructor = method.IsConstructor,
                    IsInherited = method.IsInherited,
                },

                Remarks = method.Remarks,
                Example = method.Example,
                SeeAlso = method.SeeAlso,                

                ParentId = parentClassTopic?.Id
            };

            topic.Parent = parentClassTopic;
            topic.CreateRelativeSlugAndLink(topic);
            topic.Body = method.HelpText;

            return topic;
        }

        public DocTopic ParseClass(DotnetObject obj, DocTopic parentTopic, bool dontAddToParent = false)
        {

            string type = "classheader";
            string typeText  = "Class";
            if (obj.Type != "class")
            {
                type = obj.Type;
                typeText = Westwind.Utilities.StringUtils.ProperCase(type);
            }

            var topic = new DocTopic(_project)
            {
                Title = $"{obj.FormattedName} {typeText}",
                //ListTitle = obj.FormattedName,
                DisplayType = type,

                ClassInfo = new ClassInfo
                {
                    MemberName = obj.Name,
                    Signature = obj.Signature,
                    RawSignature = obj.RawTypeName,
                    Namespace = obj.Namespace,
                    Scope = obj.Scope,
                    Syntax = obj.Syntax,
                    Implements = obj.Implements,
                    Inherits = obj.InheritsFrom,
                    InheritanceTree = obj.InheritanceTree,
                    Classname = obj.FormattedName,
                    Assembly = obj.Assembly,
                },

                Remarks = obj.Remarks,
                Example = obj.Example,
                SeeAlso = obj.SeeAlso,
               
            };

            topic.Parent = parentTopic;
            topic.CreateRelativeSlugAndLink(topic);
            topic.Body = obj.HelpText;

            if(!dontAddToParent)
                parentTopic?.Topics.Add(topic);

            
            DocTopic lastTopic = null;
            // Contructors
            foreach (var meth in obj.Constructors)
            {
                
                lastTopic = ParseMethod(meth,topic);
                topic.Topics.Add(lastTopic);
            }
            // Methods
            foreach (var meth in obj.Methods.OrderBy(m=> !m.IsInherited).OrderBy(m=> m.Name))
            {
                lastTopic = ParseMethod(meth, topic);
                topic.Topics.Add(lastTopic);
            }
            // Properties
            foreach (var prop in obj.Properties.OrderBy(m => !m.IsInherited).OrderBy(p=> p.Name))
            {
                lastTopic = ParseProperty(prop, topic);
                topic.Topics.Add(lastTopic);
            }
            // Properties
            foreach (var prop in obj.Fields.OrderBy(m => !m.IsInherited).OrderBy(p => p.Name))
            {
                lastTopic = ParseProperty(prop, topic);
                topic.Topics.Add(lastTopic);
            }
            //foreach (var ev in obj.Events)
            //{
            //    var childTopic = ParseEvent(ev, parentTopic);
            //    parentTopic.Topics.Add(childTopic);
            //}
            topic.SaveTopicFile();
            return topic;
        }


        /// <summary>
        /// Parses an entire assembly
        /// </summary>
        /// <returns></returns>
        public DocTopic ParseAssembly(string assemblyFile, DocTopic parentTopic, bool parseXmlDocs = true)
        {
            var parser = new Westwind.TypeImporter.TypeParser()
            {
                ParseXmlDocumentation = parseXmlDocs,
                NoInheritedMembers = NoInheritedMembers,
                ClassesToImport = ClassesToImport
            };

            var topics = new List<DocTopic>();

            var types = parser.GetAllTypes(assemblyFile); //.OrderBy(t => t.Namespace).OrderBy(t => t.Name);
            if (types == null || types.Count < 1)
            {
                return null;
            }

            types = types.OrderBy(t => t.Namespace).ThenBy(t => t.Name).ToList();

            try
            {
                foreach (var type in types)
                {
                    // TODO: Namespace parsing has to happen HERE to properly handle the folder layout


                    // parse but dont' add to parent topic - we'll add to our list
                    // and rearrange for namespaces
                    var topic = ParseClass(type, parentTopic, true);
                    topic.Parent = parentTopic;
                    topics.Add(topic);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (parentTopic == null)
                parentTopic = new DocTopic();

            // Namespace parsing
            var nsList = new List<DocTopic>();
            var lastNs = string.Empty;  // detect ns change
            foreach (var topic in topics)
            {
                if (lastNs != topic.ClassInfo.Namespace)
                {
                    var ns = new DocTopic(_project)
                    {
                        Title = $"{topic.ClassInfo.Namespace}",
                        DisplayType = "namespace",
                        ClassInfo = new ClassInfo
                        {
                            Assembly = topic.ClassInfo.Assembly,
                        },
                        Parent = parentTopic,                       
                    };
                                       
                    ns.CreateRelativeSlugAndLink(ns);                    
                    lastNs = topic.ClassInfo.Namespace;
                    

                    foreach (var t in topics.Where(c => c.ClassInfo.Namespace == lastNs))
                    {
                        t.Parent = ns;
                        ns.Topics.Add(t);
                    }
                    nsList.Add(ns);
                   
                }
            }

            // now add the top level namespaces
            foreach (var topic in nsList)
            {
                parentTopic.Topics.Add(topic);
            }

            return parentTopic;
        }

        #region

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; }

        protected void SetError()
        {
            SetError("CLEAR");
        }

        /// <summary>
        /// Assign an error message as a string
        /// </summary>
        /// <param name="message"></param>
        protected void SetError(string message)
        {
            if (message == null || message == "CLEAR")
            {
                ErrorMessage = string.Empty;
                return;
            }
            ErrorMessage += message;
        }

        /// <summary>
        /// Assign an error message with an exception
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="checkInner"></param>
        protected void SetError(Exception ex, bool checkInner = true)
        {
            if (ex == null)
                ErrorMessage = string.Empty;

            Exception e = ex;
            if (checkInner)
                e = e.GetBaseException();

            ErrorMessage = e.Message;
        }

        #endregion

    }
}
