// global page reference
var helpBuilder = null;


(function () {
    helpBuilder = {
        basePath: window.page.basePath,
        initializeLayout: initializeLayout,
        initializeTOC: initializeTOC,
        isLocalUrl: isLocalUrl,        
        expandTopic: expandTopic,        
        expandParent: expandParents,
        tocExpandAll: tocExpandAll,
        tocExpandTop: tocExpandTop,
        tocCollapseAll: tocCollapseAll,
        tocClearSearchBox: tocClearSearchBox,
        highlightCode: highlightCode,
        updateDocumentOutline: updateDocumentOutline,
        refreshDocument: refreshDocument,
        configureAceEditor: null // set in aceConfig
    };    
    
    //var tocConfig = {
    //    html: null,
    //    timestamp: new Date()
    //}    

    function initializeLayout(notused) {
        // modes: none/0 - with sidebar,  1 no sidebar
        var mode = getUrlEncodedKey("mode");
        if (mode)
            mode = mode * 1;
        else
            mode = 0;

        // Legacy processing page=TopicId urls to load topic by id
        var topicId = getUrlEncodedKey("topic");
        if (topicId)
            loadTopicAjax(topicId);

        if (!isLocalUrl()){
	        // load internal help links via Ajax
	        $(".page-content").on("click", "a", function (e) {            
	            var href = $(this).attr("href");
	            if (href.startsWith("_")) {                    
	                loadTopicAjax(href);
	                return false; // stop navigation
	            } 
	        });
            
            var id = getIdFromUrl();

            if (id){
                setTimeout(function() {                 
                    $(".toc li .selected").removeClass("selected");

                    var $a = $("#" + id);
                    $a.parent().addClass("selected");
                    if ($a.length > 0)                     {
                        $a[0].scrollIntoView(); 
                        var href = $a.attr("href");
                        loadTopicAjax(href); // new page url
                    }
                },100);
            }
    	}

        if (isLocalUrl() || mode === 1) {
            hideSidebar();                        
        } else {            
            $.get(helpBuilder.basePath + "TableOfContents.html", loadTableOfContents);

            // sidebar or hamburger click handler`  
            $(document.body).on("click", ".sidebar-toggle", toggleSidebar);
            $(document.body).on("dblclick touchend", ".splitter", toggleSidebar);
             
            $(".sidebar-left").resizable({
                handleSelector: ".splitter",
                resizeHeight: false
            });

            // handle back/forward navigation so URL updates
            window.onpopstate = function (event) {
                debugger;
                if (history.state?.URL)
                    loadTopicAjax(history.state.URL);
            }             
            
        }
        setTimeout(function() { 
            helpBuilder.highlightCode();
            CreateHeaderLinks();
        },10);

        setTimeout(function() {
            helpBuilder.refreshDocument();
            scrollSpy();                        
        },10);
    }

    var sidebarTappedTwice = false;
    function toggleSidebar(e) {

        // handle double tap
        if (e.type === "touchend" && !sidebarTappedTwice) {
            sidebarTappedTwice = true;
            setTimeout(function () { sidebarTappedTwice = false; }, 300);
            return false;
        }
        var $sidebar = $(".sidebar-left");
        var oldTrans = $sidebar.css("transition");
        $sidebar.css("transition", "width 0.5s ease-in-out");
        if ($sidebar.width() < 20) {
            $sidebar.show();
            $sidebar.width(400);
        } else {
            $sidebar.width(0);
        }

        setTimeout(function () { $sidebar.css("transition", oldTrans) }, 700);
        return true;
    }

    function loadTopicAjax(href) {       
        var hrefPassed = true;

        if (typeof href != "string") {
            var $a = $(this);
            href = $a.attr("href");
            hrefPassed = false;
            
            $(".toc li .selected").removeClass("selected");
            $a.parent().addClass("selected");   
        }
        
        var $a = get$Link(href);
        if ($a.length > 0)
            expandTopic($a);

        // ajax navigation
        if (href.startsWith("_") || href.startsWith("/")) {
            $.get(href, function (html) {
                var $html = $(html);
                var title = html.extract("<title>", "</title>");
                window.document.title = title;

                var $content = $html.find(".main-content");
                if ($content.length > 0) {
                    html = $content.html();
                    $(".main-content").html(html);

                    // update the navigation history/url in addressbar
                    window.history.pushState({ title: '', URL: href }, "", href);
                    
                    $(".main-content").scrollTop(0);
                } else
                    return;

                var $banner = $html.find(".banner");
                if ($banner.length > 0);
                $(".banner").html($banner.html());
                
                helpBuilder.refreshDocument();

                $(".main-content").scroll(debounce(scrollSpy,100));
                scrollSpy();
            });
            return false;  // don't allow click
        }
        return true;  // pass through click
    }; 
    function get$Link(hrefOrId) {
        var $a = hrefOrId;

        if (typeof $a !== "object") {

            if (hrefOrId.startsWith("/")) {
                //hrefOrId = hrefOrId.replace(".html", "");
                $a = $(".toc li a[href='" + hrefOrId + "']");
            }
            else
                $a = $("#" + hrefOrId.toLowerCase());
        }

        return $a;
    }

    function loadTableOfContents(html) {
        if (!html) {
            hideSidebar();
            return;
        } 

        var $tocContent = $("<div>" + getBodyFromHtmlDocument(html) + "</div>").find(".toc-content");

        $tocContent.find("a").each(function () {       
            var href = $(this).attr("href");
            if (href && !href.startsWith("http") && !href.startsWith("mailto:") && !href.startsWith("javascript:"))
                this.href = helpBuilder.basePath + href;                            

            console.log("toc: " + this.href);
        });
        $tocContent.find("img").each(function () {
            var src = $(this).attr("src");
            if (src && !src.startsWith("http")) 
                this.src = helpBuilder.basePath + src;                            
        });

        $("#toc").html($tocContent.html());
        
        showSidebar();

        // handle AJAX loading of topics        
        $(".toc").on("click", "li a", loadTopicAjax);

        initializeTOC();
        return false;
    }

    function initializeTOC() {
        // Handle clicks on + and -
        $("#toc").on("click",".toc li>i.fa",function () {            
            expandTopic($(this).find("~div a").prop("id") );
        });

        $("#toc").on("click","#SearchBoxClearButton",helpBuilder.tocClearSearchBox);

        // topic selection and expansion of active tree
        setTimeout(()=> {
            tocCollapseAll();  
            
            var topic = getUrlEncodedKey("topic");
            a$ = get$Link(topic);  // getUrlEncodedKey("topic");

            if (a$ && a$.length > 0) {
                var id = a$[0].id;                
                //expandTopic(id);
                expandParents(id);
                //loadTopicAjax(id + ".htm");                
            }
            else 
            {
                var page = window.location.href.extract("://", ".html");
                if (page) {
                    page = page.replace("://", "");
                    var idx = page.indexOf("/");
                    page = page.substr(idx);

                    var a$ = $("[href='" + page + ".html']");
                    if (a$.length > 0)
                        expandParents(a$[0].id);
                }
                else {
                    // expand all root topics
                    $(".toc>li>div>a").each(function () {
                        // default show index tre                
                        expandTopic(this.id);
                    })
                }
            }

        });


        function searchFilterFunc(target) {
            target.each(function () {
                var $a = $(this).find("div>a");             
                if ($a.length > 0) {
                    const url = $a.attr('href');  
                    const id = $a.attr('id');                         
                    if (!url.startsWith("file:") && !url.startsWith("http")) {                        
                        expandParents($a[0].id, true);
                    }
                }

                // keep selected item in the view when removing filter
                setTimeout(function() {
                    var $sel = $(".toc .selected");

                    if ($sel.length > 0)
                        $sel[0].scrollIntoView();    
                },200);
                    
            });
        }

        $("#SearchBox").searchFilter({
            targetSelector: ".toc li",
            charCount: 3,
            onSelected: debounce(searchFilterFunc, 200)
        });
    }

    function hideSidebar() {
        var $sidebar = $(".sidebar-left");
        var $toggle = $(".sidebar-toggle");
        var $splitter = $(".splitter");
        $sidebar.hide();
        $toggle.hide();
        $splitter.hide();   
    }
    function showSidebar() {
        var $sidebar = $(".sidebar-left");
        var $toggle = $(".sidebar-toggle");
        var $splitter = $(".splitter");
        $sidebar.show();
        $toggle.show();
        $splitter.show();
    }
    
    function expandTopic(topicId) {  
        var $href = get$Link(topicId);

        if (!$href || $href.length < 1) return;
      
        var $ul = $href.parent().next();  // div->ul        
        if ($ul.length < 1) return;

        $ul.toggle();

        var $button = $href.parent().prev();

        if ($ul.is(":visible"))
            $button.removeClass("fa-caret-right").addClass("fa-caret-down");
        else
            $button.removeClass("fa-caret-down").addClass("fa-caret-right");
    }

    function expandParents(id, noFocus) {
        if (!id)
            return;

        var $node = $("#" + id);        
        $node.parents("ul").show();

        if (noFocus)
            return;

        var node = $node[0];
        if (!node)
            return;

        $node.parent().addClass("selected");

        //node.scrollIntoView(true);
        node.focus();
        setTimeout(function () {
            window.scrollX = 0;
        });

    }
    function findIdByTopic(topic) {
        if (!topic) {
            var query = window.location.search;
            var match = query.search("topic=");
            if (match < 0)
                return null;
            topic = query.substr(match + 6);
            topic = decodeURIComponent(topic);
        }
        var id = null;
        $("a").each(function () {
            if ($(this).text().toLowerCase() == topic.toLocaleLowerCase()) {
                id = this.id;
                return;
            }
        });
        return id;        
    }

    function tocClearSearchBox() {  
        var val = $("#SearchBox").val();       
        if (!val)
            return;  // already empty
    
        $("#SearchBox").val("");
        clearSearchPane();

        // make all visible
        $(".toc li").show();

        tocExpandAll();
                
        setTimeout(function() {
            // make sure we preserve selection
            var $el = $(".selected");
            var id = '';
            if ($el.length > 0)
                id = $el[0].id;
            
            if (id)
                expandParents(id,false);

            $("#SearchBox")[0].focus();
        },150);
    }
    function clearSearchPane() {
        var $toc = $(".toc.topic-tree")
        var $searchPane = $(".toc.search-results");

        $toc.show();
        $searchPane.hide();
        $searchPane.html('');
    }

    function tocCollapseAll() {
        var $uls = $("ul.toc li ul:visible");        
        $uls.each(function () {            
            var $el = $(this);
            var $href = $el.prev().find("a");
            var id = $href[0].id;
            expandTopic(id);
            
            //setTimeout(()=> expandTopic(id));
        });
    }
    function tocExpandAll() {
        $("ul.toc li ul:not(:visible)").each(function () {           
            var $el = $(this);
            var $href = $el.prev().find("a");
            var id = $href[0].id;
            setTimeout(()=> expandTopic(id));
            //expandTopic(id);
        });
    }
    function tocExpandTopLevel(){
        // expand all root topics
        $(".toc>li>div>a").each(function() {
            // default show index tre                
            expandTopic(this.id);
        })
    }
    function tocExpandTop() {        
        $("ul.toc li ul:not(:visible)").each(function () {
            var $el = $(this);
            var $href = $el.prev().find("a");
            var id = $href[0].id;
            expandTopic(id);
        });
    }
    function isLocalUrl(href) {        
        if (!href)
            href = window.location.href;
        return href.startsWith("mk:@MSITStore") ||
	           href.startsWith("file://")
    }
    function getIdFromUrl(href) {      
  
        if (!href)
            href = window.location.href;

        if (href.startsWith("_"))
            return href.toLowerCase().replace(".htm","").replace("html","");
        
        var id = getUrlEncodedKey("topic");
        if (!id)
            id = getUrlEncodedKey("id");
        if (!id)
            id = getUrlEncodedKey("topicid");        
        if (!id)
            return null;
        
        return id.toLowerCase();
    }
    function mtoParts(address, domain, query) {
        var url = "ma" + "ilto" + ":" + address + "@" + domain;
        if (query)
            url = url + "?" + query;
        return url;
    }

    function highlightCode() {   
        var pres = document.querySelectorAll("pre>code");
        for (var i = 0; i < pres.length; i++) {
            hljs.highlightBlock(pres[i]);
        }

        if (window.highlightJsBadge)
            window.highlightJsBadge();
    }

    function CreateHeaderLinks() {
        var $h3 = $(".content-body>h2,.content-body>h3,.content-body>h4,.content-body>h1");

        $h3.each(function () {            
            var $h3item = $(this);
            $h3item.css("cursor", "pointer");

            var tag = $h3item.text().replace(/\s+/g, "");

            var $a = $("<a />")
	            .attr({
	                name: tag,
	                href: "#" + tag
	            })
	            .addClass('link-icon')
	            .addClass('link-hidden')
                .attr('title', 'click this link and set the bookmark url in the address bar.');

            $h3item.prepend($a);

            $h3item
	            .hover(
	                function () {
	                    $a.removeClass("link-hidden");
	                },
	                function () {
	                    $a.addClass("link-hidden");
	                })
	            .click(function () {
	                window.location = $a.prop("href");
	            });


        });

    }

    function updateDocumentOutline(){
        var navbar$ = $(".topic-outline-content");                       
        navbar$.html("");

        var headers$ = $(".content-pane").find("h1,h2,h3,h4");
        
        if (headers$.length < 2)
        {                   
            $(".content-pane").removeClass("topic-outline-visible");
            $(".topic-outline-header").hide(false);
            return;
        }                
        for (var index = 0; index < headers$.length; index++) {
            var el = headers$[index];
            var id = el.id;
            if (!id) {
                el.id = safeId(el.innerText);
                id = el.id
            }
                
            var space = "";
            if (el.nodeName == "H1")
                space = "outline-level1";
            else if (el.nodeName == "H2")
                space = "outline-level2";
            else if (el.nodeName == "H3")
                space = "outline-level3";
            else if (el.nodeName == "H4")
                space = "outline-level4";
            var a$ = $("<a></a>")
                .prop("href", "#" + id)
                .text(el.innerText);
            if (space)
                a$.addClass(space);

            navbar$.append(a$);
        } 

        $(".content-pane").addClass("topic-outline-visible");
        $(".topic-outline-header").show(true);                    
    }

      /* 
        Updates the document with post-processing scripts.
        Called when page reloads.
    */
        function refreshDocument() {
            helpBuilder.highlightCode();
            
            timeToRead();
            CreateHeaderLinks();
            
            helpBuilder.updateDocumentOutline();            
        }


        function scrollSpy() {        
            var headers$ = $(".topic-outline-content>a");        
            if(headers$.length < 1)
                return;
    
            for (var index = 0; index < headers$.length; index++) {
                const hd$ = $(headers$[index]);
                var id = hd$.attr('href');
    
                var id$;
                try{
                     id$ = $(id);
                }catch(ex) {
                    continue;
                }
                if(id$.length < 1)
                    continue;
    
                if(id$.isInViewport())
                {                
                    $(".topic-outline-content *").removeClass("active");
                    hd$.addClass("active");
                    break;
                }
            }
        }

        $.fn.isInViewport = function() {
            var elementTop = $(this).offset().top;
            var elementBottom = elementTop + $(this).outerHeight();
            var viewportTop = $(window).scrollTop();
            var viewportBottom = viewportTop + $(window).height();
            return elementBottom > viewportTop && elementTop < viewportBottom;
        };  

         /*
     *  timeToRead()
     * 
     *  Time To Read figures writes out the `Time To Read` text at the top
     *  of the document by injecting it into the `#TimeToRead` element at 
     *  the top of content templates.
     * 
     *  This function delegates to `localizedReadingTimeText()` to localize
     *  the text displayed optionally which can be overridden with a custom
     *  global (at window.) `localizedReadingTimeText()` function to provide
     *  additional localizations.
    */    
    function timeToRead() {
        ttr$ = $("#TimeToRead");
        if (ttr$.length == 0)
            return;
        
        var content = $('.content-pane').text();
        
        var regExWords = /\s+/gi;
        var wordCount = content.replace(regExWords, ' ').split(' ').length;     
        var wordsPerMinute = 250;   //  assumed avg reading speed
        
        // figure out minutes to read
        var minutes = 0;
        if (wordCount >= wordsPerMinute)
           minutes = wordCount / wordsPerMinute;           
        minutes = minutes.toFixed(0);
        
        // Language: en, fr, de, es etc. and then localize if possible to browser langauge
        var lang = navigator.language.substr(0,2);        
        var readingTimeText = localizedReadingTimeText(minutes, lang);
        
        if (readingTimeText)
            ttr$.html('<span><i class="fa fa-clock-o"></i> '+ readingTimeText +'</span>');
    }  

})();

