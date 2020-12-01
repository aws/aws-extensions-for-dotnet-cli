using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.Lambda.Tools
{
    /// <summary>
    /// Class representing ImageTag associated with a image based lambda
    /// </summary>
    public class LambdaImageTagData
    {
        public string Repo { get; set; }
        public string Tag { get; set; }
        /// <summary>
        /// Parse input imagetag to retrieve image repo and image tag
        /// </summary>
        /// <param name="text">Format: repoName:TagName</param>
        public static bool TryParse(string text, out LambdaImageTagData data)
        {
            if (string.IsNullOrEmpty(text))
            {
                data = null;
                return false;
            }
            if (text.Contains(":"))
            {
                var textArray = text.Split(':');
                data = new LambdaImageTagData { Repo = textArray[0], Tag = textArray[1] };
                return true;
            }
            data = new LambdaImageTagData { Repo = text, Tag = null };
            return true;
        }
    }
}