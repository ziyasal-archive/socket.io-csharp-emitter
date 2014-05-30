socket.io-csharp-emitter
=========================

A C# implementation of socket.io-emitter

`socket.io-csharp-emitter` allows you to communicate with socket.io servers
easily from c# processes.

## How to use

```cs

 IEmitter io = new Emitter(new EmitterOptions
    {
        Host = "localhost",
        Port = 6379
    });
  io.Emit("news","Hello from c# emitter");
```

## API

### Emitter(opts)

The following options are allowed:

- `Key`: the name of the key to pub/sub events on as prefix (`socket.io`)
- `Host`: host to connect to redis on (`localhost`)
- `Port`: port to connect to redis on (`6379`)

If you don't want to supply a redis client object, and want
`socket.io-csharp-emitter` to intiialize one for you, make sure to supply the
`host` and `port` options.

### Emitter#to(string room):IEmitter
### Emitter#in(string room):IEmitter

Specifies a specific `room` that you want to emit to.


### Emitter#Of(string namespace):IEmitter

Specifies a specific namespace that you want to emit to.

## License

MIT


Open Source Projects in use
---------------------
* [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) by StackExchange
* [Msgpack.Cli](https://github.com/msgpack/msgpack-cli) by Yusuke Fujiwara (@yfakariya)
