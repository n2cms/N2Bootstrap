﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Cassette;
using N2.Web;
using N2.Web.Mvc.Html;
using N2Bootstrap.Library.Cassette.Less;
using dotless.Core;
using dotless.Core.Importers;
using dotless.Core.Parser;
using dotless.Core.Stylizers;

namespace N2Bootstrap.Library.Less
{
    public class ThemedLessEngine
    {
        public static bool IsThemeable(string file)
        {
            var themeDirectory = Url.ResolveTokens("{ThemesUrl}").ToLower();

            if (!file.StartsWith("~"))
            {
                file = "~" + file;
            }

            var isThemeable = file.StartsWith("~" + themeDirectory, StringComparison.InvariantCultureIgnoreCase);
            return isThemeable;
        }

        public static string GetThemedFile(string file, string theme)
        {
            // n2's zip provider doesn't support ../../, so lets try to fix it manually
            var regex = new Regex(@"([^/]*/\.\./)");
            while (regex.IsMatch(file))
            {
                file = regex.Replace(file, "");
            }


            file = file.Replace("\\\\", "\\").Replace("\\", "/");
            
            if (file.StartsWith("/"))
                file = "~" + file;

            if (!IsThemeable(file))
                return file;

            HttpContext.Current.Items["theme"] = theme;
            var themeDirectory = Url.ResolveTokens("{ThemesUrl}").ToLower();
            var themedLocation = file.Replace("~" + themeDirectory, "");
            themedLocation = themedLocation.Substring(themedLocation.IndexOf("/", StringComparison.Ordinal));
            var helper = new UrlHelper(HttpContext.Current.Request.RequestContext);
            var result = helper.ThemedContent(themedLocation);
            if (System.Web.Hosting.HostingEnvironment.VirtualPathProvider.FileExists(result))
            {
                return System.Web.Hosting.HostingEnvironment.VirtualPathProvider.GetFile(result).VirtualPath;
            }
            return result;
        }

        public static CompileResult CompileLess(string file, string contents = null, string theme = null)
        {
            var importedFilePaths = new HashSet<string>();
            var engine = new LessEngine(new Parser(new ConsoleStylizer(), new Importer(importedFilePaths, file, theme)));
            if (string.IsNullOrEmpty(contents))
            {
                using (var sr = new StreamReader(System.Web.Hosting.HostingEnvironment.VirtualPathProvider.GetFile(file).Open()))
                {
                    contents = sr.ReadToEnd();
                }
            }
            var result = engine.TransformToCss(contents, file);
            return new CompileResult(result, importedFilePaths);
        }
    }
}
