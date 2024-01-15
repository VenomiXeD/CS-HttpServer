using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer_x64.Internals
{
    public class JsonCommentAttribute : Attribute
    {
        public string Comment { get; set; }
        public JsonCommentAttribute(string Comment) { this.Comment = Comment; }
    }
    public class Config
    {
        public Config() { }
        /// <summary>
        /// Configures <b>this</b> object to have the specified overriden default values
        /// </summary>
        /// <returns>Returns <b>this</b></returns>
        public virtual Config GetDefaultValues()
        {
            return this;
        }
    }
}
