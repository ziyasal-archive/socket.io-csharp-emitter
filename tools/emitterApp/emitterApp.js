var io = require('socket.io-emitter')({ host: 'localhost', port: '6379' });

setInterval(function(){
  io.emit('broadcast event', 'Hello from socket.io-emitter');
}, 5000);