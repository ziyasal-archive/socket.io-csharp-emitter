"use strict";

var express = require('express');
var routes = require('./routes');
var http = require('http');
var path = require('path');

var msgpack = require('msgpack-js').decode;
var io = require('socket.io');
var redis = require('redis');
var socketioClient = require('socket.io-client');
var redisAdapter = require('socket.io-redis');
var pub = redis.createClient();
var sub = redis.createClient(null, null, { detect_buffers: true });

//Testing purpose only.
var redisCli = redis.createClient();

var app = express();

// all environments
app.set('port', process.env.PORT || 3000);
app.set('views', path.join(__dirname, 'views'));
app.set('view engine', 'jade');
app.use(express.favicon());
app.use(express.logger('dev'));
app.use(express.json());
app.use(express.urlencoded());
app.use(express.methodOverride());
app.use(app.router);
app.use(require('stylus').middleware(path.join(__dirname, 'public')));
app.use(express.static(path.join(__dirname, 'public')));

// development only
if ('development' == app.get('env')) {
  app.use(express.errorHandler());
}

app.get('/', routes.index);

var server = http.createServer(app);

var sio = io(server, { adapter: redisAdapter({ pubClient: pub, subClient: sub }) });

sio.sockets.on('connection', function (socket) {
    socket.emit('news', { hello: 'hello socket.io connected.' });
    socket.on('broadcast event', function (payload) {
        console.log("payload received from emitter payload:");
        console.log(payload);
        sio.sockets.emit('news', { hello: 'payload received from emitter' });
        sio.sockets.emit('hello', { hello: payload });
        socket.emit('broadcast event', payload);
    });
    socket.on('my other event', function (data) {
        console.log(data);
    });
});

//Testing purpose only.
//redisCli.subscribe("socket.io#emitter");
//redisCli.on("message", function(channel, message) {
//    //TODO: Unpack failed.
//    var unpacked = msgpack(message);

//    console.log(channel + ": " + unpacked);
//});

server.listen(app.get('port'), function(){
  console.log('Express server listening on port ' + app.get('port'));
});
