using MarkdownMonster;

namespace DocMonster.MarkdownParser;

/// <summary>
/// Handle MathText and MathML in the document using $$ for block operations and $ for inline
/// Math expressions
/// </summary>
public class MathRenderExtension : IMarkdownRenderExtension
{
    public string Name { get; set; } = "MathRenderExtension";

    public void BeforeMarkdownRendered(ModifyMarkdownArguments args)
    {
    }


    /// <summary>
    /// No content is added by this extension - it's all handled via script header and javascript events
    /// </summary>
    /// <param name="args"></param>
    public void AfterMarkdownRendered(ModifyHtmlAndHeadersArguments args)
    {
      
        if (mmApp.Configuration.Markdown.UseMathematics &&
            (args.Html.Contains(" class=\"math\"") || args.Markdown.Contains("useMath: true")))
            args.HeadersToEmbed = MathJaxScript;
    }


    /// <summary>
    /// After HTML has been rendered we need to make sure that
    /// script is rendered into the header.
    /// </summary>
    /// <param name="args"></param>
    public void AfterDocumentRendered(ModifyHtmlArguments args)
    {
            

    }

    public const string MathJaxScript = """
                                        <script>
                                        MathJax = {
                                          startup: {
                                            ready: function () {
                                              MathJax.startup.defaultReady();
                                              const toMML = MathJax.startup.toMML;      
                                              MathJax.startup.output.postFilters.add((args) => {
                                                const math = args.math, node = args.data;
                                                const original = (math.math ? math.math :
                                                                  math.inputJax.processStrings ? '' : math.start.node.outerHTML);
                                                node.setAttribute('data-original', original);
                                                node.setAttribute('data-mathml', toMML(math.root).replace(/\n\s*/g, ''));
                                              });
                                            }
                                          }
                                        };
                                        // refresh when the document is refreshed via code
                                        document.addEventListener('previewUpdated',function() {
                                           setTimeout(function() {
                                             MathJax.typeset(); 
                                           },10);
                                        });
                                        </script>
                                        <script id="MathJax-script" async src="https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-chtml.js"></script>
                                        """;
//        public const string MathJaxScript = @"
//<script type=""text/x-mathjax-config"">
//    // enable inline parsing with single $ instead of /
//    MathJax.Hub.Config({
//        tex2jax: {
//            //inlineMath: [['$','$'],['\\(','\\)']],
//            //displayMath: [ ['$$','$$'], ['\\[','\\]'] ],
//            processEscapes: true
//        },
//        //asciimath2jax: {
//        //    delimiters: [['`','`']]
//        //},
//        TeX: {
//            extensions: ['autoload-all.js']
//        }
//    });

//    // refresh when the document is refreshed via code
//    $(document).on('previewUpdated',function() {
//        setTimeout(function() {
//            MathJax.Hub.Queue(['Typeset',MathJax.Hub,'#MainContent']);
//        },10);
//    });
//</script>
//<style>
//    span.math span.MJXc-display {
//        display: inline-block;
//    }
//</style>
//<!-- <script src=""https://cdnjs.cloudflare.com/ajax/libs/mathjax/2.7.9/latest.js?config=TeX-MML-AM_CHTML"" async></script> -->
// <script src=""https://polyfill.io/v3/polyfill.min.js?features=es6""></script>
//  <script id=""MathJax-script"" async  src=""https://cdn.jsdelivr.net/npm/mathjax@3.0.1/es5/tex-mml-chtml.js"">
//";
        
}
