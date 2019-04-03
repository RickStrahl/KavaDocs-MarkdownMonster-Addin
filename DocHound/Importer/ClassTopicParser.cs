using System;
using System.Collections.Generic;
using DocHound.Model;
using Westwind.TypeImporter;

namespace DocHound.Importer
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
            return null;
        }

        public DocTopic ParseMethod(ObjectMethod method, DocTopic parentClassTopic)
        {
            var topic = new DocTopic(_project)
            {
                Title = method.Name,
                Body = method.HelpText,
                DisplayType = "classmethod",

                ClassInfo = new ClassInfo
                {
                    MemberName = method.Name,
                    Signature = method.Signature,
                    Exceptions = method.Exceptions,                    
                    Scope = method.Scope,
                    Static = method.Static,
                    Syntax = method.Syntax

                },

                Remarks = method.Remarks,
                Example = method.Example,
                SeeAlso = method.SeeAlso,

                Parent = parentClassTopic,
                ParentId = parentClassTopic?.Id
            };

            parentClassTopic?.Topics.Add(topic);

            return topic;
        }

        public DocTopic ParseClass(DotnetObject obj, DocTopic parentTopic)
        {
            var topic = new DocTopic(_project)
            {
                Title = obj.FormattedName,
                DisplayType = "classheader",

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


            parentTopic?.Topics.Add(topic);

            return topic;
        }


        public DocTopic ParseNamespace(DocTopic parentTopic)
        {

            return null;
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

            var types = parser.GetAllTypes(assemblyFile);
            if (types == null || types.Count < 1)
            {

                return null;
            }

            foreach (var type in types)
            {
                var topic = ParseClass(type, parentTopic);
                topic.Parent = parentTopic;
                topics.Add(topic);
            }

            if (parentTopic == null)
                parentTopic = new DocTopic();

            parentTopic.Topics = new System.Collections.ObjectModel.ObservableCollection<DocTopic>(topics);

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
