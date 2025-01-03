using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocHound.Configuration;
using DocHound.Model;

namespace DocHound.Templates
{
    public class TemplateModel
    {

        public DocTopic Topic { get; set; }
        public DocProject Project { get; set; }
        public KavaDocsConfiguration Configuration { get; set; }

        public TemplateHelpers Helpers { get; set; }


        public void InitializeTemplate(DocTopic model, object configurationData)
        {
            Topic = model as DocTopic;
            Project = Topic?.Project;
            Configuration = KavaDocsConfiguration.Current;
            Helpers = new TemplateHelpers(Topic, this);
        }

    }

}
