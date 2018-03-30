using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Dbarone.Lake.Web
{
    /// <summary>
    /// JsonResult with added streaming capabilities
    /// for large responses.
    /// </summary>
    public class JsonStreamingResult : JsonResult
    {
        private IEnumerable data = null;
        private int bufferSize;

        public JsonStreamingResult(IEnumerable data, int bufferSize = 1000)
        {
            this.bufferSize = bufferSize;
            this.data = data;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            HttpResponseBase response = context.HttpContext.Response;
            response.ContentType = "application/json";
            //JsonSerializer serializer = JsonSerializer.Create(new JsonSerializerSettings());

            if (data != null)
            {
                int i = 0;
                JsonTextWriter writer = new JsonTextWriter(response.Output) { Formatting = Formatting.Indented };
                writer.WriteStartArray();
                JsonSerializer serializer = new JsonSerializer();
                
                foreach (object item in data)
                {
                    serializer.Serialize(writer, item);
                    if (i % bufferSize == 0)
                    {
                        response.Flush();
                    }
                    i++;
                }
                writer.WriteEndArray();
                response.Flush();
            }
        }
    }
}



