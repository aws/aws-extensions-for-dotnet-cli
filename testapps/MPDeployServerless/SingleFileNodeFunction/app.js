console.log('Loading');

exports.handler = function (event, context) {

    if (event != null) {
        console.log('event = ' + JSON.stringify(event));
    }
    else {
        console.log('No event object');

    }

    var response = {};
    response.statusCode = 200;
    response.body = 'SingleFileNodeFunction';
    response.headers = {};
    response.headers['Content-Type'] = "text/plain";

    context.done(null, response);  // SUCCESS with message
};