if(helpBuilder.isLocalUrl())
    setTimeout(()=> helpBuilder.initializeLayout(),10);

// global functions called from HelpBuilder Dev
function updatedocumentcontent(html,pragmaLine) {
    
    $("body").html(html);
    //helpBuilder.highlightCode();
    helpBuilder.initializeLayout();

    if (typeof pragmaLine === "number" && pragmaLine > 0)
        setTimeout(function() {
            scrolltopragmaline(pragmaLine);
        });       

    
    // refresh syntax coloring and header links
    helpBuilder.refreshDocument();
}

function scrolltopragmaline(lineno) {

    //setTimeout(function() {
        try {

            var $el = $("#pragma-line-" + lineno);
            if ($el.length < 1) {
                var origLine = lineno;
                for (var i = 0; i < 3; i++) {
                    lineno++;
                    $el = $("#pragma-line-" + lineno);
                    if ($el.length > 0)
                        break;
                }
                if ($el.length < 1) {
                    lineno = origLine;
                    for (var i = 0; i < 3; i++) {
                        lineno--;
                        $el = $("#pragma-line-" + lineno);
                        if ($el.length > 0)
                            break;
                    }
                }
                if ($el.length < 1)
                    return;
            }
         
            $el.addClass("line-highlight");
            setTimeout(function() { $el.removeClass("line-highlight"); }, 1200);

            $el[0].scrollIntoView();
            $mc = $(".main-content");                
            $mc[0].scrollTop = $mc[0].scrollTop - 80;                        
        }
        catch(ex) {  
            
        }
    //},20);       
}