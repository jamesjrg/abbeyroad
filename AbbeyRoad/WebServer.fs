module AbbeyRoad.WebServer

open Suave
open Suave.Http
open Suave.Sockets
open Suave.WebSocket
open Suave.Web
open Suave.Http.Files
open Suave.Sockets.Control

let giveMusic (webSocket : WebSocket) =
    fun cx -> socket {
        let loop = ref true
        while !loop do
        let! msg = webSocket.read()
        match msg with
        | (Text, data, true) ->
            let str = Utils.UTF8.toString data
            do! webSocket.send Text data true
        | (Ping, _, _) ->
            do! webSocket.send Pong [||] true
        | (Close, _, _) ->
            do! webSocket.send Close [||] true
            loop := false
        | _ -> ()
    }

let app : Types.WebPart =
    choose [
    Applicatives.path "/givemethemusic" >>= handShake giveMusic
    Applicatives.GET >>= choose [ Applicatives.path "/" >>= file "index.htm"; browseHome ];
    RequestErrors.NOT_FOUND "Found no handlers."
    ]

let start () =
    let config = defaultConfig
    printfn "Starting on %d" config.bindings.Head.socketBinding.port
    startWebServer config app
