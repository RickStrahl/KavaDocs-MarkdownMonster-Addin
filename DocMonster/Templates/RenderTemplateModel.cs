using DocMonster.Configuration;
using DocMonster.Model;

namespace DocMonster.Templates;

/// <summary>
/// This is the model that is passed into the Razor template
/// It contains the current topic, project and configuration
/// and a helper instance which are also surfaced directly on
/// the template class instance.
/// </summary>
public class RenderTemplateModel
{

    public RenderTemplateModel(DocTopic topic)
    {
        Topic = topic;
        Project = topic?.Project;
        Configuration = DocMonsterConfiguration.Current;

        Helpers = new TemplateHelpers(this);
    }

    public DocTopic Topic { get; set; }
    public DocProject Project { get; set; }
    public DocMonsterConfiguration Configuration { get; set; }
    public TemplateHelpers Helpers {get; set; }
}
