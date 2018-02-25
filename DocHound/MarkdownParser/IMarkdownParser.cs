﻿namespace DocHound.MarkdownParser
{
    public interface IMarkdownParser
    {
        /// <summary>
        /// Returns parsed markdown
        /// </summary>
        /// <param name="markdown"></param>
        /// <returns></returns>
        string Parse(string markdown);
    }
}