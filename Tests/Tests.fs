module Tests

open FSharp.Data
open FSharp.Data.WebApi
open Microsoft.Owin.Testing
open FsUnit
open NUnit.Framework
open Owin
open System
open System.Net
open System.Net.Http
open System.Web.Http
open Swensen.Unquote

type TestModel = JsonProvider< """
    {
        "field": "value"
    }""", RootName = "Test">

[<RoutePrefix("api"); Route("test")>]
type TestController() =
    inherit ApiController()

    member x.Get() =
        TestModel.GetSample()

    member x.Put(model : TestModel.Test) =
        model

let Configuration(app : IAppBuilder) = 
    let config = 
        let config = new HttpConfiguration()
        config.Formatters.Remove config.Formatters.XmlFormatter |> ignore
        config.Formatters.Insert(0, new JsonFormatter())
        config.MapHttpAttributeRoutes()
        config
    app.UseWebApi config |> ignore

let server = TestServer.Create(Action<IAppBuilder>(Configuration))
let client = server.HttpClient

let getStatusCodeAndContent (response : HttpResponseMessage) = async { let! content = response.Content.ReadAsStringAsync
                                                                                          () |> Async.AwaitTask
                                                                       return response.StatusCode, content }
let get (url : string) = async { let! response = client.GetAsync(url) |> Async.AwaitTask
                                 return! response |> getStatusCodeAndContent }

let put (url : string) jsonContent = 
    async { 
        use content = new StringContent(jsonContent, Text.Encoding.UTF8, "application/json")
        let! response = client.PutAsync(url, content) |> Async.AwaitTask
        return! response |> getStatusCodeAndContent
    }

let isSuccess status = (int status) >= 200 && (int status) < 300

let contains substring (content : string) =
    content.Contains(substring)

[<Test>]
let ``can write JSON document``() =
    async {
        let! status, content = get "api/test"
        test <@ status = HttpStatusCode.OK @>
        test <@ content = TestModel.GetSample().JsonValue.ToString(JsonSaveOptions.DisableFormatting) @>
    }
    |> Async.RunSynchronously

[<Test>]
let ``can read JSON document``() =
    async {
        let document = TestModel.Test("another value")
        let! status, content = put "api/test" (document.JsonValue.ToString())
        test <@ status = HttpStatusCode.OK @>
        test <@ content = document.JsonValue.ToString(JsonSaveOptions.DisableFormatting) @>
    }
    |> Async.RunSynchronously
