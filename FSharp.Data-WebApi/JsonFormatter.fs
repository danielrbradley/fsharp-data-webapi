namespace FSharp.Data.WebApi

open FSharp.Data.Runtime
open FSharp.Data

type JsonFormatter() as formatter = 
    inherit System.Net.Http.Formatting.MediaTypeFormatter()
    do formatter.SupportedMediaTypes.Add(new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"))
    override x.CanReadType t = typeof<IJsonDocument>.IsAssignableFrom(t)
    override x.CanWriteType t = typeof<IJsonDocument>.IsAssignableFrom(t)

    override x.ReadFromStreamAsync(t, stream, contentHeaders, formatterContext) = 
        async { 
            let jsonValue = JsonValue.Load(stream)
            return JsonDocument.Create(jsonValue, "") :> obj
        }
        |> Async.StartAsTask
    
    override x.WriteToStreamAsync(t, value, stream, content, transport) = 
        async { 
            let jsonValue = (value :?> IJsonDocument).JsonValue
            let writer = new System.IO.StreamWriter(stream)
            jsonValue.WriteTo(writer, JsonSaveOptions.DisableFormatting)
            writer.Flush()
            stream.Flush()
        }
        |> Async.StartAsTask :> System.Threading.Tasks.Task
