var keyToNote = {
    'Black1': 'D4',           
    'White1': 'E4',
    'Black2': 'F#4',
    'White2': 'G4',
    'Black3': 'A4',
    'White3': 'B4',
    'Black4': 'C#4',
    'White4': 'D5',
    'Black5': 'E5',
    'White5': 'F5',
    'Black6': 'G5',
    'White6': 'A5',
    'Black7': 'B5',
};

var piano = new Wad(Wad.presets.piano)
piano.setVolume(0.5);

function pressKey(key) {
    piano.play({ pitch : keyToNote[key] }); 
}

document.addEventListener('DOMContentLoaded', function() {
    var connection = new WebSocket('ws://127.0.0.1:8083/givemethemusic');

    connection.onopen = function () {
    };

    connection.onerror = function (error) {
    };

    connection.onmessage = function (message) {
        var json = JSON.parse(message.data);
        var newKeys = json.newKeys;
        newKeys.forEach(function(key) {
            pressKey(key);
            console.log(key);
        });
    };
});
