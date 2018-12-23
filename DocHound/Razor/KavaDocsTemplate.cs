using DocHound.Configuration;
using DocHound.Model;
using Westwind.RazorHosting;

namespace DocHound.Razor
{
    public class KavaDocsTemplate : RazorTemplateFolderHost<DocTopic>

    {
        public DocTopic Topic  { get; set; }
        public DocProject Project { get; set; }
        public KavaDocsConfiguration Configuration { get; set; }

        public TemplateHelpers Helpers { get; set; }


        public override void InitializeTemplate(object model, object configurationData)
        {
            Topic = model as DocTopic;
            Project = Topic?.Project;
            Configuration = KavaDocsConfiguration.Current;
            Layout = "_Layout.cshtml";

            Helpers = new TemplateHelpers(Topic,(RazorTemplateBase) this);
                        
            base.InitializeTemplate(model, configurationData);
        }

     }

    public class KavaDocsStringTemplate : RazorTemplateBase<DocTopic>
    {
        public DocTopic Topic { get; set; }
        public DocProject Project { get; set; }
        public KavaDocsConfiguration Configuration { get; set; }

        public TemplateHelpers Helpers { get; set; }


        public override void InitializeTemplate(object model, object configurationData)
        {
            Topic = model as DocTopic;
            Project = Topic?.Project;
            Configuration = KavaDocsConfiguration.Current;
            
            Helpers = new TemplateHelpers(Topic, this);

            base.InitializeTemplate(model, configurationData);
        }

    }
}
