socket.io-csharp-emitter
=========================

A C# implementation of socket.io-emitter

[![Build status](https://ci.appveyor.com/api/projects/status/l8xbixquvx439rwa?svg=true)](https://ci.appveyor.com/project/ziyasal/socket-io-csharp-emitter)

[socket.io](http://socket.io/) provides a hook point to easily allow you to emit events to browsers from anywhere so `socket.io-csharp-emitter` communicates with [socket.io](http://socket.io/) servers through redis

## How to use

```cs
PM> Install-Package SocketIO.Emitter
```

```cs

 IEmitter io = new Emitter(new EmitterOptions
    {
        Host = "localhost",
        Port = 6379
    });
  io.Emit("news","Hello from c# emitter");
```

## API

### Emitter(EmitterOptions opts)

The following options are allowed:

- `Key`: the name of the key to pub/sub events on as prefix (`socket.io`)
- `Host`: host to connect to redis on (`localhost`)
- `Port`: port to connect to redis on (`6379`)

If you don't want to supply a redis client object, and want
`socket.io-csharp-emitter` to intiialize one for you, make sure to supply the
`host` and `port` options.

Specifies a specific `room` that you want to emit to.

### Emitter#In(string room):IEmitter
```cs

 IEmitter io = new Emitter(new EmitterOptions
 {
    Host = "localhost",
    Port = 6379
 });
    
 io.In("room-name").Emit("news","Hello from c# emitter");
```
### Emitter#To(string room):IEmitter
```cs

 IEmitter io = new Emitter(new EmitterOptions
 {
    Host = "localhost",
    Port = 6379
 });
    
 io.To("room-name").Emit("news","Hello from c# emitter");
```

### Emitter#Of(string namespace):IEmitter
Specifies a specific namespace that you want to emit to.
```cs

 IEmitter io = new Emitter(new EmitterOptions
 {
    Host = "localhost",
    Port = 6379
 });
    
 io.Of("/nsp").In("room-name").Emit("news","Hello from c# emitter");
```


## License

MIT


Open Source Projects in use
---------------------
* [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) by StackExchange
* [Msgpack.Cli](https://github.com/msgpack/msgpack-cli) by Yusuke Fujiwara (@yfakariya)
