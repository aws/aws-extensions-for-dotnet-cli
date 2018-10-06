console.log('Loading');

exports.handler = function (event, context) {

    var fs = require('fs');

    var contents = fs.readFileSync('datafile.txt', 'utf8');

    var response = {};
    response.statusCode = 200;
    response.body = contents;
    response.headers = {};
    response.headers['Content-Type'] = "text/plain";

    context.done(null, response);  // SUCCESS with message
};
